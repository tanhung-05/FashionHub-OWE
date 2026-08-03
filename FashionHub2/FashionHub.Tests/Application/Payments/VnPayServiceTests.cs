using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FashionHub.Web.Application.Payments;
using FashionHub.Web.Data;
using FashionHub.Web.Domain;
using FashionHub.Web.Models.Generated;
using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FashionHub.Tests.Application.Payments;

public sealed class VnPayServiceTests
{
    private const string HashSecret = "test-hash-secret-for-vnpay";

    [Fact]
    public async Task CreatePaymentUrlAsync_CreatesSignedPendingTransaction()
    {
        await using var dbContext = CreateDbContext();
        SeedOrder(dbContext);
        var service = CreateService(dbContext);

        var result = await service.CreatePaymentUrlAsync(10, 1, "127.0.0.1");

        result.IsSuccess.Should().BeTrue();
        var uri = new Uri(result.Value!);
        uri.Host.Should().Be("sandbox.vnpayment.vn");
        var query = QueryHelpers.ParseQuery(uri.Query);
        query["vnp_Amount"].ToString().Should().Be("13000000");
        query["vnp_TxnRef"].ToString().Should().StartWith("FH10-");
        query["vnp_SecureHash"].ToString().Should().HaveLength(128);

        var transaction = await dbContext.GiaoDichThanhToans.SingleAsync();
        transaction.TrangThai.Should().Be(PaymentStatusIds.Pending);
        transaction.SoTien.Should().Be(130000m);
        (await dbContext.DonHangs.FindAsync(10))!
            .TrangThaiThanhToan.Should().Be(PaymentStatusIds.Pending);
    }

    [Fact]
    public async Task ProcessCallbackAsync_WithInvalidSignature_DoesNotUpdatePayment()
    {
        await using var dbContext = CreateDbContext();
        SeedOrder(dbContext);
        var service = CreateService(dbContext);
        var paymentUrl = (await service.CreatePaymentUrlAsync(10, 1, "127.0.0.1")).Value!;
        var callback = CreateCallback(paymentUrl, "00", "00", 130000m);
        callback["vnp_SecureHash"] = new string('0', 128);

        var result = await service.ProcessCallbackAsync(callback);

        result.IsValidSignature.Should().BeFalse();
        result.MerchantResponseCode.Should().Be("97");
        (await dbContext.GiaoDichThanhToans.SingleAsync())
            .TrangThai.Should().Be(PaymentStatusIds.Pending);
    }

    [Fact]
    public async Task ProcessCallbackAsync_WithWrongAmount_RejectsUpdate()
    {
        await using var dbContext = CreateDbContext();
        SeedOrder(dbContext);
        var service = CreateService(dbContext);
        var paymentUrl = (await service.CreatePaymentUrlAsync(10, 1, "127.0.0.1")).Value!;
        var callback = CreateCallback(paymentUrl, "00", "00", 100000m);

        var result = await service.ProcessCallbackAsync(callback);

        result.IsValidSignature.Should().BeTrue();
        result.MerchantResponseCode.Should().Be("04");
        (await dbContext.GiaoDichThanhToans.SingleAsync())
            .TrangThai.Should().Be(PaymentStatusIds.Pending);
    }

    [Fact]
    public async Task ProcessCallbackAsync_WithSuccessfulResponse_IsIdempotent()
    {
        await using var dbContext = CreateDbContext();
        SeedOrder(dbContext);
        var service = CreateService(dbContext);
        var paymentUrl = (await service.CreatePaymentUrlAsync(10, 1, "127.0.0.1")).Value!;
        var callback = CreateCallback(paymentUrl, "00", "00", 130000m);

        var firstResult = await service.ProcessCallbackAsync(callback);
        var secondResult = await service.ProcessCallbackAsync(callback);

        firstResult.IsSuccessful.Should().BeTrue();
        firstResult.MerchantResponseCode.Should().Be("00");
        secondResult.IsSuccessful.Should().BeTrue();
        secondResult.IsAlreadyProcessed.Should().BeTrue();
        secondResult.MerchantResponseCode.Should().Be("02");

        var transaction = await dbContext.GiaoDichThanhToans.SingleAsync();
        transaction.TrangThai.Should().Be(PaymentStatusIds.Paid);
        transaction.MaGiaoDichCong.Should().Be("14587452");
        transaction.MaNganHang.Should().Be("NCB");
        var order = await dbContext.DonHangs.FindAsync(10);
        order!.TrangThaiThanhToan.Should().Be(PaymentStatusIds.Paid);
        order.NgayThanhToan.Should().NotBeNull();
    }

    [Fact]
    public async Task QueryTransactionAsync_WithSignedPaidResponse_ReturnsPaidResult()
    {
        await using var dbContext = CreateDbContext();
        SeedOrder(dbContext);
        var service = CreateService(dbContext, new QueryResponseHandler());
        await service.CreatePaymentUrlAsync(10, 1, "127.0.0.1");
        var transaction = await dbContext.GiaoDichThanhToans.SingleAsync();

        var result = await service.QueryTransactionAsync(
            transaction,
            "127.0.0.1");

        result.IsValidSignature.Should().BeTrue();
        result.IsRequestSuccessful.Should().BeTrue();
        result.IsPaid.Should().BeTrue();
        result.GatewayTransactionNumber.Should().Be("14587452");
        result.BankCode.Should().Be("NCB");
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VnPayTests_{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static VnPayService CreateService(
        ApplicationDbContext dbContext,
        HttpMessageHandler? handler = null) =>
        new(
            dbContext,
            Options.Create(new VnPayOptions
            {
                PaymentUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
                TmnCode = "TESTCODE",
                HashSecret = HashSecret,
                ReturnUrl = "https://fashionhub.example/payment/vnpay-return",
                PaymentTimeoutMinutes = 15
            }),
            TimeProvider.System,
            new TestHttpClientFactory(handler),
            NullLogger<VnPayService>.Instance);

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient client;

        public TestHttpClientFactory(HttpMessageHandler? handler = null)
        {
            client = handler == null ? new HttpClient() : new HttpClient(handler);
        }

        public HttpClient CreateClient(string name) => client;
    }

    private sealed class QueryResponseHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadFromJsonAsync<Dictionary<string, JsonElement>>(
                cancellationToken: cancellationToken);
            var response = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_ResponseId"] = "QUERY0001",
                ["vnp_Command"] = "querydr",
                ["vnp_ResponseCode"] = "00",
                ["vnp_Message"] = "Success",
                ["vnp_TmnCode"] = "TESTCODE",
                ["vnp_TxnRef"] = body!["vnp_TxnRef"].ToString(),
                ["vnp_Amount"] = "13000000",
                ["vnp_BankCode"] = "NCB",
                ["vnp_PayDate"] = "20260803120000",
                ["vnp_TransactionNo"] = "14587452",
                ["vnp_TransactionType"] = "01",
                ["vnp_TransactionStatus"] = "00",
                ["vnp_OrderInfo"] = "Thanh toan don hang 10",
                ["vnp_PromotionCode"] = string.Empty,
                ["vnp_PromotionAmount"] = string.Empty
            };
            response["vnp_SecureHash"] = SignQueryResponse(response);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response)
            };
        }
    }

    private static string SignQueryResponse(IReadOnlyDictionary<string, string> response)
    {
        var data = string.Join(
            "|",
            response["vnp_ResponseId"],
            response["vnp_Command"],
            response["vnp_ResponseCode"],
            response["vnp_Message"],
            response["vnp_TmnCode"],
            response["vnp_TxnRef"],
            response["vnp_Amount"],
            response["vnp_BankCode"],
            response["vnp_PayDate"],
            response["vnp_TransactionNo"],
            response["vnp_TransactionType"],
            response["vnp_TransactionStatus"],
            response["vnp_OrderInfo"],
            response["vnp_PromotionCode"],
            response["vnp_PromotionAmount"]);
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(HashSecret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)))
            .ToLowerInvariant();
    }

    private static void SeedOrder(ApplicationDbContext dbContext)
    {
        var method = new PhuongThucThanhToan
        {
            IdphuongThucThanhToan = 2,
            MaPhuongThuc = PaymentMethodCodes.VnPay,
            TenPhuongThuc = "VNPAY",
            TrangThai = true
        };
        dbContext.PhuongThucThanhToans.Add(method);
        dbContext.DonHangs.Add(new DonHang
        {
            IddonHang = 10,
            IdnguoiDung = 1,
            TenNguoiNhan = "Test User",
            DiaChiGiao = "Test address",
            SoDienThoai = "0123456789",
            TongTienHang = 100000m,
            PhiVanChuyen = 30000m,
            TongThanhToan = 130000m,
            IdphuongThucThanhToan = 2,
            IdphuongThucThanhToanNavigation = method,
            IdtrangThai = OrderStatusIds.Pending,
            TrangThaiThanhToan = PaymentStatusIds.Pending,
            NgayTao = DateTime.Now
        });
        dbContext.SaveChanges();
    }

    private static Dictionary<string, string> CreateCallback(
        string paymentUrl,
        string responseCode,
        string transactionStatus,
        decimal amount)
    {
        var parsed = QueryHelpers.ParseQuery(new Uri(paymentUrl).Query);
        var callback = parsed
            .Where(item => item.Key != "vnp_SecureHash")
            .ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.Ordinal);
        callback["vnp_Amount"] = decimal.ToInt64(amount * 100).ToString();
        callback["vnp_ResponseCode"] = responseCode;
        callback["vnp_TransactionStatus"] = transactionStatus;
        callback["vnp_TransactionNo"] = "14587452";
        callback["vnp_BankCode"] = "NCB";
        callback["vnp_PayDate"] = "20260803120000";
        callback["vnp_SecureHash"] = Sign(callback);
        return callback;
    }

    private static string Sign(IReadOnlyDictionary<string, string> parameters)
    {
        var query = string.Join(
            "&",
            parameters
                .Where(item => item.Key != "vnp_SecureHash" && !string.IsNullOrEmpty(item.Value))
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item =>
                    $"{WebUtility.UrlEncode(item.Key)}={WebUtility.UrlEncode(item.Value)}"));
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(HashSecret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(query)))
            .ToLowerInvariant();
    }
}
