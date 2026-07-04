using FashionHub.Models;
using FashionHub.ViewModels;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace FashionHub.Controllers
{
    public class HomeController : Controller
    {
        private QL_SHOPQUANAO_PROEntities db = new QL_SHOPQUANAO_PROEntities();

        public ActionResult Index()
        {
            var viewModel = new HomeViewModel();

            var sanPhamMoiQuery = db.SanPhams
                .Where(p => p.TrangThai == true)
                .OrderByDescending(p => p.IDSanPham)
                .Take(8)
                .Include(p => p.BienTheSanPhams.Select(bt => bt.HinhAnh_BienThe.Select(ha => ha.HinhAnh)));

            viewModel.SanPhamMoi = sanPhamMoiQuery.ToList().Select(p => new ProductCardViewModel
            {
                IDSanPham = p.IDSanPham,
                TenSanPham = p.TenSanPham,
                Gia = p.Gia,
                AnhChinhURL = p.BienTheSanPhams
                                .SelectMany(bt => bt.HinhAnh_BienThe)
                                .FirstOrDefault(habt => habt.LaAnhChinh == true)?
                                .HinhAnh.DuongDan ?? "/Content/images/placeholder.png",
                IsOutStock = !p.BienTheSanPhams.Any(bt => bt.SoLuongTon > 0),
                GiaKhuyenMai = p.GiaKhuyenMai,
                NgayBatDauKM = p.NgayBatDauKM,
                NgayKetThucKM = p.NgayKetThucKM

            }).ToList();

            viewModel.SanPhamNoiBat = viewModel.SanPhamMoi.OrderBy(x => x.Gia).ToList();

            return View(viewModel);
        }

        [ChildActionOnly]
        public ActionResult _MenuPartial()
        {
            var categories = db.DanhMucs.OrderBy(c => c.IDDanhMucCha).ToList();
            return PartialView(categories);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}