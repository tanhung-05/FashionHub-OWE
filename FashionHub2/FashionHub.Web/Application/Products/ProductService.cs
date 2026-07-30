using FashionHub.Web.Application.Common;
using FashionHub.Web.Data;
using FashionHub.Web.Models.Generated;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Application.Products;

public sealed class ProductService : IProductService
{
    private const string PlaceholderImage = "/images/products/aothun1_den_boxy.jpg";
    private readonly ApplicationDbContext dbContext;

    public ProductService(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<ServiceResult<PagedResult<ProductSummaryDto>>> GetProductsAsync(
        ProductQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        if (query.MinPrice.HasValue
            && query.MaxPrice.HasValue
            && query.MinPrice > query.MaxPrice)
        {
            return ServiceResult<PagedResult<ProductSummaryDto>>.Failure(
                ServiceErrorType.Validation,
                "invalid-price-range",
                "Minimum price cannot be greater than maximum price.");
        }

        var now = DateTime.Now;
        var products = BuildActiveProductsQuery();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            products = products.Where(product =>
                product.TenSanPham.ToLower().Contains(search));
        }

        if (query.CategoryId.HasValue)
        {
            products = products.Where(product =>
                product.IddanhMuc == query.CategoryId.Value
                || (product.IddanhMucNavigation != null
                    && product.IddanhMucNavigation.IddanhMucCha == query.CategoryId.Value));
        }

        if (query.BrandIds.Count > 0)
        {
            products = products.Where(product =>
                product.IdthuongHieu.HasValue
                && query.BrandIds.Contains(product.IdthuongHieu.Value));
        }

        if (query.ColorIds.Count > 0)
        {
            products = products.Where(product => product.BienTheSanPhams.Any(variant =>
                variant.TrangThai
                && variant.DeletedAt == null
                && variant.IdmauSac.HasValue
                && query.ColorIds.Contains(variant.IdmauSac.Value)));
        }

        if (query.SizeIds.Count > 0)
        {
            products = products.Where(product => product.BienTheSanPhams.Any(variant =>
                variant.TrangThai
                && variant.DeletedAt == null
                && variant.IdkichThuoc.HasValue
                && query.SizeIds.Contains(variant.IdkichThuoc.Value)));
        }

        if (query.InStock.HasValue)
        {
            products = query.InStock.Value
                ? products.Where(product => product.BienTheSanPhams.Any(variant =>
                    variant.TrangThai
                    && variant.DeletedAt == null
                    && variant.SoLuongTon > 0))
                : products.Where(product => !product.BienTheSanPhams.Any(variant =>
                    variant.TrangThai
                    && variant.DeletedAt == null
                    && variant.SoLuongTon > 0));
        }

        if (query.OnSaleOnly)
        {
            products = products.Where(product =>
                product.GiaKhuyenMai.HasValue
                && product.GiaKhuyenMai < product.Gia
                && product.NgayBatDauKm <= now
                && product.NgayKetThucKm >= now);
        }

        if (query.MinPrice.HasValue)
        {
            products = products.Where(product =>
                (product.GiaKhuyenMai.HasValue
                 && product.GiaKhuyenMai < product.Gia
                 && product.NgayBatDauKm <= now
                 && product.NgayKetThucKm >= now
                    ? product.GiaKhuyenMai.Value
                    : product.Gia) >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            products = products.Where(product =>
                (product.GiaKhuyenMai.HasValue
                 && product.GiaKhuyenMai < product.Gia
                 && product.NgayBatDauKm <= now
                 && product.NgayKetThucKm >= now
                    ? product.GiaKhuyenMai.Value
                    : product.Gia) <= query.MaxPrice.Value);
        }

        products = ApplySorting(products, query, now);

        var totalItems = await products.CountAsync(cancellationToken);
        var items = await ProjectSummaries(products, now)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return ServiceResult<PagedResult<ProductSummaryDto>>.Success(
            new PagedResult<ProductSummaryDto>(
                items,
                query.PageNumber,
                query.PageSize,
                totalItems));
    }

    public async Task<ServiceResult<ProductDetailDto>> GetProductAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        if (productId <= 0)
        {
            return ServiceResult<ProductDetailDto>.Failure(
                ServiceErrorType.Validation,
                "invalid-product-id",
                "Product id must be greater than zero.");
        }

        var product = await BuildActiveProductsQuery()
            .Include(item => item.IddanhMucNavigation)
            .Include(item => item.IdthuongHieuNavigation)
            .Include(item => item.BienTheSanPhams)
                .ThenInclude(variant => variant.IdmauSacNavigation)
            .Include(item => item.BienTheSanPhams)
                .ThenInclude(variant => variant.IdkichThuocNavigation)
            .Include(item => item.BienTheSanPhams)
                .ThenInclude(variant => variant.HinhAnhBienThes)
                    .ThenInclude(link => link.IdhinhAnhNavigation)
            .FirstOrDefaultAsync(item => item.IdsanPham == productId, cancellationToken);

        if (product == null)
        {
            return ServiceResult<ProductDetailDto>.Failure(
                ServiceErrorType.NotFound,
                "product-not-found",
                $"Product with id {productId} was not found.");
        }

        var variants = product.BienTheSanPhams
            .Where(variant => variant.TrangThai && variant.DeletedAt == null)
            .OrderBy(variant => variant.IdbienThe)
            .Select(variant => new ProductVariantDto(
                variant.IdbienThe,
                variant.Sku,
                variant.IdmauSac,
                variant.IdmauSacNavigation?.TenMau,
                variant.IdmauSacNavigation?.MaMauHex,
                variant.IdkichThuoc,
                variant.IdkichThuocNavigation?.TenKichThuoc,
                variant.Gia > 0 ? variant.Gia : product.Gia,
                variant.SoLuongTon,
                variant.HinhAnhBienThes
                    .OrderBy(link => link.ThuTuHienThi)
                    .Select(link => link.IdhinhAnh)
                    .ToList()))
            .ToList();

        var images = product.BienTheSanPhams
            .Where(variant => variant.TrangThai && variant.DeletedAt == null)
            .SelectMany(variant => variant.HinhAnhBienThes)
            .OrderByDescending(link => link.LaAnhChinh)
            .ThenBy(link => link.ThuTuHienThi)
            .Select(link => link.IdhinhAnhNavigation)
            .DistinctBy(image => image.IdhinhAnh)
            .Select(image => new ProductImageDto(
                image.IdhinhAnh,
                image.DuongDan,
                image.MoTa))
            .ToList();

        var now = DateTime.Now;
        var relatedProducts = await ProjectSummaries(
                BuildActiveProductsQuery()
                    .Where(item =>
                        item.IdsanPham != productId
                        && item.IddanhMuc == product.IddanhMuc)
                    .OrderByDescending(item => item.NgayTao)
                    .ThenByDescending(item => item.IdsanPham),
                now)
            .Take(4)
            .ToListAsync(cancellationToken);

        var reviews = await dbContext.DanhGia
            .AsNoTracking()
            .Where(review =>
                review.IdsanPham == productId
                && review.TrangThai
                && review.DeletedAt == null)
            .OrderByDescending(review => review.NgayTao)
            .Select(review => new ProductReviewDto(
                review.IddanhGia,
                review.IdnguoiDungNavigation.HoTen,
                review.DiemSo,
                review.NoiDung,
                review.NgayTao))
            .ToListAsync(cancellationToken);

        var saleIsActive = IsSaleActive(product, now);
        var stockQuantity = variants.Sum(variant => variant.StockQuantity);
        var detail = new ProductDetailDto(
            product.IdsanPham,
            product.TenSanPham,
            product.Slug,
            product.MoTa,
            product.Gia,
            product.GiaKhuyenMai,
            saleIsActive ? product.GiaKhuyenMai!.Value : product.Gia,
            product.NgayBatDauKm,
            product.NgayKetThucKm,
            product.IddanhMuc,
            product.IddanhMucNavigation?.TenDanhMuc,
            product.IdthuongHieuNavigation?.TenThuongHieu,
            stockQuantity,
            stockQuantity > 0,
            product.NgayTao,
            product.NgayCapNhat,
            images,
            variants,
            reviews,
            relatedProducts);

        return ServiceResult<ProductDetailDto>.Success(detail);
    }

    public async Task<ServiceResult<ProductFilterOptionsDto>> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await dbContext.DanhMucs
            .AsNoTracking()
            .Where(category => category.TrangThai && category.DeletedAt == null)
            .OrderBy(category => category.TenDanhMuc)
            .Select(category => new CategoryOptionDto(
                category.IddanhMuc,
                category.TenDanhMuc,
                category.IddanhMucCha))
            .ToListAsync(cancellationToken);

        var colors = await dbContext.MauSacs
            .AsNoTracking()
            .OrderBy(color => color.TenMau)
            .Select(color => new ColorOptionDto(
                color.IdmauSac,
                color.TenMau,
                color.MaMauHex))
            .ToListAsync(cancellationToken);

        var brands = await dbContext.ThuongHieus
            .AsNoTracking()
            .Where(brand => brand.TrangThai && brand.DeletedAt == null)
            .OrderBy(brand => brand.TenThuongHieu)
            .Select(brand => new BrandOptionDto(
                brand.IdthuongHieu,
                brand.TenThuongHieu))
            .ToListAsync(cancellationToken);

        var sizes = await dbContext.KichThuocs
            .AsNoTracking()
            .OrderBy(size => size.IdkichThuoc)
            .Select(size => new SizeOptionDto(
                size.IdkichThuoc,
                size.TenKichThuoc))
            .ToListAsync(cancellationToken);

        return ServiceResult<ProductFilterOptionsDto>.Success(
            new ProductFilterOptionsDto(categories, brands, colors, sizes));
    }

    private IQueryable<SanPham> BuildActiveProductsQuery()
    {
        return dbContext.SanPhams
            .AsNoTracking()
            .Where(product => product.TrangThai && product.DeletedAt == null);
    }

    private static IQueryable<SanPham> ApplySorting(
        IQueryable<SanPham> products,
        ProductQueryParameters query,
        DateTime now)
    {
        var descending = query.SortDirection.Equals(
            "desc",
            StringComparison.OrdinalIgnoreCase);

        return query.SortBy.ToLowerInvariant() switch
        {
            "name" => descending
                ? products.OrderByDescending(product => product.TenSanPham)
                : products.OrderBy(product => product.TenSanPham),
            "price" => descending
                ? products.OrderByDescending(product =>
                    product.GiaKhuyenMai.HasValue
                    && product.GiaKhuyenMai < product.Gia
                    && product.NgayBatDauKm <= now
                    && product.NgayKetThucKm >= now
                        ? product.GiaKhuyenMai.Value
                        : product.Gia)
                : products.OrderBy(product =>
                    product.GiaKhuyenMai.HasValue
                    && product.GiaKhuyenMai < product.Gia
                    && product.NgayBatDauKm <= now
                    && product.NgayKetThucKm >= now
                        ? product.GiaKhuyenMai.Value
                        : product.Gia),
            _ => descending
                ? products.OrderByDescending(product => product.NgayTao)
                    .ThenByDescending(product => product.IdsanPham)
                : products.OrderBy(product => product.NgayTao)
                    .ThenBy(product => product.IdsanPham)
        };
    }

    private static IQueryable<ProductSummaryDto> ProjectSummaries(
        IQueryable<SanPham> products,
        DateTime now)
    {
        return products.Select(product => new ProductSummaryDto(
            product.IdsanPham,
            product.TenSanPham,
            product.Slug,
            product.Gia,
            product.GiaKhuyenMai,
            product.GiaKhuyenMai.HasValue
            && product.GiaKhuyenMai < product.Gia
            && product.NgayBatDauKm <= now
            && product.NgayKetThucKm >= now
                ? product.GiaKhuyenMai.Value
                : product.Gia,
            product.NgayBatDauKm,
            product.NgayKetThucKm,
            product.BienTheSanPhams
                .Where(variant => variant.TrangThai && variant.DeletedAt == null)
                .SelectMany(variant => variant.HinhAnhBienThes)
                .OrderByDescending(link => link.LaAnhChinh)
                .ThenBy(link => link.ThuTuHienThi)
                .Select(link => link.IdhinhAnhNavigation.DuongDan)
                .FirstOrDefault() ?? PlaceholderImage,
            product.IddanhMuc,
            product.IddanhMucNavigation != null
                ? product.IddanhMucNavigation.TenDanhMuc
                : null,
            product.BienTheSanPhams
                .Where(variant => variant.TrangThai && variant.DeletedAt == null)
                .Sum(variant => (int?)variant.SoLuongTon) ?? 0,
            product.BienTheSanPhams.Any(variant =>
                variant.TrangThai
                && variant.DeletedAt == null
                && variant.SoLuongTon > 0),
            product.NgayTao));
    }

    private static bool IsSaleActive(SanPham product, DateTime now)
    {
        return product.GiaKhuyenMai.HasValue
            && product.GiaKhuyenMai < product.Gia
            && product.NgayBatDauKm <= now
            && product.NgayKetThucKm >= now;
    }
}
