using FashionHub.Web.Application.Orders;
using FashionHub.Web.Data;
using FashionHub.Web.Domain;
using FashionHub.Web.Models.Generated;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Tests.Application.Orders;

public sealed class OrderCancellationServiceTests
{
    [Fact]
    public async Task ApplyAsync_RestoresInventoryCouponAndHistoryOnce()
    {
        await using var dbContext = CreateDbContext();
        var order = SeedOrder(dbContext);
        var service = new OrderCancellationService(dbContext, TimeProvider.System);

        var firstResult = await service.ApplyAsync(order, 1, "Test cancellation");
        await dbContext.SaveChangesAsync();
        var secondResult = await service.ApplyAsync(order, 1, "Duplicate cancellation");
        await dbContext.SaveChangesAsync();

        firstResult.Should().BeTrue();
        secondResult.Should().BeFalse();
        order.IdtrangThai.Should().Be(OrderStatusIds.Cancelled);

        var variant = await dbContext.BienTheSanPhams.SingleAsync();
        variant.SoLuongTon.Should().Be(10);
        variant.TongDaBan.Should().Be(0);

        var coupon = await dbContext.MaGiamGia.SingleAsync();
        coupon.DaSuDung.Should().Be(0);

        var inventoryHistory = await dbContext.LichSuTonKhos.SingleAsync();
        inventoryHistory.SoLuongThayDoi.Should().Be(2);
        inventoryHistory.TonTruoc.Should().Be(8);
        inventoryHistory.TonSau.Should().Be(10);
        (await dbContext.LichSuDonHangs.CountAsync()).Should().Be(1);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"OrderCancellationTests_{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static DonHang SeedOrder(ApplicationDbContext dbContext)
    {
        var coupon = new MaGiamGium
        {
            IdmaGiamGia = 1,
            MaCode = "TEST10",
            LoaiGiamGia = CouponTypes.FixedAmount,
            GiaTri = 10000,
            SoLuong = 10,
            DaSuDung = 1,
            NgayBatDau = DateTime.Now.AddDays(-1),
            NgayKetThuc = DateTime.Now.AddDays(1),
            TrangThai = true,
            NgayTao = DateTime.Now
        };
        var variant = new BienTheSanPham
        {
            IdbienThe = 1,
            IdsanPham = 1,
            Sku = "TEST-001",
            Gia = 100000,
            SoLuongTon = 8,
            TongDaBan = 2,
            TrangThai = true,
            RowVersion = []
        };
        var order = new DonHang
        {
            IddonHang = 10,
            IdnguoiDung = 1,
            IdmaGiamGia = coupon.IdmaGiamGia,
            TenNguoiNhan = "Test User",
            DiaChiGiao = "Test address",
            SoDienThoai = "0123456789",
            TongTienHang = 200000,
            PhiVanChuyen = 30000,
            TienGiamGia = 10000,
            TongThanhToan = 220000,
            IdtrangThai = OrderStatusIds.Pending,
            TrangThaiThanhToan = PaymentStatusIds.Unpaid,
            NgayTao = DateTime.Now
        };
        order.ChiTietDonHangs.Add(new ChiTietDonHang
        {
            IdchiTietDonHang = 1,
            IddonHang = order.IddonHang,
            IdbienThe = variant.IdbienThe,
            TenSanPham = "Test Product",
            DonGia = 100000,
            SoLuong = 2
        });

        dbContext.MaGiamGia.Add(coupon);
        dbContext.BienTheSanPhams.Add(variant);
        dbContext.DonHangs.Add(order);
        dbContext.SaveChanges();
        return order;
    }
}
