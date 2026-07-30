using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FashionHub.Web.Data;
using FashionHub.Web.Domain;
using FashionHub.Web.Models.Generated;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FashionHub.Tests.Areas.Admin;

public class AdminAreaTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> factory;

    public AdminAreaTests(CustomWebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Navigation_AdminLogin_RendersWorkingModuleLinksAndClaims()
    {
        using var client = CreateClient();
        (await client.LoginAsAdminAsync()).StatusCode.Should().Be(HttpStatusCode.Redirect);

        var response = await client.GetAsync("/Admin");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("href=\"/Admin/Orders");
        html.Should().Contain("href=\"/Admin/Products");
        html.Should().Contain("href=\"/Admin/Categories");
        html.Should().Contain("href=\"/Admin/Users");
        html.Should().Contain("href=\"/Admin/Coupons");
        html.Should().Contain("href=\"/Admin/Reports");
        html.Should().Contain("Admin User");
        html.Should().Contain("admin@example.com");
        html.Should().NotContain("asp-area=");
        html.Should().NotContain("asp-controller=");
        html.Should().NotContain("href=\"#\"");
    }

    [Theory]
    [InlineData("/Admin")]
    [InlineData("/Admin/Orders")]
    [InlineData("/Admin/Products")]
    [InlineData("/Admin/Products/Create")]
    [InlineData("/Admin/Products/Edit/1")]
    [InlineData("/Admin/Categories")]
    [InlineData("/Admin/Users")]
    [InlineData("/Admin/Users/Details/1")]
    [InlineData("/Admin/Coupons")]
    [InlineData("/Admin/Coupons/Create")]
    [InlineData("/Admin/Reports")]
    [InlineData("/Admin/Reports/SalesReport")]
    [InlineData("/Admin/Reports/CustomerReport")]
    [InlineData("/Admin/Reports/ProductPerformance")]
    public async Task ModuleRoute_AdminLogin_ReturnsSuccess(string path)
    {
        using var client = CreateClient();
        (await client.LoginAsAdminAsync()).StatusCode.Should().Be(HttpStatusCode.Redirect);

        var response = await client.GetAsync(path);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ProductEdit_RendersExistingVariants()
    {
        using var client = CreateClient();
        await client.LoginAsAdminAsync();

        var html = await client.GetStringAsync("/Admin/Products/Edit/1");

        html.Should().Contain("TEST-001");
        html.Should().Contain("name=\"IDSanPham\"");
        html.Should().Contain("name=\"IDMauSac\"");
        html.Should().Contain("name=\"IDKichThuoc\"");
        html.Should().Contain("name=\"SoLuongTon\"");
    }

    [Fact]
    public async Task OrderDetails_PendingOrder_RendersAllowedStatusTransitions()
    {
        using var isolatedFactory = new CustomWebApplicationFactory<Program>();
        using var client = CreateClient(isolatedFactory);
        await client.LoginAsAdminAsync();
        var orderId = await SeedPendingOrderAsync(isolatedFactory);

        var response = await client.GetAsync($"/Admin/Orders/Details/{orderId}");
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        html.Should().Contain($"Đơn hàng #{orderId}");
        html.Should().Contain($"value=\"{OrderStatusIds.Pending}\"");
        html.Should().Contain($"value=\"{OrderStatusIds.Confirmed}\"");
        html.Should().Contain($"value=\"{OrderStatusIds.Cancelled}\"");
        html.Should().NotContain($"value=\"{OrderStatusIds.Shipping}\"");
    }

    [Fact]
    public async Task ConfirmOrder_PendingOrder_UpdatesStatusAndWritesHistory()
    {
        using var isolatedFactory = new CustomWebApplicationFactory<Program>();
        using var client = CreateClient(isolatedFactory);
        await client.LoginAsAdminAsync();
        var orderId = await SeedPendingOrderAsync(isolatedFactory);
        var token = await client.GetAntiforgeryTokenAsync("/Admin/Orders");

        var response = await client.PostAsync(
            "/Admin/Orders/ConfirmOrder",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["id"] = orderId.ToString(),
                ["__RequestVerificationToken"] = token
            }));
        var payload = await response.Content.ReadFromJsonAsync<ConfirmOrderResponse>();

        response.EnsureSuccessStatusCode();
        payload!.Success.Should().BeTrue();

        using var scope = isolatedFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await db.DonHangs
            .Where(order => order.IddonHang == orderId)
            .Select(order => order.IdtrangThai)
            .SingleAsync()).Should().Be(OrderStatusIds.Confirmed);
        (await db.LichSuDonHangs.AnyAsync(history =>
            history.IddonHang == orderId
            && history.IdtrangThaiCu == OrderStatusIds.Pending
            && history.IdtrangThaiMoi == OrderStatusIds.Confirmed))
            .Should().BeTrue();
        var audit = await db.AdminActivityLogs.SingleAsync(log =>
            log.HanhDong == "UPDATE_STATUS"
            && log.IdbanGhi == orderId.ToString());
        using var oldData = JsonDocument.Parse(audit.DuLieuCu!);
        using var newData = JsonDocument.Parse(audit.DuLieuMoi!);
        oldData.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
        oldData.RootElement.GetProperty("StatusId").GetInt32()
            .Should().Be(OrderStatusIds.Pending);
        newData.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
        newData.RootElement.GetProperty("StatusId").GetInt32()
            .Should().Be(OrderStatusIds.Confirmed);
    }

    [Fact]
    public async Task AdminRoot_CustomerRole_RedirectsToAccessDenied()
    {
        using var client = CreateClient();
        await client.LoginAsCustomerAsync();

        var response = await client.GetAsync("/Admin");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/AccessDenied");
    }

    [Fact]
    public async Task ToggleUserStatus_WithoutAntiforgeryToken_ReturnsBadRequest()
    {
        using var client = CreateClient();
        await client.LoginAsAdminAsync();

        var response = await client.PostAsync(
            "/Admin/Users/ToggleStatus",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["id"] = "1"
            }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ToggleUserStatus_WithAntiforgeryToken_RotatesSecurityStamp()
    {
        using var isolatedFactory = new CustomWebApplicationFactory<Program>();
        using var client = isolatedFactory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false
            });
        await client.LoginAsAdminAsync();
        var token = await client.GetAntiforgeryTokenAsync("/Admin/Users");

        Guid oldStamp;
        using (var scope = isolatedFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            oldStamp = await db.NguoiDungs
                .Where(user => user.IdnguoiDung == 1)
                .Select(user => user.SecurityStamp)
                .SingleAsync();
        }

        var response = await client.PostAsync(
            "/Admin/Users/ToggleStatus",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["id"] = "1",
                ["__RequestVerificationToken"] = token
            }));
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ToggleStatusResponse>();

        payload!.Success.Should().BeTrue();
        using var verificationScope = isolatedFactory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var newStamp = await verificationDb.NguoiDungs
            .Where(user => user.IdnguoiDung == 1)
            .Select(user => user.SecurityStamp)
            .SingleAsync();
        newStamp.Should().NotBe(oldStamp);
    }

    [Fact]
    public async Task ExistingAdminSession_WhenDatabaseRoleChanges_IsRejected()
    {
        using var isolatedFactory = new CustomWebApplicationFactory<Program>();
        using var client = isolatedFactory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false
            });
        await client.LoginAsAdminAsync();

        using (var scope = isolatedFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var admin = await db.NguoiDungs.SingleAsync(user =>
                user.Email == "admin@example.com");
            admin.IdvaiTro = 2;
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync("/Admin");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }

    private HttpClient CreateClient() =>
        CreateClient(factory);

    private static HttpClient CreateClient(
        CustomWebApplicationFactory<Program> testFactory) =>
        testFactory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false
            });

    private static async Task<int> SeedPendingOrderAsync(
        CustomWebApplicationFactory<Program> testFactory)
    {
        using var scope = testFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var order = new DonHang
        {
            IdnguoiDung = 1,
            TenNguoiNhan = "Order Test User",
            DiaChiGiao = "123 Test Street",
            SoDienThoai = "0123456789",
            TongTienHang = 100000,
            PhiVanChuyen = 30000,
            TienGiamGia = 0,
            TongThanhToan = 130000,
            IdphuongThucThanhToan = 1,
            IdtrangThai = OrderStatusIds.Pending,
            NgayTao = DateTime.Now
        };
        db.DonHangs.Add(order);
        await db.SaveChangesAsync();
        return order.IddonHang;
    }

    private sealed record ToggleStatusResponse(bool Success, bool NewStatus, string Message);

    private sealed record ConfirmOrderResponse(bool Success, string Message);
}
