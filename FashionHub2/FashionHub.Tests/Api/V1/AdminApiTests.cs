using System.Net;
using System.Net.Http.Json;
using FashionHub.Web.Application.Admin;
using FashionHub.Web.Application.Cart;
using FashionHub.Web.Application.Common;
using FashionHub.Web.Application.Orders;
using FashionHub.Web.Data;
using FashionHub.Web.Domain;
using FashionHub.Web.Models.Generated;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FashionHub.Tests.Api.V1;

public class AdminApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> factory;

    public AdminApiTests(CustomWebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task AdminEndpoints_CustomerRole_ReturnsForbidden()
    {
        using var client = CreateClient();
        await client.LoginAsCustomerAsync();

        var response = await client.GetAsync("/api/v1/admin/products");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Products_AdminCanCreateUpdateAndSoftDelete()
    {
        using var client = CreateClient();
        await client.LoginAsAdminAsync();
        var createRequest = new SaveAdminProductRequest
        {
            Name = "API Product",
            Slug = $"api-product-{Guid.NewGuid():N}",
            Description = "Created by integration test",
            Price = 250000,
            CategoryId = 1,
            BrandId = 1,
            IsActive = true
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/admin/products",
            createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await createResponse.Content.ReadFromJsonAsync<AdminProductDto>();

        createRequest.Name = "Updated API Product";
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/admin/products/{product!.Id}",
            createRequest);
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<AdminProductDto>();
        updated!.Name.Should().Be("Updated API Product");

        var deleteResponse = await client.DeleteAsync(
            $"/api/v1/admin/products/{product.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Products_AdminListIsPaged()
    {
        using var client = CreateClient();
        await client.LoginAsAdminAsync();

        var response = await client.GetAsync(
            "/api/v1/admin/products?pageNumber=1&pageSize=10");

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<AdminProductDto>>();
        page!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Orders_AdminCanApplyValidStatusTransition()
    {
        using var customerClient = CreateClient();
        await customerClient.LoginAsCustomerAsync();
        await customerClient.DeleteAsync("/api/v1/cart");
        await customerClient.PostAsJsonAsync(
            "/api/v1/cart/items",
            new AddCartItemRequest { VariantId = 1, Quantity = 1 });
        var createOrderResponse = await customerClient.PostAsJsonAsync(
            "/api/v1/orders",
            new CreateOrderRequest { AddressId = 1, PaymentMethodId = 1 });
        createOrderResponse.EnsureSuccessStatusCode();
        var order = await createOrderResponse.Content.ReadFromJsonAsync<OrderDetailDto>();

        using var adminClient = CreateClient();
        await adminClient.LoginAsAdminAsync();
        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/v1/admin/orders/{order!.Id}/status",
            new UpdateOrderStatusRequest { StatusId = 1 });

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<AdminOrderDetailDto>();
        updated!.Order.StatusId.Should().Be(1);
    }

    [Fact]
    public async Task Orders_AdminCannotConfirmUnpaidVnPayOrder()
    {
        using var isolatedFactory = new CustomWebApplicationFactory<Program>();
        int orderId;
        using (var scope = isolatedFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.PhuongThucThanhToans.Add(new PhuongThucThanhToan
            {
                IdphuongThucThanhToan = 2,
                MaPhuongThuc = PaymentMethodCodes.VnPay,
                TenPhuongThuc = "VNPAY",
                TrangThai = true
            });
            var order = new DonHang
            {
                IdnguoiDung = 1,
                TenNguoiNhan = "VNPAY Test User",
                DiaChiGiao = "123 Test Street",
                SoDienThoai = "0123456789",
                TongTienHang = 100000,
                PhiVanChuyen = 30000,
                TongThanhToan = 130000,
                IdphuongThucThanhToan = 2,
                IdtrangThai = OrderStatusIds.Pending,
                TrangThaiThanhToan = PaymentStatusIds.Pending,
                NgayTao = DateTime.Now
            };
            db.DonHangs.Add(order);
            await db.SaveChangesAsync();
            orderId = order.IddonHang;
        }

        using var client = isolatedFactory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false
            });
        await client.LoginAsAdminAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/admin/orders/{orderId}/status",
            new UpdateOrderStatusRequest { StatusId = OrderStatusIds.Confirmed });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var verificationScope = isolatedFactory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        (await verificationDb.DonHangs
            .Where(order => order.IddonHang == orderId)
            .Select(order => order.IdtrangThai)
            .SingleAsync()).Should().Be(OrderStatusIds.Pending);
    }

    [Fact]
    public async Task Dashboard_InvalidDateRange_ReturnsBadRequest()
    {
        using var client = CreateClient();
        await client.LoginAsAdminAsync();

        var response = await client.GetAsync(
            "/api/v1/admin/reports/dashboard?fromDate=2026-08-02&toDate=2026-08-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private HttpClient CreateClient() =>
        factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false
            });
}
