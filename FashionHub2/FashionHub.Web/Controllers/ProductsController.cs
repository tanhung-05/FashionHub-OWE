using FashionHub.Web.Application.Products;
using FashionHub.Web.ViewModels.Products;
using Microsoft.AspNetCore.Mvc;

namespace FashionHub.Web.Controllers;

public class ProductsController : Controller
{
    private readonly IProductService productService;

    public ProductsController(IProductService productService)
    {
        this.productService = productService;
    }

    public async Task<IActionResult> Index(
        string? searchString,
        int? categoryId,
        List<int>? colorIds,
        List<int>? sizeIds,
        string? priceRange,
        string? sortBy,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var query = new ProductQueryParameters
        {
            PageNumber = Math.Max(page, 1),
            PageSize = 9,
            Search = searchString,
            CategoryId = categoryId,
            ColorIds = colorIds ?? new List<int>(),
            SizeIds = sizeIds ?? new List<int>(),
            SortBy = sortBy?.StartsWith("price", StringComparison.OrdinalIgnoreCase) == true
                ? "price"
                : "newest",
            SortDirection = sortBy == "price_asc" ? "asc" : "desc"
        };

        var productResult = await productService.GetProductsAsync(query, cancellationToken);
        var filterResult = await productService.GetFilterOptionsAsync(cancellationToken);

        if (!productResult.IsSuccess || !filterResult.IsSuccess)
        {
            return View(new ProductsViewModel());
        }

        var products = productResult.Value!;
        var filters = filterResult.Value!;
        var viewModel = new ProductsViewModel
        {
            Products = products.Items.Select(ProductMvcMapper.ToCard).ToList(),
            Categories = ProductMvcMapper.ToCategories(filters),
            Colors = ProductMvcMapper.ToColors(filters),
            Sizes = ProductMvcMapper.ToSizes(filters),
            CurrentPage = products.PageNumber,
            TotalPages = products.TotalPages,
            SelectedCategoryId = categoryId,
            SelectedColorIds = query.ColorIds,
            SelectedSizeIds = query.SizeIds,
            SelectedPriceRange = priceRange,
            SelectedSortBy = sortBy,
            searchString = searchString
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Details(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await productService.GetProductAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error?.Type == Application.Common.ServiceErrorType.NotFound
                ? NotFound()
                : BadRequest();
        }

        return View(ProductMvcMapper.ToDetail(result.Value!));
    }

    [HttpPost]
    public IActionResult SearchByImage(IFormFile? imageFile)
    {
        return RedirectToAction("Index", new
        {
            error = "Tính năng tìm kiếm bằng hình ảnh đã bị tắt để ứng dụng hoạt động ổn định."
        });
    }
}
