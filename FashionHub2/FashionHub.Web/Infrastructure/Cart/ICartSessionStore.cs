namespace FashionHub.Web.Infrastructure.Cart;

public sealed record CartSessionItem(int IdbienThe, int SoLuong);

public interface ICartSessionStore
{
    IReadOnlyList<CartSessionItem> Load();

    void Save(IReadOnlyCollection<CartSessionItem> items);

    void Clear();
}
