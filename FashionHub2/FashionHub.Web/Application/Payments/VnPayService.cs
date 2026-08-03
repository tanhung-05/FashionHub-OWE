using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FashionHub.Web.Application.Common;
using FashionHub.Web.Data;
using FashionHub.Web.Domain;
using FashionHub.Web.Models.Generated;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FashionHub.Web.Application.Payments;

public sealed class VnPayService : IVnPayService
{
    public const string HttpClientName = "VnPay";
    private const string GatewayName = "VNPAY";
    private readonly ApplicationDbContext dbContext;
    private readonly VnPayOptions options;
    private readonly TimeProvider timeProvider;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<VnPayService> logger;

    public VnPayService(
        ApplicationDbContext dbContext,
        IOptions<VnPayOptions> options,
        TimeProvider timeProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<VnPayService> logger)
    {
        this.dbContext = dbContext;
        this.options = options.Value;
        this.timeProvider = timeProvider;
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    public bool IsConfigured =>
        Uri.TryCreate(options.PaymentUrl, UriKind.Absolute, out _)
        && !string.IsNullOrWhiteSpace(options.TmnCode)
        && !string.IsNullOrWhiteSpace(options.HashSecret)
        && Uri.TryCreate(options.ReturnUrl, UriKind.Absolute, out _);

    public async Task<ServiceResult<string>> CreatePaymentUrlAsync(
        int orderId,
        int userId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return Failure(
                ServiceErrorType.Conflict,
                "vnpay-not-configured",
                "VNPAY chưa được cấu hình. Vui lòng chọn phương thức thanh toán khác.");
        }

        var order = await dbContext.DonHangs
            .Include(item => item.IdphuongThucThanhToanNavigation)
            .FirstOrDefaultAsync(item =>
                item.IddonHang == orderId
                && item.IdnguoiDung == userId,
                cancellationToken);
        if (order == null)
        {
            return Failure(
                ServiceErrorType.NotFound,
                "order-not-found",
                "Không tìm thấy đơn hàng cần thanh toán.");
        }

        if (!string.Equals(
                order.IdphuongThucThanhToanNavigation?.MaPhuongThuc,
                PaymentMethodCodes.VnPay,
                StringComparison.OrdinalIgnoreCase))
        {
            return Failure(
                ServiceErrorType.Conflict,
                "payment-method-not-vnpay",
                "Đơn hàng không sử dụng phương thức VNPAY.");
        }

        if (order.IdtrangThai == OrderStatusIds.Cancelled
            || order.TrangThaiThanhToan is PaymentStatusIds.Paid or PaymentStatusIds.Refunded)
        {
            return Failure(
                ServiceErrorType.Conflict,
                "order-not-payable",
                "Đơn hàng không còn ở trạng thái có thể thanh toán.");
        }

        var transaction = await dbContext.GiaoDichThanhToans
            .Where(item =>
                item.IddonHang == orderId
                && item.CongThanhToan == GatewayName
                && item.TrangThai == PaymentStatusIds.Pending)
            .OrderByDescending(item => item.NgayTao)
            .FirstOrDefaultAsync(cancellationToken);

        var nowUtc = timeProvider.GetUtcNow();
        if (transaction == null)
        {
            transaction = new GiaoDichThanhToan
            {
                IddonHang = order.IddonHang,
                MaThamChieu = CreateReference(order.IddonHang, nowUtc),
                CongThanhToan = GatewayName,
                SoTien = order.TongThanhToan,
                TrangThai = PaymentStatusIds.Pending,
                NoiDung = $"Thanh toan don hang {order.IddonHang}",
                NgayTao = nowUtc.UtcDateTime,
                RowVersion = []
            };
            dbContext.GiaoDichThanhToans.Add(transaction);
        }

        order.TrangThaiThanhToan = PaymentStatusIds.Pending;
        order.NgayCapNhat = DateTime.Now;
        await dbContext.SaveChangesAsync(cancellationToken);

        var parameters = BuildPaymentParameters(transaction, ipAddress, nowUtc);
        var query = BuildQuery(parameters);
        var signature = ComputeSignature(query);
        var paymentUrl = $"{options.PaymentUrl.TrimEnd('?')}?{query}&vnp_SecureHash={signature}";

        logger.LogInformation(
            "Created VNPAY payment request {Reference} for order {OrderId}",
            transaction.MaThamChieu,
            order.IddonHang);

        return ServiceResult<string>.Success(paymentUrl);
    }

    public async Task<VnPayCallbackResult> ProcessCallbackAsync(
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || !HasValidSignature(parameters))
        {
            return Invalid("Chữ ký VNPAY không hợp lệ.", "97");
        }

        if (!parameters.TryGetValue("vnp_TxnRef", out var reference)
            || string.IsNullOrWhiteSpace(reference))
        {
            return Invalid("Thiếu mã tham chiếu giao dịch.", "99");
        }

        var transaction = await dbContext.GiaoDichThanhToans
            .Include(item => item.IddonHangNavigation)
            .FirstOrDefaultAsync(
                item => item.MaThamChieu == reference,
                cancellationToken);
        if (transaction == null)
        {
            return new VnPayCallbackResult(
                true,
                false,
                false,
                null,
                reference,
                Get(parameters, "vnp_ResponseCode"),
                "Không tìm thấy giao dịch.",
                "01");
        }

        if (!TryReadAmount(parameters, out var amount)
            || amount != transaction.SoTien)
        {
            return new VnPayCallbackResult(
                true,
                false,
                false,
                transaction.IddonHang,
                reference,
                Get(parameters, "vnp_ResponseCode"),
                "Số tiền giao dịch không khớp.",
                "04");
        }

        if (transaction.TrangThai != PaymentStatusIds.Pending)
        {
            var wasPaid = transaction.TrangThai == PaymentStatusIds.Paid;
            return new VnPayCallbackResult(
                true,
                wasPaid,
                true,
                transaction.IddonHang,
                reference,
                Get(parameters, "vnp_ResponseCode"),
                "Giao dịch đã được xử lý trước đó.",
                "02");
        }

        var responseCode = Get(parameters, "vnp_ResponseCode");
        var transactionStatus = Get(parameters, "vnp_TransactionStatus");
        var successful = responseCode == "00" && transactionStatus == "00";
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        transaction.MaPhanHoi = responseCode;
        transaction.MaGiaoDichCong = NullIfEmpty(Get(parameters, "vnp_TransactionNo"));
        transaction.MaNganHang = NullIfEmpty(Get(parameters, "vnp_BankCode"));
        transaction.NgayCapNhat = nowUtc;
        transaction.TrangThai = successful
            ? PaymentStatusIds.Paid
            : PaymentStatusIds.Failed;
        transaction.NgayThanhToan = successful ? nowUtc : null;

        var order = transaction.IddonHangNavigation;
        order.TrangThaiThanhToan = transaction.TrangThai;
        order.NgayThanhToan = transaction.NgayThanhToan;
        order.NgayCapNhat = DateTime.Now;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogInformation(
                "VNPAY callback {Reference} was processed concurrently",
                reference);
            return new VnPayCallbackResult(
                true,
                successful,
                true,
                transaction.IddonHang,
                reference,
                responseCode,
                "Giao dịch đã được xử lý.",
                "02");
        }

        logger.LogInformation(
            "Processed VNPAY callback {Reference} with response {ResponseCode}",
            reference,
            responseCode);

        return new VnPayCallbackResult(
            true,
            successful,
            false,
            transaction.IddonHang,
            reference,
            responseCode,
            successful
                ? "Thanh toán thành công."
                : "Thanh toán chưa thành công hoặc đã bị hủy.",
            "00");
    }

    public async Task<VnPayQueryResult> QueryTransactionAsync(
        GiaoDichThanhToan transaction,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured
            || !Uri.TryCreate(options.TransactionApiUrl, UriKind.Absolute, out var apiUri))
        {
            return QueryFailure("VNPAY query API chưa được cấu hình.");
        }

        var vietnamTimeZone = GetVietnamTimeZone();
        var nowVietnam = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), vietnamTimeZone);
        var transactionUtc = DateTime.SpecifyKind(transaction.NgayTao, DateTimeKind.Utc);
        var transactionVietnam = TimeZoneInfo.ConvertTime(
            new DateTimeOffset(transactionUtc),
            vietnamTimeZone);
        var requestId = Guid.NewGuid().ToString("N");
        var createDate = nowVietnam.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var transactionDate = transactionVietnam.ToString(
            "yyyyMMddHHmmss",
            CultureInfo.InvariantCulture);
        var orderInfo = $"Query order {transaction.IddonHang}";
        var serverIp = string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress;
        var signatureData = string.Join(
            "|",
            requestId,
            "2.1.0",
            "querydr",
            options.TmnCode,
            transaction.MaThamChieu,
            transactionDate,
            createDate,
            serverIp,
            orderInfo);

        var request = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_RequestId"] = requestId,
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "querydr",
            ["vnp_TmnCode"] = options.TmnCode,
            ["vnp_TxnRef"] = transaction.MaThamChieu,
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_TransactionDate"] = transactionDate,
            ["vnp_CreateDate"] = createDate,
            ["vnp_IpAddr"] = serverIp,
            ["vnp_SecureHash"] = ComputeSignature(signatureData)
        };

        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.PostAsJsonAsync(apiUri, request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return QueryFailure($"VNPAY query API trả về HTTP {(int)response.StatusCode}.");
            }

            var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(
                cancellationToken: cancellationToken);
            return payload == null
                ? QueryFailure("VNPAY query API không trả về dữ liệu.")
                : ParseQueryResponse(payload);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(
                exception,
                "Unable to query VNPAY transaction {Reference}",
                transaction.MaThamChieu);
            return QueryFailure("Không thể kết nối API truy vấn VNPAY.");
        }
    }

    private VnPayQueryResult ParseQueryResponse(
        IReadOnlyDictionary<string, JsonElement> payload)
    {
        string Read(string key) =>
            payload.TryGetValue(key, out var value) ? value.ToString() : string.Empty;

        var responseCode = Read("vnp_ResponseCode");
        var transactionStatus = Read("vnp_TransactionStatus");
        var signatureData = string.Join(
            "|",
            Read("vnp_ResponseId"),
            Read("vnp_Command"),
            responseCode,
            Read("vnp_Message"),
            Read("vnp_TmnCode"),
            Read("vnp_TxnRef"),
            Read("vnp_Amount"),
            Read("vnp_BankCode"),
            Read("vnp_PayDate"),
            Read("vnp_TransactionNo"),
            Read("vnp_TransactionType"),
            transactionStatus,
            Read("vnp_OrderInfo"),
            Read("vnp_PromotionCode"),
            Read("vnp_PromotionAmount"));
        var validSignature = HasMatchingHash(signatureData, Read("vnp_SecureHash"));

        return new VnPayQueryResult(
            validSignature,
            responseCode == "00",
            responseCode != "91",
            responseCode,
            transactionStatus,
            Read("vnp_Message"),
            NullIfEmpty(Read("vnp_TransactionNo")),
            NullIfEmpty(Read("vnp_BankCode")),
            ParseVnPayDate(Read("vnp_PayDate")));
    }

    private bool HasMatchingHash(string data, string receivedHash)
    {
        if (receivedHash.Length != 128)
        {
            return false;
        }

        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(ComputeSignature(data)),
                Convert.FromHexString(receivedHash));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static DateTime? ParseVnPayDate(string value) =>
        DateTime.TryParseExact(
            value,
            "yyyyMMddHHmmss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var result)
            ? result
            : null;

    private static VnPayQueryResult QueryFailure(string message) =>
        new(false, false, false, string.Empty, string.Empty, message, null, null, null);

    private SortedDictionary<string, string> BuildPaymentParameters(
        GiaoDichThanhToan transaction,
        string ipAddress,
        DateTimeOffset nowUtc)
    {
        var vietnamTime = TimeZoneInfo.ConvertTime(
            nowUtc,
            GetVietnamTimeZone());
        var expiresAt = vietnamTime.AddMinutes(
            Math.Clamp(options.PaymentTimeoutMinutes, 5, 60));

        return new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Amount"] = decimal
                .ToInt64(transaction.SoTien * 100)
                .ToString(CultureInfo.InvariantCulture),
            ["vnp_Command"] = "pay",
            ["vnp_CreateDate"] = vietnamTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_CurrCode"] = "VND",
            ["vnp_ExpireDate"] = expiresAt.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_IpAddr"] = string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress,
            ["vnp_Locale"] = "vn",
            ["vnp_OrderInfo"] = transaction.NoiDung ?? $"Thanh toan don hang {transaction.IddonHang}",
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = options.ReturnUrl,
            ["vnp_TmnCode"] = options.TmnCode,
            ["vnp_TxnRef"] = transaction.MaThamChieu,
            ["vnp_Version"] = "2.1.0"
        };
    }

    private bool HasValidSignature(IReadOnlyDictionary<string, string> parameters)
    {
        var receivedHash = Get(parameters, "vnp_SecureHash");
        if (receivedHash.Length != 128)
        {
            return false;
        }

        var signableParameters = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in parameters)
        {
            if (key.StartsWith("vnp_", StringComparison.Ordinal)
                && !key.Equals("vnp_SecureHash", StringComparison.Ordinal)
                && !key.Equals("vnp_SecureHashType", StringComparison.Ordinal)
                && !string.IsNullOrEmpty(value))
            {
                signableParameters[key] = value;
            }
        }

        var expectedHash = ComputeSignature(BuildQuery(signableParameters));
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(expectedHash),
                Convert.FromHexString(receivedHash));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private string ComputeSignature(string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(options.HashSecret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)))
            .ToLowerInvariant();
    }

    private static string BuildQuery(IEnumerable<KeyValuePair<string, string>> parameters) =>
        string.Join(
            "&",
            parameters.Select(item =>
                $"{WebUtility.UrlEncode(item.Key)}={WebUtility.UrlEncode(item.Value)}"));

    private static bool TryReadAmount(
        IReadOnlyDictionary<string, string> parameters,
        out decimal amount)
    {
        amount = 0;
        return long.TryParse(
                Get(parameters, "vnp_Amount"),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var scaledAmount)
            && scaledAmount >= 0
            && (amount = scaledAmount / 100m) >= 0;
    }

    private static string Get(
        IReadOnlyDictionary<string, string> parameters,
        string key) =>
        parameters.TryGetValue(key, out var value) ? value : string.Empty;

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string CreateReference(int orderId, DateTimeOffset nowUtc) =>
        $"FH{orderId}-{nowUtc:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..34];

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
    }

    private static VnPayCallbackResult Invalid(string message, string merchantCode) =>
        new(false, false, false, null, null, string.Empty, message, merchantCode);

    private static ServiceResult<string> Failure(
        ServiceErrorType type,
        string code,
        string message) =>
        ServiceResult<string>.Failure(type, code, message);
}
