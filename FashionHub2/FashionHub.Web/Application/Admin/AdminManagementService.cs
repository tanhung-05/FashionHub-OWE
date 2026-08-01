using FashionHub.Web.Application.Common;
using FashionHub.Web.Data;
using FashionHub.Web.Domain;
using FashionHub.Web.Models.Generated;
using FashionHub.Web.Utilities;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Application.Admin;

public sealed class AdminManagementService : IAdminManagementService
{
    private const int CustomerRoleId = 2;
    private readonly ApplicationDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public AdminManagementService(
        ApplicationDbContext dbContext,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.timeProvider = timeProvider;
    }

    public async Task<ServiceResult<PagedResult<AdminCategoryDto>>> GetCategoriesAsync(
        AdminManagementQuery query,
        CancellationToken cancellationToken = default)
    {
        var categories = dbContext.DanhMucs
            .AsNoTracking()
            .Where(category => category.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            categories = categories.Where(category =>
                category.TenDanhMuc.Contains(search));
        }

        var total = await categories.CountAsync(cancellationToken);
        var items = await categories
            .OrderBy(category => category.TenDanhMuc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(category => new AdminCategoryDto(
                category.IddanhMuc,
                category.TenDanhMuc,
                category.Slug,
                category.IddanhMucCha,
                category.IddanhMucChaNavigation == null
                    ? null
                    : category.IddanhMucChaNavigation.TenDanhMuc,
                category.SanPhams.Count(product => product.DeletedAt == null),
                category.InverseIddanhMucChaNavigation.Count(child =>
                    child.DeletedAt == null),
                category.TrangThai))
            .ToListAsync(cancellationToken);

        return ServiceResult<PagedResult<AdminCategoryDto>>.Success(
            new PagedResult<AdminCategoryDto>(
                items,
                query.PageNumber,
                query.PageSize,
                total));
    }

    public async Task<ServiceResult<AdminCategoryDto>> GetCategoryAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var category = await FindCategoryAsync(id, track: false, cancellationToken);
        return category is null
            ? NotFound<AdminCategoryDto>("category-not-found", "Category not found.")
            : ServiceResult<AdminCategoryDto>.Success(await MapCategoryAsync(
                category,
                cancellationToken));
    }

    public async Task<ServiceResult<AdminCategoryDto>> CreateCategoryAsync(
        SaveAdminCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateCategoryAsync(
            request,
            excludeId: null,
            cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var category = new DanhMuc
        {
            TenDanhMuc = request.Name.Trim(),
            Slug = await CreateUniqueCategorySlugAsync(
                request.Name,
                excludeId: null,
                cancellationToken),
            IddanhMucCha = request.ParentId,
            TrangThai = request.IsActive
        };
        dbContext.DanhMucs.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<AdminCategoryDto>.Success(
            await MapCategoryAsync(category, cancellationToken));
    }

    public async Task<ServiceResult<AdminCategoryDto>> UpdateCategoryAsync(
        int id,
        SaveAdminCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await FindCategoryAsync(id, track: true, cancellationToken);
        if (category is null)
        {
            return NotFound<AdminCategoryDto>("category-not-found", "Category not found.");
        }

        var validation = await ValidateCategoryAsync(
            request,
            id,
            cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        category.TenDanhMuc = request.Name.Trim();
        category.Slug = await CreateUniqueCategorySlugAsync(
            request.Name,
            id,
            cancellationToken);
        category.IddanhMucCha = request.ParentId;
        category.TrangThai = request.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<AdminCategoryDto>.Success(
            await MapCategoryAsync(category, cancellationToken));
    }

    public async Task<ServiceResult<bool>> DeleteCategoryAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var category = await dbContext.DanhMucs
            .Include(item => item.SanPhams)
            .Include(item => item.InverseIddanhMucChaNavigation)
            .SingleOrDefaultAsync(item =>
                item.IddanhMuc == id && item.DeletedAt == null,
                cancellationToken);
        if (category is null)
        {
            return NotFound<bool>("category-not-found", "Category not found.");
        }

        if (category.SanPhams.Any(product => product.DeletedAt == null))
        {
            return Conflict<bool>(
                "category-has-products",
                "Move or delete products before deleting this category.");
        }

        if (category.InverseIddanhMucChaNavigation.Any(child => child.DeletedAt == null))
        {
            return Conflict<bool>(
                "category-has-children",
                "Delete child categories before deleting this category.");
        }

        category.TrangThai = false;
        category.DeletedAt = Now();
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<PagedResult<AdminCouponDto>>> GetCouponsAsync(
        AdminManagementQuery query,
        CancellationToken cancellationToken = default)
    {
        var coupons = dbContext.MaGiamGia
            .AsNoTracking()
            .Where(coupon => coupon.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            coupons = coupons.Where(coupon =>
                coupon.MaCode.Contains(search)
                || (coupon.TenChuongTrinh != null
                    && coupon.TenChuongTrinh.Contains(search)));
        }

        var total = await coupons.CountAsync(cancellationToken);
        var entities = await coupons
            .OrderByDescending(coupon => coupon.IdmaGiamGia)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        return ServiceResult<PagedResult<AdminCouponDto>>.Success(
            new PagedResult<AdminCouponDto>(
                entities.Select(MapCoupon).ToList(),
                query.PageNumber,
                query.PageSize,
                total));
    }

    public async Task<ServiceResult<AdminCouponDto>> GetCouponAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var coupon = await FindCouponAsync(id, cancellationToken);
        return coupon is null
            ? NotFound<AdminCouponDto>("coupon-not-found", "Coupon not found.")
            : ServiceResult<AdminCouponDto>.Success(MapCoupon(coupon));
    }

    public async Task<ServiceResult<AdminCouponDto>> CreateCouponAsync(
        SaveAdminCouponRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateCouponAsync(
            request,
            excludeId: null,
            usedCount: 0,
            cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var coupon = new MaGiamGium
        {
            MaCode = request.Code.Trim().ToUpperInvariant(),
            TenChuongTrinh = NormalizeOptional(request.Name),
            LoaiGiamGia = request.DiscountType,
            GiaTri = request.Value,
            DonHangToiThieu = request.MinimumOrder,
            GiamToiDa = request.MaximumDiscount,
            SoLuong = request.Quantity,
            DaSuDung = 0,
            NgayBatDau = request.StartsAt,
            NgayKetThuc = request.EndsAt,
            NgayTao = Now(),
            TrangThai = request.IsActive
        };
        dbContext.MaGiamGia.Add(coupon);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<AdminCouponDto>.Success(MapCoupon(coupon));
    }

    public async Task<ServiceResult<AdminCouponDto>> UpdateCouponAsync(
        int id,
        SaveAdminCouponRequest request,
        CancellationToken cancellationToken = default)
    {
        var coupon = await FindCouponAsync(id, cancellationToken);
        if (coupon is null)
        {
            return NotFound<AdminCouponDto>("coupon-not-found", "Coupon not found.");
        }

        var validation = await ValidateCouponAsync(
            request,
            id,
            coupon.DaSuDung,
            cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        coupon.MaCode = request.Code.Trim().ToUpperInvariant();
        coupon.TenChuongTrinh = NormalizeOptional(request.Name);
        coupon.LoaiGiamGia = request.DiscountType;
        coupon.GiaTri = request.Value;
        coupon.DonHangToiThieu = request.MinimumOrder;
        coupon.GiamToiDa = request.MaximumDiscount;
        coupon.SoLuong = request.Quantity;
        coupon.NgayBatDau = request.StartsAt;
        coupon.NgayKetThuc = request.EndsAt;
        coupon.TrangThai = request.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<AdminCouponDto>.Success(MapCoupon(coupon));
    }

    public async Task<ServiceResult<bool>> DeleteCouponAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var coupon = await FindCouponAsync(id, cancellationToken);
        if (coupon is null)
        {
            return NotFound<bool>("coupon-not-found", "Coupon not found.");
        }

        if (coupon.DaSuDung > 0)
        {
            coupon.TrangThai = false;
            coupon.DeletedAt = Now();
        }
        else
        {
            dbContext.MaGiamGia.Remove(coupon);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<AdminCouponDto>> ToggleCouponAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var coupon = await FindCouponAsync(id, cancellationToken);
        if (coupon is null)
        {
            return NotFound<AdminCouponDto>("coupon-not-found", "Coupon not found.");
        }

        if (!coupon.TrangThai
            && (coupon.NgayKetThuc < Now() || coupon.DaSuDung >= coupon.SoLuong))
        {
            return Conflict<AdminCouponDto>(
                "coupon-cannot-activate",
                "An expired or exhausted coupon cannot be activated.");
        }

        coupon.TrangThai = !coupon.TrangThai;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<AdminCouponDto>.Success(MapCoupon(coupon));
    }

    public async Task<ServiceResult<PagedResult<AdminCustomerDto>>> GetCustomersAsync(
        AdminManagementQuery query,
        CancellationToken cancellationToken = default)
    {
        var customers = dbContext.NguoiDungs
            .AsNoTracking()
            .Where(user => user.IdvaiTro == CustomerRoleId && user.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            customers = customers.Where(user =>
                user.HoTen.Contains(search)
                || user.Email.Contains(search)
                || (user.SoDienThoai != null && user.SoDienThoai.Contains(search)));
        }

        var total = await customers.CountAsync(cancellationToken);
        var items = await customers
            .OrderByDescending(user => user.NgayTao)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(user => new AdminCustomerDto(
                user.IdnguoiDung,
                user.HoTen,
                user.Email,
                user.SoDienThoai,
                user.NgayTao,
                user.TrangThai,
                user.DonHangs.Count,
                user.DonHangs
                    .Where(order => order.IdtrangThai == OrderStatusIds.Completed)
                    .Sum(order => (decimal?)order.TongThanhToan) ?? 0))
            .ToListAsync(cancellationToken);
        return ServiceResult<PagedResult<AdminCustomerDto>>.Success(
            new PagedResult<AdminCustomerDto>(
                items,
                query.PageNumber,
                query.PageSize,
                total));
    }

    public async Task<ServiceResult<AdminCustomerDetailDto>> GetCustomerAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.NguoiDungs
            .AsNoTracking()
            .Where(user =>
                user.IdnguoiDung == id
                && user.IdvaiTro == CustomerRoleId
                && user.DeletedAt == null)
            .Select(user => new AdminCustomerDto(
                user.IdnguoiDung,
                user.HoTen,
                user.Email,
                user.SoDienThoai,
                user.NgayTao,
                user.TrangThai,
                user.DonHangs.Count,
                user.DonHangs
                    .Where(order => order.IdtrangThai == OrderStatusIds.Completed)
                    .Sum(order => (decimal?)order.TongThanhToan) ?? 0))
            .SingleOrDefaultAsync(cancellationToken);
        if (customer is null)
        {
            return NotFound<AdminCustomerDetailDto>(
                "customer-not-found",
                "Customer not found.");
        }

        var orders = await dbContext.DonHangs
            .AsNoTracking()
            .Where(order => order.IdnguoiDung == id)
            .OrderByDescending(order => order.NgayTao)
            .Select(order => new AdminCustomerOrderDto(
                order.IddonHang,
                order.NgayTao,
                order.TongThanhToan,
                order.IdtrangThai,
                order.IdtrangThaiNavigation.TenTrangThai))
            .ToListAsync(cancellationToken);
        return ServiceResult<AdminCustomerDetailDto>.Success(
            new AdminCustomerDetailDto(customer, orders));
    }

    public async Task<ServiceResult<AdminCustomerDto>> ToggleCustomerAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.NguoiDungs.SingleOrDefaultAsync(item =>
            item.IdnguoiDung == id
            && item.IdvaiTro == CustomerRoleId
            && item.DeletedAt == null,
            cancellationToken);
        if (user is null)
        {
            return NotFound<AdminCustomerDto>("customer-not-found", "Customer not found.");
        }

        user.TrangThai = !user.TrangThai;
        user.SecurityStamp = Guid.NewGuid();
        user.NgayCapNhat = Now();
        await dbContext.SaveChangesAsync(cancellationToken);

        var detail = await GetCustomerAsync(id, cancellationToken);
        return ServiceResult<AdminCustomerDto>.Success(detail.Value!.Customer);
    }

    private async Task<ServiceResult<AdminCategoryDto>?> ValidateCategoryAsync(
        SaveAdminCategoryRequest request,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var duplicate = await dbContext.DanhMucs.AnyAsync(category =>
            category.DeletedAt == null
            && category.TenDanhMuc == name
            && category.IddanhMucCha == request.ParentId
            && (!excludeId.HasValue || category.IddanhMuc != excludeId.Value),
            cancellationToken);
        if (duplicate)
        {
            return Conflict<AdminCategoryDto>(
                "category-already-exists",
                "A category with this name and parent already exists.");
        }

        if (!request.ParentId.HasValue)
        {
            return null;
        }

        if (request.ParentId == excludeId)
        {
            return Conflict<AdminCategoryDto>(
                "category-parent-cycle",
                "A category cannot be its own parent.");
        }

        var parent = await FindCategoryAsync(
            request.ParentId.Value,
            track: false,
            cancellationToken);
        if (parent is null)
        {
            return NotFound<AdminCategoryDto>(
                "parent-category-not-found",
                "Parent category not found.");
        }

        var ancestorId = parent.IddanhMucCha;
        while (excludeId.HasValue && ancestorId.HasValue)
        {
            if (ancestorId.Value == excludeId.Value)
            {
                return Conflict<AdminCategoryDto>(
                    "category-parent-cycle",
                    "The selected parent would create a category cycle.");
            }

            ancestorId = await dbContext.DanhMucs
                .Where(category => category.IddanhMuc == ancestorId.Value)
                .Select(category => category.IddanhMucCha)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    private async Task<ServiceResult<AdminCouponDto>?> ValidateCouponAsync(
        SaveAdminCouponRequest request,
        int? excludeId,
        int usedCount,
        CancellationToken cancellationToken)
    {
        if (request.StartsAt > request.EndsAt)
        {
            return Validation<AdminCouponDto>(
                "coupon-date-range-invalid",
                "Coupon end date must be after its start date.");
        }

        if (request.DiscountType == CouponTypes.Percentage && request.Value > 100)
        {
            return Validation<AdminCouponDto>(
                "coupon-percentage-invalid",
                "Percentage discount cannot exceed 100.");
        }

        if (request.Quantity < usedCount)
        {
            return Conflict<AdminCouponDto>(
                "coupon-quantity-below-usage",
                "Coupon quantity cannot be lower than its used count.");
        }

        var code = request.Code.Trim().ToUpperInvariant();
        var duplicate = await dbContext.MaGiamGia.AnyAsync(coupon =>
            coupon.DeletedAt == null
            && coupon.MaCode == code
            && (!excludeId.HasValue || coupon.IdmaGiamGia != excludeId.Value),
            cancellationToken);
        return duplicate
            ? Conflict<AdminCouponDto>(
                "coupon-code-already-exists",
                "Coupon code already exists.")
            : null;
    }

    private Task<DanhMuc?> FindCategoryAsync(
        int id,
        bool track,
        CancellationToken cancellationToken)
    {
        IQueryable<DanhMuc> query = dbContext.DanhMucs;
        if (!track)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(category =>
            category.IddanhMuc == id && category.DeletedAt == null,
            cancellationToken);
    }

    private Task<MaGiamGium?> FindCouponAsync(
        int id,
        CancellationToken cancellationToken) =>
        dbContext.MaGiamGia.SingleOrDefaultAsync(coupon =>
            coupon.IdmaGiamGia == id && coupon.DeletedAt == null,
            cancellationToken);

    private async Task<AdminCategoryDto> MapCategoryAsync(
        DanhMuc category,
        CancellationToken cancellationToken)
    {
        var parentName = category.IddanhMucCha.HasValue
            ? await dbContext.DanhMucs
                .Where(parent => parent.IddanhMuc == category.IddanhMucCha.Value)
                .Select(parent => parent.TenDanhMuc)
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        var productCount = await dbContext.SanPhams.CountAsync(product =>
            product.IddanhMuc == category.IddanhMuc && product.DeletedAt == null,
            cancellationToken);
        var childCount = await dbContext.DanhMucs.CountAsync(child =>
            child.IddanhMucCha == category.IddanhMuc && child.DeletedAt == null,
            cancellationToken);
        return new AdminCategoryDto(
            category.IddanhMuc,
            category.TenDanhMuc,
            category.Slug,
            category.IddanhMucCha,
            parentName,
            productCount,
            childCount,
            category.TrangThai);
    }

    private async Task<string> CreateUniqueCategorySlugAsync(
        string name,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        var baseSlug = SlugGenerator.Generate(name);
        var slug = baseSlug;
        var suffix = 2;
        while (await dbContext.DanhMucs.AnyAsync(category =>
            category.Slug == slug
            && category.DeletedAt == null
            && (!excludeId.HasValue || category.IddanhMuc != excludeId.Value),
            cancellationToken))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }

    private static AdminCouponDto MapCoupon(MaGiamGium coupon) => new(
        coupon.IdmaGiamGia,
        coupon.MaCode,
        coupon.TenChuongTrinh,
        coupon.LoaiGiamGia,
        coupon.GiaTri,
        coupon.DonHangToiThieu,
        coupon.GiamToiDa,
        coupon.SoLuong,
        coupon.DaSuDung,
        coupon.NgayBatDau,
        coupon.NgayKetThuc,
        coupon.TrangThai);

    private static ServiceResult<T> NotFound<T>(string code, string message) =>
        ServiceResult<T>.Failure(ServiceErrorType.NotFound, code, message);

    private static ServiceResult<T> Conflict<T>(string code, string message) =>
        ServiceResult<T>.Failure(ServiceErrorType.Conflict, code, message);

    private static ServiceResult<T> Validation<T>(string code, string message) =>
        ServiceResult<T>.Failure(ServiceErrorType.Validation, code, message);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private DateTime Now() => timeProvider.GetLocalNow().DateTime;
}
