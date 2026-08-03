using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace FashionHub.Tests.Controllers;

public class AccountControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;
    
    public AccountControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
    
    [Fact]
    public async Task Register_Get_ReturnsRegisterPage()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/Account/Register");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Tạo tài khoản");
    }
    
    [Fact]
    public async Task Login_Get_RendersForgotPasswordLink()
    {
        var response = await _client.GetAsync("/Account/Login");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("href=\"/Account/ForgotPassword\"");
    }

    [Fact]
    public async Task Profile_WithoutAuth_RedirectsToLogin()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/Account/Profile");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }
    
    [Fact]
    public async Task OrderHistory_WithoutAuth_RedirectsToLogin()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/Account/OrderHistory");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }
    
    [Fact]
    public async Task Addresses_WithoutAuth_RedirectsToLogin()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/Account/Addresses");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }
    
    [Fact]
    public async Task AccessDenied_ReturnsAccessDeniedPage()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/Account/AccessDenied");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Truy cập bị từ chối");
    }

    [Fact]
    public async Task Login_AsAdmin_RedirectsToAdminDashboard()
    {
        var response = await _client.LoginAsAdminAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString()
            .Should().Be("/Admin");
    }

    [Fact]
    public async Task OrderHistory_AfterLogin_ReturnsOrderHistoryPage()
    {
        await _client.LoginAsCustomerAsync();

        var response = await _client.GetAsync("/Account/OrderHistory");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Lịch sử đơn hàng");
    }

    [Fact]
    public async Task Profile_AfterLogin_ReturnsCompleteProfileDashboard()
    {
        await _client.LoginAsCustomerAsync();

        var response = await _client.GetAsync("/Account/Profile");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("profile-stats");
        html.Should().Contain("Thông tin cá nhân");
        html.Should().Contain("Địa chỉ ưu tiên");
    }

    [Fact]
    public async Task Addresses_AfterLogin_RendersMvcAddressRoutes()
    {
        await _client.LoginAsCustomerAsync();

        var addressesResponse = await _client.GetAsync("/Account/Addresses");
        var createResponse = await _client.GetAsync("/Account/CreateAddress");

        addressesResponse.EnsureSuccessStatusCode();
        createResponse.EnsureSuccessStatusCode();
        var addressesHtml = await addressesResponse.Content.ReadAsStringAsync();
        var createHtml = await createResponse.Content.ReadAsStringAsync();
        addressesHtml.Should().Contain("href=\"/Account/CreateAddress\"");
        addressesHtml.Should().Contain("action=\"/Account/DeleteAddress/1\"");
        addressesHtml.Should().NotContain("href=\"/api/v1/account/addresses\"");
        createHtml.Should().Contain("action=\"/Account/CreateAddress\"");
    }

    [Fact]
    public async Task AddAddressAjax_WithValidData_SavesAddress()
    {
        await _client.LoginAsCustomerAsync();
        var token = await _client.GetAntiforgeryTokenAsync("/Account/CreateAddress");

        var response = await _client.PostAsync(
            "/Account/AddAddressAjax",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["TenNguoiNhan"] = "Nguyen Van Test",
                ["SoDienThoai"] = "0912345678",
                ["ChiTiet"] = "45 Nguyen Hue",
                ["PhuongXa"] = "Phuong Ben Nghe",
                ["QuanHuyen"] = "Quan 1",
                ["TinhThanh"] = "Thanh pho Ho Chi Minh",
                ["LaMacDinh"] = "false",
                ["__RequestVerificationToken"] = token
            }));

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AddressAjaxResponse>();
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.NewAddress.Should().NotBeNull();
        payload.NewAddress!.FullAddress.Should().Contain("45 Nguyen Hue");
    }

    private sealed record AddressAjaxResponse(
        bool Success,
        string Message,
        AddressAjaxItem? NewAddress);

    private sealed record AddressAjaxItem(
        int IddiaChi,
        string TenNguoiNhan,
        string SoDienThoai,
        bool LaMacDinh,
        string FullAddress);
}
