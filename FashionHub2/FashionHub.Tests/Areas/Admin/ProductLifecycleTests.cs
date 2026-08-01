using System.Net;
using System.Text.Json;
using FashionHub.Web.Data;
using FashionHub.Web.Models.Generated;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FashionHub.Tests.Areas.Admin;

public class ProductLifecycleTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> factory;

    public ProductLifecycleTests(CustomWebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task ProductLifecycle_StopSellingAndRestore_PreservesProductData()
    {
        const int productId = 8201;
        const int activeVariantId = 8201;
        const int previouslyDeletedVariantId = 8202;
        var previouslyDeletedAt = DateTime.Now.AddDays(-1);
        await SeedProductAsync(
            productId,
            activeVariantId,
            previouslyDeletedVariantId,
            previouslyDeletedAt);

        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false
            });
        await client.LoginAsAdminAsync();

        var activeListHtml = WebUtility.HtmlDecode(
            await client.GetStringAsync("/Admin/Products"));
        activeListHtml.Should().Contain($"Lifecycle Product {productId}");
        activeListHtml.Should().Contain("title=\"Ngừng kinh doanh\"");

        var stopSellingResponse = await client.PostFormWithAntiforgeryAsync(
            "/Admin/Products/Delete",
            new Dictionary<string, string>
            {
                ["id"] = productId.ToString()
            },
            "/Admin/Products");

        stopSellingResponse.EnsureSuccessStatusCode();
        using (var payload = JsonDocument.Parse(
            await stopSellingResponse.Content.ReadAsStringAsync()))
        {
            payload.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
            payload.RootElement.GetProperty("message").GetString()
                .Should().Contain("ngừng kinh doanh");
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var product = await dbContext.SanPhams
                .AsNoTracking()
                .SingleAsync(item => item.IdsanPham == productId);
            var activeVariant = await dbContext.BienTheSanPhams
                .AsNoTracking()
                .SingleAsync(item => item.IdbienThe == activeVariantId);
            var previouslyDeletedVariant = await dbContext.BienTheSanPhams
                .AsNoTracking()
                .SingleAsync(item => item.IdbienThe == previouslyDeletedVariantId);

            product.DeletedAt.Should().NotBeNull();
            product.TrangThai.Should().BeFalse();
            activeVariant.DeletedAt.Should().Be(product.DeletedAt);
            activeVariant.TrangThai.Should().BeFalse();
            previouslyDeletedVariant.DeletedAt.Should().Be(previouslyDeletedAt);
        }

        var currentListHtml = WebUtility.HtmlDecode(
            await client.GetStringAsync("/Admin/Products"));
        currentListHtml.Should().NotContain($"Lifecycle Product {productId}");

        var archivedListHtml = WebUtility.HtmlDecode(
            await client.GetStringAsync("/Admin/Products?showDeleted=true"));
        archivedListHtml.Should().Contain($"Lifecycle Product {productId}");
        archivedListHtml.Should().Contain("Đã ngừng kinh doanh");
        archivedListHtml.Should().Contain("Khôi phục");
        archivedListHtml.Should().NotContain($"/Admin/Products/Edit/{productId}");

        var restoreResponse = await client.PostFormWithAntiforgeryAsync(
            "/Admin/Products/Restore",
            new Dictionary<string, string>
            {
                ["id"] = productId.ToString()
            },
            "/Admin/Products?showDeleted=true");

        restoreResponse.EnsureSuccessStatusCode();
        using (var payload = JsonDocument.Parse(
            await restoreResponse.Content.ReadAsStringAsync()))
        {
            payload.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
            payload.RootElement.GetProperty("message").GetString()
                .Should().Contain("Ngừng bán");
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var product = await dbContext.SanPhams
                .AsNoTracking()
                .SingleAsync(item => item.IdsanPham == productId);
            var activeVariant = await dbContext.BienTheSanPhams
                .AsNoTracking()
                .SingleAsync(item => item.IdbienThe == activeVariantId);
            var previouslyDeletedVariant = await dbContext.BienTheSanPhams
                .AsNoTracking()
                .SingleAsync(item => item.IdbienThe == previouslyDeletedVariantId);

            product.DeletedAt.Should().BeNull();
            product.TrangThai.Should().BeFalse();
            activeVariant.DeletedAt.Should().BeNull();
            activeVariant.TrangThai.Should().BeTrue();
            previouslyDeletedVariant.DeletedAt.Should().Be(previouslyDeletedAt);
            previouslyDeletedVariant.TrangThai.Should().BeFalse();
            (await dbContext.AdminActivityLogs.CountAsync(log =>
                log.IdbanGhi == productId.ToString()
                && (log.HanhDong == "SOFT_DELETE" || log.HanhDong == "RESTORE")))
                .Should().Be(2);
        }

        var restoredListHtml = WebUtility.HtmlDecode(
            await client.GetStringAsync("/Admin/Products"));
        restoredListHtml.Should().Contain($"Lifecycle Product {productId}");
        restoredListHtml.Should().Contain("Ngừng bán");
    }

    private async Task SeedProductAsync(
        int productId,
        int activeVariantId,
        int previouslyDeletedVariantId,
        DateTime previouslyDeletedAt)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        dbContext.SanPhams.Add(new SanPham
        {
            IdsanPham = productId,
            TenSanPham = $"Lifecycle Product {productId}",
            MoTa = "Product used to verify soft deletion and restoration",
            IddanhMuc = 1,
            IdthuongHieu = 1,
            Gia = 200000,
            TrangThai = true,
            NgayTao = DateTime.Now
        });
        dbContext.BienTheSanPhams.AddRange(
            new BienTheSanPham
            {
                IdbienThe = activeVariantId,
                IdsanPham = productId,
                IdmauSac = 1,
                IdkichThuoc = 1,
                Sku = $"LIFECYCLE-{activeVariantId}",
                Gia = 200000,
                SoLuongTon = 5,
                SoLuongCanhBao = 1,
                TongDaBan = 0,
                TrangThai = true,
                NgayTao = DateTime.Now,
                RowVersion = BitConverter.GetBytes((long)activeVariantId)
            },
            new BienTheSanPham
            {
                IdbienThe = previouslyDeletedVariantId,
                IdsanPham = productId,
                IdmauSac = 1,
                IdkichThuoc = 1,
                Sku = $"LIFECYCLE-{previouslyDeletedVariantId}",
                Gia = 200000,
                SoLuongTon = 0,
                SoLuongCanhBao = 1,
                TongDaBan = 0,
                TrangThai = false,
                NgayTao = DateTime.Now.AddDays(-2),
                NgayCapNhat = previouslyDeletedAt,
                DeletedAt = previouslyDeletedAt,
                RowVersion = BitConverter.GetBytes((long)previouslyDeletedVariantId)
            });

        await dbContext.SaveChangesAsync();
    }
}
