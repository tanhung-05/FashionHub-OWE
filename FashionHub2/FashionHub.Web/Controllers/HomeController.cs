using System.Diagnostics;
using FashionHub.Web.Application.Products;
using FashionHub.Web.Models;
using FashionHub.Web.ViewModels.Home;
using FashionHub.Web.ViewModels.Products;
using Microsoft.AspNetCore.Mvc;

namespace FashionHub.Web.Controllers;

public class HomeController : Controller
{
    private readonly IProductService productService;

    public HomeController(IProductService productService)
    {
        this.productService = productService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var newestResult = await productService.GetProductsAsync(
            new ProductQueryParameters
            {
                PageNumber = 1,
                PageSize = 8,
                SortBy = "newest",
                SortDirection = "desc"
            },
            cancellationToken);

        var saleResult = await productService.GetProductsAsync(
            new ProductQueryParameters
            {
                PageNumber = 1,
                PageSize = 8,
                SortBy = "newest",
                SortDirection = "desc",
                OnSaleOnly = true
            },
            cancellationToken);
        var filterResult = await productService.GetFilterOptionsAsync(cancellationToken);

        var viewModel = new HomeViewModel
        {
            SanPhamMoi = newestResult.IsSuccess
                ? newestResult.Value!.Items.Select(ProductMvcMapper.ToCard).ToList()
                : new List<ProductCardViewModel>(),
            SanPhamKhuyenMai = saleResult.IsSuccess
                ? saleResult.Value!.Items.Select(ProductMvcMapper.ToCard).ToList()
                : new List<ProductCardViewModel>(),
            DanhMuc = filterResult.IsSuccess
                ? ProductMvcMapper.ToCategories(filterResult.Value!)
                : new List<Models.Generated.DanhMuc>(),
            ThuongHieu = filterResult.IsSuccess
                ? ProductMvcMapper.ToBrands(filterResult.Value!)
                : new List<Models.Generated.ThuongHieu>()
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
