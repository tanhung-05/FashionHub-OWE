using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FashionHub.Web.Data;
using FashionHub.Web.Models;
using FashionHub.Web.ViewModels.Home;
using FashionHub.Web.ViewModels.Products;

namespace FashionHub.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new HomeViewModel();

        // Query sản phẩm mới - 8 sản phẩm mới nhất (theo IdsanPham desc)
        var sanPhamMoiQuery = await _context.SanPhams
            .Where(p => p.TrangThai == true)
            .OrderByDescending(p => p.IdsanPham)
            .Take(8)
            .Include(p => p.BienTheSanPhams)
                .ThenInclude(bt => bt.HinhAnhBienThes)
                .ThenInclude(habt => habt.IdhinhAnhNavigation)
            .ToListAsync();

        viewModel.SanPhamMoi = sanPhamMoiQuery.Select(p => new ProductCardViewModel
        {
            IDSanPham = p.IdsanPham,
            TenSanPham = p.TenSanPham ?? string.Empty,
            Gia = p.Gia,
            AnhChinhURL = p.BienTheSanPhams
                            .SelectMany(bt => bt.HinhAnhBienThes)
                            .FirstOrDefault(habt => habt.LaAnhChinh == true)?
                            .IdhinhAnhNavigation?.DuongDan ?? "/images/placeholder.png",
            IsOutStock = !p.BienTheSanPhams.Any(bt => bt.SoLuongTon > 0),
            GiaKhuyenMai = p.GiaKhuyenMai,
            NgayBatDauKM = p.NgayBatDauKm,
            NgayKetThucKM = p.NgayKetThucKm
        }).ToList();

        // Query sản phẩm khuyến mãi - sản phẩm có GiaKhuyenMai
        var sanPhamKhuyenMaiQuery = await _context.SanPhams
            .Where(p => p.TrangThai == true && p.GiaKhuyenMai.HasValue)
            .OrderByDescending(p => p.IdsanPham)
            .Take(8)
            .Include(p => p.BienTheSanPhams)
                .ThenInclude(bt => bt.HinhAnhBienThes)
                .ThenInclude(habt => habt.IdhinhAnhNavigation)
            .ToListAsync();

        viewModel.SanPhamKhuyenMai = sanPhamKhuyenMaiQuery.Select(p => new ProductCardViewModel
        {
            IDSanPham = p.IdsanPham,
            TenSanPham = p.TenSanPham ?? string.Empty,
            Gia = p.Gia,
            AnhChinhURL = p.BienTheSanPhams
                            .SelectMany(bt => bt.HinhAnhBienThes)
                            .FirstOrDefault(habt => habt.LaAnhChinh == true)?
                            .IdhinhAnhNavigation?.DuongDan ?? "/images/placeholder.png",
            IsOutStock = !p.BienTheSanPhams.Any(bt => bt.SoLuongTon > 0),
            GiaKhuyenMai = p.GiaKhuyenMai,
            NgayBatDauKM = p.NgayBatDauKm,
            NgayKetThucKM = p.NgayKetThucKm
        }).ToList();

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
