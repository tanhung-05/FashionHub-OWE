using FashionHub.Web.Application.Authentication;
using FashionHub.Web.Application.Cart;
using FashionHub.Web.Application.Common;
using FashionHub.Web.Data;
using FashionHub.Web.Infrastructure.Cart;
using FashionHub.Web.Models.Generated;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Services;

public sealed class CartService : ICartService
{
    public const string CartSessionKey = "CartSession";

    private readonly ApplicationDbContext dbContext;
    private readonly ICurrentUserService currentUser;
    private readonly ICartSessionStore sessionStore;

    public CartService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        ICartSessionStore sessionStore)
    {
        this.dbContext = dbContext;
        this.currentUser = currentUser;
        this.sessionStore = sessionStore;
    }

    public async Task<ServiceResult<CartDto>> GetCartAsync(
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.UserId.HasValue)
        {
            return ServiceResult<CartDto>.Success(
                await LoadSessionCartAsync(cancellationToken));
        }

        if (!await IsActiveUserAsync(currentUser.UserId.Value, cancellationToken))
        {
            return Failure(
                ServiceErrorType.Unauthorized,
                "stale_authentication",
                "Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.");
        }

        return ServiceResult<CartDto>.Success(
            await LoadDatabaseCartAsync(currentUser.UserId.Value, cancellationToken));
    }

    public async Task<ServiceResult<CartDto>> AddAsync(
        int variantId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity < 1)
        {
            return InvalidQuantity();
        }

        var variant = await GetVariantAsync(variantId, cancellationToken);
        if (variant == null)
        {
            return Failure(
                ServiceErrorType.NotFound,
                "cart_variant_not_found",
                "Sản phẩm không hợp lệ.");
        }

        if (currentUser.UserId.HasValue)
        {
            var userId = currentUser.UserId.Value;
            if (!await IsActiveUserAsync(userId, cancellationToken))
            {
                return Failure(
                    ServiceErrorType.Unauthorized,
                    "stale_authentication",
                    "Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.");
            }

            var cartItem = await dbContext.GioHangs.FindAsync(
                new object[] { userId, variantId },
                cancellationToken);
            var newQuantity = (cartItem?.SoLuong ?? 0) + quantity;
            if (newQuantity > variant.SoLuongTon)
            {
                return InsufficientStock(variant.SoLuongTon);
            }

            if (cartItem == null)
            {
                dbContext.GioHangs.Add(new GioHang
                {
                    IdnguoiDung = userId,
                    IdbienThe = variantId,
                    SoLuong = quantity,
                    NgayThem = DateTime.Now,
                    NgayCapNhat = DateTime.Now
                });
            }
            else
            {
                cartItem.SoLuong = newQuantity;
                cartItem.NgayCapNhat = DateTime.Now;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return ServiceResult<CartDto>.Success(
                await LoadDatabaseCartAsync(userId, cancellationToken));
        }

        var items = sessionStore.Load().ToList();
        var existingItem = items.FirstOrDefault(item => item.IdbienThe == variantId);
        var newSessionQuantity = (existingItem?.SoLuong ?? 0) + quantity;
        if (newSessionQuantity > variant.SoLuongTon)
        {
            return InsufficientStock(variant.SoLuongTon);
        }

        if (existingItem == null)
        {
            items.Add(new CartSessionItem(variantId, quantity));
        }
        else
        {
            items[items.IndexOf(existingItem)] = existingItem with
            {
                SoLuong = newSessionQuantity
            };
        }

        sessionStore.Save(items);
        return ServiceResult<CartDto>.Success(
            await LoadSessionCartAsync(cancellationToken));
    }

    public async Task<ServiceResult<CartDto>> UpdateAsync(
        int variantId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity < 1)
        {
            return InvalidQuantity();
        }

        var stock = await dbContext.BienTheSanPhams
            .AsNoTracking()
            .Where(variant =>
                variant.IdbienThe == variantId
                && variant.TrangThai
                && variant.DeletedAt == null
                && variant.IdsanPhamNavigation.TrangThai
                && variant.IdsanPhamNavigation.DeletedAt == null)
            .Select(variant => (int?)variant.SoLuongTon)
            .FirstOrDefaultAsync(cancellationToken);

        if (!stock.HasValue)
        {
            return Failure(
                ServiceErrorType.NotFound,
                "cart_variant_not_found",
                "Sản phẩm không hợp lệ.");
        }

        if (quantity > stock.Value)
        {
            return InsufficientStock(stock.Value);
        }

        if (currentUser.UserId.HasValue)
        {
            var userId = currentUser.UserId.Value;
            var databaseItem = await dbContext.GioHangs.FindAsync(
                new object[] { userId, variantId },
                cancellationToken);
            if (databaseItem == null)
            {
                return CartItemNotFound();
            }

            databaseItem.SoLuong = quantity;
            databaseItem.NgayCapNhat = DateTime.Now;
            await dbContext.SaveChangesAsync(cancellationToken);
            return ServiceResult<CartDto>.Success(
                await LoadDatabaseCartAsync(userId, cancellationToken));
        }

        var items = sessionStore.Load().ToList();
        var existingItem = items.FirstOrDefault(item => item.IdbienThe == variantId);
        if (existingItem == null)
        {
            return CartItemNotFound();
        }

        items[items.IndexOf(existingItem)] = existingItem with { SoLuong = quantity };
        sessionStore.Save(items);
        return ServiceResult<CartDto>.Success(
            await LoadSessionCartAsync(cancellationToken));
    }

    public async Task<ServiceResult<CartDto>> RemoveAsync(
        int variantId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId.HasValue)
        {
            var userId = currentUser.UserId.Value;
            var databaseItem = await dbContext.GioHangs.FindAsync(
                new object[] { userId, variantId },
                cancellationToken);
            if (databaseItem == null)
            {
                return CartItemNotFound();
            }

            dbContext.GioHangs.Remove(databaseItem);
            await dbContext.SaveChangesAsync(cancellationToken);
            return ServiceResult<CartDto>.Success(
                await LoadDatabaseCartAsync(userId, cancellationToken));
        }

        var items = sessionStore.Load().ToList();
        var removed = items.RemoveAll(item => item.IdbienThe == variantId);
        if (removed == 0)
        {
            return CartItemNotFound();
        }

        sessionStore.Save(items);
        return ServiceResult<CartDto>.Success(
            await LoadSessionCartAsync(cancellationToken));
    }

    public async Task<ServiceResult<CartDto>> ClearAsync(
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId.HasValue)
        {
            var databaseItems = await dbContext.GioHangs
                .Where(item => item.IdnguoiDung == currentUser.UserId.Value)
                .ToListAsync(cancellationToken);

            if (databaseItems.Count > 0)
            {
                dbContext.GioHangs.RemoveRange(databaseItems);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        sessionStore.Clear();
        return ServiceResult<CartDto>.Success(new CartDto(Array.Empty<CartItemDto>()));
    }

    public async Task MergeGuestCartAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var guestItems = sessionStore.Load();
        if (guestItems.Count == 0
            || !await IsActiveUserAsync(userId, cancellationToken))
        {
            return;
        }

        var variantIds = guestItems.Select(item => item.IdbienThe).Distinct().ToList();
        var variants = await dbContext.BienTheSanPhams
            .AsNoTracking()
            .Where(variant =>
                variantIds.Contains(variant.IdbienThe)
                && variant.TrangThai
                && variant.DeletedAt == null
                && variant.IdsanPhamNavigation.TrangThai
                && variant.IdsanPhamNavigation.DeletedAt == null)
            .ToDictionaryAsync(variant => variant.IdbienThe, cancellationToken);
        var databaseItems = await dbContext.GioHangs
            .Where(item =>
                item.IdnguoiDung == userId
                && variantIds.Contains(item.IdbienThe))
            .ToDictionaryAsync(item => item.IdbienThe, cancellationToken);

        foreach (var guestItem in guestItems)
        {
            if (!variants.TryGetValue(guestItem.IdbienThe, out var variant)
                || variant.SoLuongTon <= 0)
            {
                continue;
            }

            if (databaseItems.TryGetValue(guestItem.IdbienThe, out var databaseItem))
            {
                databaseItem.SoLuong = Math.Min(
                    databaseItem.SoLuong + guestItem.SoLuong,
                    variant.SoLuongTon);
                databaseItem.NgayCapNhat = DateTime.Now;
            }
            else
            {
                dbContext.GioHangs.Add(new GioHang
                {
                    IdnguoiDung = userId,
                    IdbienThe = guestItem.IdbienThe,
                    SoLuong = Math.Min(guestItem.SoLuong, variant.SoLuongTon),
                    NgayThem = DateTime.Now,
                    NgayCapNhat = DateTime.Now
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        sessionStore.Clear();
    }

    private async Task<CartDto> LoadDatabaseCartAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var databaseItems = await dbContext.GioHangs
            .AsNoTracking()
            .Where(item =>
                item.IdnguoiDung == userId
                && item.IdbienTheNavigation.TrangThai
                && item.IdbienTheNavigation.DeletedAt == null
                && item.IdbienTheNavigation.IdsanPhamNavigation.TrangThai
                && item.IdbienTheNavigation.IdsanPhamNavigation.DeletedAt == null)
            .Include(item => item.IdbienTheNavigation)
                .ThenInclude(variant => variant.IdsanPhamNavigation)
            .Include(item => item.IdbienTheNavigation)
                .ThenInclude(variant => variant.IdmauSacNavigation)
            .Include(item => item.IdbienTheNavigation)
                .ThenInclude(variant => variant.IdkichThuocNavigation)
            .Include(item => item.IdbienTheNavigation)
                .ThenInclude(variant => variant.HinhAnhBienThes)
                    .ThenInclude(image => image.IdhinhAnhNavigation)
            .OrderBy(item => item.NgayThem)
            .ToListAsync(cancellationToken);

        return new CartDto(databaseItems
            .Select(item => MapCartItem(item.IdbienTheNavigation, item.SoLuong))
            .ToList());
    }

    private async Task<CartDto> LoadSessionCartAsync(CancellationToken cancellationToken)
    {
        var sessionItems = sessionStore.Load();
        if (sessionItems.Count == 0)
        {
            return new CartDto(Array.Empty<CartItemDto>());
        }

        var variantIds = sessionItems.Select(item => item.IdbienThe).Distinct().ToList();
        var variants = await dbContext.BienTheSanPhams
            .AsNoTracking()
            .Where(variant =>
                variantIds.Contains(variant.IdbienThe)
                && variant.TrangThai
                && variant.DeletedAt == null
                && variant.IdsanPhamNavigation.TrangThai
                && variant.IdsanPhamNavigation.DeletedAt == null)
            .Include(variant => variant.IdsanPhamNavigation)
            .Include(variant => variant.IdmauSacNavigation)
            .Include(variant => variant.IdkichThuocNavigation)
            .Include(variant => variant.HinhAnhBienThes)
                .ThenInclude(image => image.IdhinhAnhNavigation)
            .ToDictionaryAsync(variant => variant.IdbienThe, cancellationToken);

        var cartItems = sessionItems
            .Where(item => variants.ContainsKey(item.IdbienThe))
            .Select(item => MapCartItem(
                variants[item.IdbienThe],
                Math.Min(item.SoLuong, variants[item.IdbienThe].SoLuongTon)))
            .Where(item => item.Quantity > 0)
            .ToList();

        return new CartDto(cartItems);
    }

    private Task<BienTheSanPham?> GetVariantAsync(
        int variantId,
        CancellationToken cancellationToken)
    {
        return dbContext.BienTheSanPhams
            .Include(variant => variant.IdsanPhamNavigation)
            .Include(variant => variant.IdmauSacNavigation)
            .Include(variant => variant.IdkichThuocNavigation)
            .Include(variant => variant.HinhAnhBienThes)
                .ThenInclude(image => image.IdhinhAnhNavigation)
            .FirstOrDefaultAsync(variant =>
                variant.IdbienThe == variantId
                && variant.TrangThai
                && variant.DeletedAt == null
                && variant.IdsanPhamNavigation.TrangThai
                && variant.IdsanPhamNavigation.DeletedAt == null,
                cancellationToken);
    }

    private Task<bool> IsActiveUserAsync(int userId, CancellationToken cancellationToken)
    {
        return dbContext.NguoiDungs
            .AsNoTracking()
            .AnyAsync(user =>
                user.IdnguoiDung == userId
                && user.TrangThai
                && user.DeletedAt == null,
                cancellationToken);
    }

    private static CartItemDto MapCartItem(BienTheSanPham variant, int quantity)
    {
        return new CartItemDto(
            variant.IdbienThe,
            variant.IdsanPham,
            variant.IdsanPhamNavigation.TenSanPham,
            variant.IdmauSacNavigation?.TenMau,
            variant.IdkichThuocNavigation?.TenKichThuoc,
            GetFinalPrice(variant),
            quantity,
            GetVariantImageUrl(variant),
            variant.SoLuongTon);
    }

    private static decimal GetFinalPrice(BienTheSanPham variant)
    {
        var product = variant.IdsanPhamNavigation;
        var now = DateTime.Now;

        if (product.GiaKhuyenMai.HasValue
            && product.NgayBatDauKm.HasValue
            && product.NgayKetThucKm.HasValue
            && now >= product.NgayBatDauKm.Value
            && now <= product.NgayKetThucKm.Value)
        {
            return product.GiaKhuyenMai.Value;
        }

        return variant.Gia > 0 ? variant.Gia : product.Gia;
    }

    private static string GetVariantImageUrl(BienTheSanPham variant)
    {
        return variant.HinhAnhBienThes
            .OrderByDescending(image => image.LaAnhChinh)
            .ThenBy(image => image.ThuTuHienThi)
            .Select(image => image.IdhinhAnhNavigation.DuongDan)
            .FirstOrDefault()
            ?? "/images/placeholder.png";
    }

    private static ServiceResult<CartDto> InvalidQuantity() =>
        Failure(
            ServiceErrorType.Validation,
            "invalid_cart_quantity",
            "Số lượng không hợp lệ.");

    private static ServiceResult<CartDto> InsufficientStock(int availableStock) =>
        Failure(
            ServiceErrorType.Conflict,
            "insufficient_stock",
            $"Chỉ còn {availableStock} sản phẩm.");

    private static ServiceResult<CartDto> CartItemNotFound() =>
        Failure(
            ServiceErrorType.NotFound,
            "cart_item_not_found",
            "Sản phẩm không có trong giỏ hàng.");

    private static ServiceResult<CartDto> Failure(
        ServiceErrorType type,
        string code,
        string message) =>
        ServiceResult<CartDto>.Failure(type, code, message);
}
