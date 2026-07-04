using FashionHub.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace FashionHub.Areas.Admin.Controllers
{
    public class ReportsController : BaseController
    {
        private QL_SHOPQUANAO_PROEntities db = new QL_SHOPQUANAO_PROEntities();

        // GET: Admin/Reports
        public ActionResult Index()
        {
            // 1. Tổng doanh thu (Chỉ tính đơn hoàn thành)
            ViewBag.TotalRevenue = db.DonHangs
                .Where(d => d.IDTrangThai == 3)
                .Sum(d => (decimal?)d.TongThanhToan) ?? 0;

            // 2. Tổng số đơn hàng
            ViewBag.TotalOrders = db.DonHangs.Count();

            // 3. Tổng số sản phẩm đang kinh doanh
            ViewBag.TotalProducts = db.SanPhams.Count(p => p.TrangThai == true);

            // 4. Tổng số khách hàng
            ViewBag.TotalCustomers = db.NguoiDungs.Count(u => u.IDVaiTro == 2);

            // (Tùy chọn thêm) Doanh thu hôm nay
            var today = DateTime.Now.Date;
            ViewBag.TodayRevenue = db.DonHangs
                .Where(d => d.IDTrangThai == 3 && DbFunctions.TruncateTime(d.NgayTao) == today)
                .Sum(d => (decimal?)d.TongThanhToan) ?? 0;

            return View();
        }
        [HttpGet]
        public JsonResult GetRevenueData()
        {
            var toDate = DateTime.Now.Date;
            var fromDate = toDate.AddDays(-30);

            var data = db.DonHangs
                .Where(d => d.IDTrangThai == 3 && d.NgayTao >= fromDate && d.NgayTao <= toDate) // Chỉ tính đơn hoàn thành
                .GroupBy(d => DbFunctions.TruncateTime(d.NgayTao)) // Group theo ngày
                .Select(g => new {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.TongThanhToan)
                })
                .ToList() // Thực thi query
                .Select(x => new {
                    Date = x.Date.Value.ToString("dd/MM"),
                    Revenue = x.Revenue
                })
                .ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }
        // API: Top 5 sản phẩm bán chạy
        [HttpGet]
        public JsonResult GetTopProducts()
        {
            var data = db.ChiTietDonHangs
                .Where(ct => ct.DonHang.IDTrangThai == 3) // Chỉ tính đơn hoàn thành
                .GroupBy(ct => ct.TenSanPham)
                .Select(g => new {
                    ProductName = g.Key,
                    Quantity = g.Sum(x => x.SoLuong)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
                .ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }
    }
}