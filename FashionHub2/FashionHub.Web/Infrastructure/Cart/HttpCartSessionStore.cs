using System.Text.Json;
using FashionHub.Web.Services;

namespace FashionHub.Web.Infrastructure.Cart;

public sealed class HttpCartSessionStore : ICartSessionStore
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpCartSessionStore(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public IReadOnlyList<CartSessionItem> Load()
    {
        var json = Session.GetString(CartService.CartSessionKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<CartSessionItem>();
        }

        return JsonSerializer.Deserialize<List<CartSessionItem>>(json)
            ?? new List<CartSessionItem>();
    }

    public void Save(IReadOnlyCollection<CartSessionItem> items)
    {
        if (items.Count == 0)
        {
            Clear();
            return;
        }

        Session.SetString(CartService.CartSessionKey, JsonSerializer.Serialize(items));
    }

    public void Clear() => Session.Remove(CartService.CartSessionKey);

    private ISession Session =>
        httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("HTTP session is not available.");
}
