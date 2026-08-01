using System.Net;
using FashionHub.Web.Data;
using FashionHub.Web.Domain;
using FashionHub.Web.Models.Generated;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FashionHub.Tests.Controllers;

public class ProductReviewFlowTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    public ProductReviewFlowTests(CustomWebApplicationFactory<Program> factory)
    {
        this.factory = factory;
        client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false
            });
    }

    [Fact]
    public async Task CreateReview_CompletedOwnedOrder_CreatesAndDisplaysReview()
    {
        const int orderId = 8101;
        const int productId = 8101;
        const int orderItemId = 8101;
        await SeedOrderAsync(
            orderId,
            productId,
            orderItemId,
            OrderStatusIds.Completed);
        await client.LoginAsCustomerAsync();

        var detailHtml = WebUtility.HtmlDecode(
            await client.GetStringAsync($"/Account/OrderDetail/{orderId}"));
        detailHtml.Should().Contain("Đánh giá sản phẩm");
        detailHtml.Should().Contain("/Account/CreateReview");

        var response = await client.PostFormWithAntiforgeryAsync(
            "/Account/CreateReview",
            new Dictionary<string, string>
            {
                ["IddonHang"] = orderId.ToString(),
                ["IdchiTietDonHang"] = orderItemId.ToString(),
                ["DiemSo"] = "5",
                ["NoiDung"] = "  Sản phẩm rất tốt.  "
            },
            $"/Account/OrderDetail/{orderId}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString()
            .Should().Be($"/Account/OrderDetail/{orderId}");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var review = await dbContext.DanhGia
                .AsNoTracking()
                .SingleAsync(item =>
                    item.IdnguoiDung == 1
                    && item.IdsanPham == productId);

            review.IdchiTietDonHang.Should().Be(orderItemId);
            review.DiemSo.Should().Be(5);
            review.NoiDung.Should().Be("Sản phẩm rất tốt.");
            review.TrangThai.Should().BeTrue();
        }

        var reviewedOrderHtml = WebUtility.HtmlDecode(
            await client.GetStringAsync($"/Account/OrderDetail/{orderId}"));
        reviewedOrderHtml.Should().Contain("Đánh giá của bạn");
        reviewedOrderHtml.Should().Contain("Sản phẩm rất tốt.");
        reviewedOrderHtml.Should().NotContain("Đánh giá sản phẩm");

        var productHtml = WebUtility.HtmlDecode(
            await client.GetStringAsync($"/Products/Details/{productId}"));
        productHtml.Should().Contain("Sản phẩm rất tốt.");
        productHtml.Should().Contain("Test User");
    }

    [Fact]
    public async Task CreateReview_OrderNotCompleted_DoesNotCreateReview()
    {
        const int orderId = 8102;
        const int productId = 8102;
        const int orderItemId = 8102;
        await SeedOrderAsync(
            orderId,
            productId,
            orderItemId,
            OrderStatusIds.Shipping);
        await client.LoginAsCustomerAsync();

        var response = await client.PostFormWithAntiforgeryAsync(
            "/Account/CreateReview",
            new Dictionary<string, string>
            {
                ["IddonHang"] = orderId.ToString(),
                ["IdchiTietDonHang"] = orderItemId.ToString(),
                ["DiemSo"] = "4",
                ["NoiDung"] = "Chưa được phép đánh giá"
            },
            "/Account/ChangePassword");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        (await CountReviewsAsync(productId)).Should().Be(0);

        var detailHtml = WebUtility.HtmlDecode(
            await client.GetStringAsync($"/Account/OrderDetail/{orderId}"));
        detailHtml.Should().Contain(
            "Chỉ có thể đánh giá sản phẩm trong đơn hàng đã hoàn thành.");
        detailHtml.Should().NotContain("Đánh giá sản phẩm");
    }

    [Fact]
    public async Task CreateReview_OrderOwnedByAnotherUser_ReturnsNotFound()
    {
        const int orderId = 8103;
        const int productId = 8103;
        const int orderItemId = 8103;
        await SeedOrderAsync(
            orderId,
            productId,
            orderItemId,
            OrderStatusIds.Completed,
            ownerId: 2);
        await client.LoginAsCustomerAsync();

        var response = await client.PostFormWithAntiforgeryAsync(
            "/Account/CreateReview",
            new Dictionary<string, string>
            {
                ["IddonHang"] = orderId.ToString(),
                ["IdchiTietDonHang"] = orderItemId.ToString(),
                ["DiemSo"] = "5"
            },
            "/Account/ChangePassword");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await CountReviewsAsync(productId)).Should().Be(0);
    }

    [Fact]
    public async Task CreateReview_ProductAlreadyReviewed_DoesNotCreateDuplicate()
    {
        const int orderId = 8104;
        const int productId = 8104;
        const int orderItemId = 8104;
        await SeedOrderAsync(
            orderId,
            productId,
            orderItemId,
            OrderStatusIds.Completed,
            addExistingReview: true);
        await client.LoginAsCustomerAsync();

        var initialHtml = WebUtility.HtmlDecode(
            await client.GetStringAsync($"/Account/OrderDetail/{orderId}"));
        initialHtml.Should().Contain("Đánh giá của bạn");
        initialHtml.Should().NotContain("Đánh giá sản phẩm");

        var response = await client.PostFormWithAntiforgeryAsync(
            "/Account/CreateReview",
            new Dictionary<string, string>
            {
                ["IddonHang"] = orderId.ToString(),
                ["IdchiTietDonHang"] = orderItemId.ToString(),
                ["DiemSo"] = "2",
                ["NoiDung"] = "Đánh giá thứ hai"
            },
            "/Account/ChangePassword");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        (await CountReviewsAsync(productId)).Should().Be(1);

        var detailHtml = WebUtility.HtmlDecode(
            await client.GetStringAsync($"/Account/OrderDetail/{orderId}"));
        detailHtml.Should().Contain("Bạn đã đánh giá sản phẩm này.");
    }

    [Fact]
    public async Task CreateReview_RatingOutsideAllowedRange_DoesNotCreateReview()
    {
        const int orderId = 8105;
        const int productId = 8105;
        const int orderItemId = 8105;
        await SeedOrderAsync(
            orderId,
            productId,
            orderItemId,
            OrderStatusIds.Completed);
        await client.LoginAsCustomerAsync();

        var response = await client.PostFormWithAntiforgeryAsync(
            "/Account/CreateReview",
            new Dictionary<string, string>
            {
                ["IddonHang"] = orderId.ToString(),
                ["IdchiTietDonHang"] = orderItemId.ToString(),
                ["DiemSo"] = "6",
                ["NoiDung"] = "Điểm không hợp lệ"
            },
            $"/Account/OrderDetail/{orderId}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        (await CountReviewsAsync(productId)).Should().Be(0);

        var detailHtml = WebUtility.HtmlDecode(
            await client.GetStringAsync($"/Account/OrderDetail/{orderId}"));
        detailHtml.Should().Contain("Điểm đánh giá phải từ 1 đến 5 sao.");
    }

    private async Task SeedOrderAsync(
        int orderId,
        int productId,
        int orderItemId,
        int statusId,
        int ownerId = 1,
        bool addExistingReview = false)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var variantId = productId;

        dbContext.SanPhams.Add(new SanPham
        {
            IdsanPham = productId,
            TenSanPham = $"Review Product {productId}",
            MoTa = "Product used by the review flow tests",
            IddanhMuc = 1,
            IdthuongHieu = 1,
            Gia = 150000,
            TrangThai = true,
            NgayTao = DateTime.Now
        });
        dbContext.BienTheSanPhams.Add(new BienTheSanPham
        {
            IdbienThe = variantId,
            IdsanPham = productId,
            IdmauSac = 1,
            IdkichThuoc = 1,
            Sku = $"REVIEW-{productId}",
            Gia = 150000,
            SoLuongTon = 10,
            SoLuongCanhBao = 2,
            TongDaBan = 1,
            TrangThai = true,
            NgayTao = DateTime.Now,
            RowVersion = BitConverter.GetBytes((long)variantId)
        });
        dbContext.DonHangs.Add(new DonHang
        {
            IddonHang = orderId,
            IdnguoiDung = ownerId,
            TenNguoiNhan = "Review Customer",
            DiaChiGiao = "123 Review Street",
            SoDienThoai = "0123456789",
            TongTienHang = 150000,
            PhiVanChuyen = 0,
            TienGiamGia = 0,
            TongThanhToan = 150000,
            IdphuongThucThanhToan = 1,
            IdtrangThai = statusId,
            NgayTao = DateTime.Now
        });
        dbContext.ChiTietDonHangs.Add(new ChiTietDonHang
        {
            IdchiTietDonHang = orderItemId,
            IddonHang = orderId,
            IdbienThe = variantId,
            SoLuong = 1,
            DonGia = 150000,
            TenSanPham = $"Review Product {productId}",
            TenMau = "Đen",
            TenKichThuoc = "M"
        });

        if (addExistingReview)
        {
            dbContext.DanhGia.Add(new DanhGia
            {
                IddanhGia = productId,
                IdnguoiDung = ownerId,
                IdsanPham = productId,
                IdchiTietDonHang = orderItemId,
                DiemSo = 4,
                NoiDung = "Đánh giá đã có",
                TrangThai = true,
                NgayTao = DateTime.Now
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task<int> CountReviewsAsync(int productId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        return await dbContext.DanhGia.CountAsync(review =>
            review.IdnguoiDung == 1
            && review.IdsanPham == productId);
    }
}
