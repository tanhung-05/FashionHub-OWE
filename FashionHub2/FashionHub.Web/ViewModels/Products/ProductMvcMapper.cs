using FashionHub.Web.Application.Products;
using FashionHub.Web.Models.Generated;
using Newtonsoft.Json;

namespace FashionHub.Web.ViewModels.Products;

public static class ProductMvcMapper
{
    public static ProductCardViewModel ToCard(ProductSummaryDto product)
    {
        return new ProductCardViewModel
        {
            IDSanPham = product.Id,
            TenSanPham = product.Name,
            Gia = product.Price,
            GiaKhuyenMai = product.SalePrice,
            NgayBatDauKM = product.SaleStart,
            NgayKetThucKM = product.SaleEnd,
            AnhChinhURL = product.ThumbnailUrl,
            IsOutStock = !product.IsAvailable
        };
    }

    public static ProductDetailViewModel ToDetail(ProductDetailDto product)
    {
        var variants = product.Variants
            .Select(variant => new ProductVariantViewModel
            {
                IDBienThe = variant.Id,
                IDMauSac = variant.ColorId,
                IDKichThuoc = variant.SizeId,
                Gia = variant.Price,
                GiaKhuyenMai = product.SalePrice,
                SoLuongTon = variant.StockQuantity,
                Sku = variant.Sku,
                HinhAnhIDs = variant.ImageIds.ToList()
            })
            .ToList();

        return new ProductDetailViewModel
        {
            IDSanPham = product.Id,
            TenSanPham = product.Name,
            MoTa = product.Description,
            IDDanhMuc = product.CategoryId ?? 0,
            TenDanhMuc = product.CategoryName ?? string.Empty,
            TenThuongHieu = product.BrandName,
            Gia = product.Price,
            GiaKhuyenMai = product.SalePrice,
            NgayBatDauKM = product.SaleStart,
            NgayKetThucKM = product.SaleEnd,
            IsOutStock = !product.IsAvailable,
            AvailableColors = product.Variants
                .Where(variant => variant.ColorId.HasValue)
                .GroupBy(variant => variant.ColorId!.Value)
                .Select(group => new MauSac
                {
                    IdmauSac = group.Key,
                    TenMau = group.First().ColorName ?? string.Empty,
                    MaMauHex = group.First().ColorHex
                })
                .ToList(),
            AvailableSizes = product.Variants
                .Where(variant => variant.SizeId.HasValue)
                .GroupBy(variant => variant.SizeId!.Value)
                .Select(group => new KichThuoc
                {
                    IdkichThuoc = group.Key,
                    TenKichThuoc = group.First().SizeName ?? string.Empty
                })
                .ToList(),
            AllImages = product.Images
                .Select(image => new HinhAnh
                {
                    IdhinhAnh = image.Id,
                    DuongDan = image.Url,
                    MoTa = image.AltText
                })
                .ToList(),
            VariantsJson = JsonConvert.SerializeObject(variants),
            RelatedProducts = product.RelatedProducts.Select(ToCard).ToList()
        };
    }

    public static List<DanhMuc> ToCategories(ProductFilterOptionsDto filters)
    {
        return filters.Categories
            .Select(category => new DanhMuc
            {
                IddanhMuc = category.Id,
                TenDanhMuc = category.Name,
                IddanhMucCha = category.ParentId
            })
            .ToList();
    }

    public static List<MauSac> ToColors(ProductFilterOptionsDto filters)
    {
        return filters.Colors
            .Select(color => new MauSac
            {
                IdmauSac = color.Id,
                TenMau = color.Name,
                MaMauHex = color.Hex
            })
            .ToList();
    }

    public static List<KichThuoc> ToSizes(ProductFilterOptionsDto filters)
    {
        return filters.Sizes
            .Select(size => new KichThuoc
            {
                IdkichThuoc = size.Id,
                TenKichThuoc = size.Name
            })
            .ToList();
    }
}
