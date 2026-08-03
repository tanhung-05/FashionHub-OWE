using FashionHub.Web.Application.Common;
using FashionHub.Web.Application.Orders;
using FashionHub.Web.Application.Payments;
using FashionHub.Web.Data;
using FashionHub.Web.Domain;
using FashionHub.Web.Models.Generated;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FashionHub.Tests.Application.Payments;

public sealed class PendingPaymentReconciliationServiceTests
{
    [Fact]
    public async Task ReconcileAsync_ExpiresUnfinishedPaymentAndRestoresStock()
    {
        await using var dbContext = CreateDbContext();
        SeedPendingPayment(dbContext);
        var options = Options.Create(new VnPayOptions
        {
            AbandonedPaymentHours = 24
        });
        var cancellationService = new OrderCancellationService(
            dbContext,
            TimeProvider.System);
        var service = new PendingPaymentReconciliationService(
            dbContext,
            new ExpiredVnPayService(),
            cancellationService,
            options,
            TimeProvider.System,
            NullLogger<PendingPaymentReconciliationService>.Instance);

        var result = await service.ReconcileAsync();

        result.Should().Be(new PaymentReconciliationSummary(1, 0, 1, 0));
        (await dbContext.GiaoDichThanhToans.SingleAsync())
            .TrangThai.Should().Be(PaymentStatusIds.Failed);
        (await dbContext.DonHangs.SingleAsync())
            .IdtrangThai.Should().Be(OrderStatusIds.Cancelled);
        var variant = await dbContext.BienTheSanPhams.SingleAsync();
        variant.SoLuongTon.Should().Be(10);
        variant.TongDaBan.Should().Be(0);
        (await dbContext.LichSuTonKhos.CountAsync()).Should().Be(1);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"PaymentReconciliationTests_{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void SeedPendingPayment(ApplicationDbContext dbContext)
    {
        var order = new DonHang
        {
            IddonHang = 20,
            IdnguoiDung = 1,
            TenNguoiNhan = "Test User",
            DiaChiGiao = "Test address",
            SoDienThoai = "0123456789",
            TongTienHang = 200000,
            PhiVanChuyen = 30000,
            TongThanhToan = 230000,
            IdtrangThai = OrderStatusIds.Pending,
            TrangThaiThanhToan = PaymentStatusIds.Pending,
            NgayTao = DateTime.Now.AddDays(-2)
        };
        order.ChiTietDonHangs.Add(new ChiTietDonHang
        {
            IdchiTietDonHang = 1,
            IddonHang = order.IddonHang,
            IdbienThe = 1,
            TenSanPham = "Test Product",
            DonGia = 100000,
            SoLuong = 2
        });
        dbContext.DonHangs.Add(order);
        dbContext.BienTheSanPhams.Add(new BienTheSanPham
        {
            IdbienThe = 1,
            IdsanPham = 1,
            Sku = "TEST-001",
            Gia = 100000,
            SoLuongTon = 8,
            TongDaBan = 2,
            TrangThai = true,
            RowVersion = []
        });
        dbContext.GiaoDichThanhToans.Add(new GiaoDichThanhToan
        {
            IdgiaoDich = 1,
            IddonHang = order.IddonHang,
            MaThamChieu = "FH20-OLD-TRANSACTION",
            CongThanhToan = PaymentMethodCodes.VnPay,
            SoTien = order.TongThanhToan,
            TrangThai = PaymentStatusIds.Pending,
            NgayTao = DateTime.UtcNow.AddHours(-25),
            RowVersion = []
        });
        dbContext.SaveChanges();
    }

    private sealed class ExpiredVnPayService : IVnPayService
    {
        public bool IsConfigured => true;

        public Task<VnPayQueryResult> QueryTransactionAsync(
            GiaoDichThanhToan transaction,
            string ipAddress,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new VnPayQueryResult(
                true,
                true,
                true,
                "00",
                "01",
                "Transaction is incomplete",
                null,
                null,
                null));

        public Task<ServiceResult<string>> CreatePaymentUrlAsync(
            int orderId,
            int userId,
            string ipAddress,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<VnPayCallbackResult> ProcessCallbackAsync(
            IReadOnlyDictionary<string, string> parameters,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
