using System.Security.Claims;
using System.Text.Json;
using FashionHub.Web.Data;
using FashionHub.Web.Services;
using FashionHub.Web.ViewModels.Cart;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FashionHub.Tests.Services;

public class CartServiceTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> factory;

    public CartServiceTests(CustomWebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task AddAsync_AuthenticatedCustomer_PersistsCartInDatabase()
    {
        using var scope = factory.Services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = CreateAuthenticatedContext(userId: 1);
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var result = await cartService.AddAsync(variantId: 1, quantity: 2);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(
            item => item.VariantId == 1 && item.Quantity == 2);
        dbContext.GioHangs.Should().ContainSingle(
            item => item.IdnguoiDung == 1 && item.IdbienThe == 1 && item.SoLuong == 2);
    }

    [Fact]
    public async Task MergeGuestCartAsync_MovesSessionCartIntoDatabase()
    {
        using var scope = factory.Services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = CreateAuthenticatedContext(userId: 1);
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.GioHangs.RemoveRange(dbContext.GioHangs.Where(item => item.IdnguoiDung == 1));
        await dbContext.SaveChangesAsync();

        var guestCart = new List<CartItemViewModel>
        {
            new() { IdbienThe = 1, SoLuong = 3 }
        };
        accessor.HttpContext.Session.SetString(
            CartService.CartSessionKey,
            JsonSerializer.Serialize(guestCart));

        await cartService.MergeGuestCartAsync(userId: 1);

        dbContext.GioHangs.Should().ContainSingle(
            item => item.IdnguoiDung == 1 && item.IdbienThe == 1 && item.SoLuong == 3);
        accessor.HttpContext.Session.GetString(CartService.CartSessionKey).Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_StaleAuthenticatedUser_DoesNotInsertCartRow()
    {
        using var scope = factory.Services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = CreateAuthenticatedContext(userId: 999);
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var result = await cartService.AddAsync(variantId: 1, quantity: 1);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("Phiên đăng nhập");
        dbContext.GioHangs.Should().NotContain(item => item.IdnguoiDung == 999);
    }

    private static DefaultHttpContext CreateAuthenticatedContext(int userId)
    {
        var context = new DefaultHttpContext
        {
            Session = new TestSession(),
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) },
                authenticationType: "Test"))
        };

        return context;
    }

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> values = new();

        public bool IsAvailable => true;

        public string Id { get; } = Guid.NewGuid().ToString("N");

        public IEnumerable<string> Keys => values.Keys;

        public void Clear() => values.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => values.Remove(key);

        public void Set(string key, byte[] value) => values[key] = value;

        public bool TryGetValue(string key, out byte[] value)
        {
            return values.TryGetValue(key, out value!);
        }
    }
}
