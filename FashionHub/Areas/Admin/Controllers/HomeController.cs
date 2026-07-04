using FashionHub.Areas.Admin.ViewModels;
using FashionHub.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace FashionHub.Areas.Admin.Controllers
{
    public class HomeController : BaseController
    {
        private QL_SHOPQUANAO_PROEntities db = new QL_SHOPQUANAO_PROEntities();

        // GET: Admin/Home
        public ActionResult Index()
        {
            var model = new DashboardViewModel
            {
                TotalOrders = db.DonHangs.Count(),


                NewOrders = db.DonHangs.Count(o => o.IDTrangThai == 0),

                TotalRevenue = db.DonHangs.Where(o => o.IDTrangThai == 3).Sum(o => (decimal?)o.TongThanhToan) ?? 0,

                TotalProducts = db.SanPhams.Count(),

                TotalCustomers = db.NguoiDungs.Count(u => u.IDVaiTro == 2)
            };

            return View(model);
        }
        [HttpGet]
        public JsonResult GetRevenueTrend()
        {
            var toDate = DateTime.Now.Date;
            var fromDate = toDate.AddDays(-6); 

            var rawData = db.DonHangs
                .Where(d => d.IDTrangThai == 3 && d.NgayTao >= fromDate && d.NgayTao <= toDate)
                .Select(d => new { d.NgayTao, d.TongThanhToan })
                .ToList();

            var data = rawData
                .GroupBy(d => d.NgayTao.Value.Date)
                .Select(g => new {
                    Date = g.Key.ToString("dd/MM"),
                    Revenue = g.Sum(x => x.TongThanhToan)
                })
                .OrderBy(x => x.Date)
                .ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        // Biểu đồ Phân bố trạng thái đơn hàng
        [HttpGet]
        public JsonResult GetOrderStatusData()
        {
            // Lấy số lượng đơn theo từng trạng thái
            var data = db.DonHangs
                .GroupBy(d => d.IDTrangThai)
                .Select(g => new {
                    StatusId = g.Key,
                    Count = g.Count()
                })
                .ToList() 
                .Select(x => new {
                    Label = GetStatusName(x.StatusId),
                    Count = x.Count,
                    Color = GetStatusColor(x.StatusId)
                })
                .ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        // Hàm phụ trợ lấy tên trạng thái
        private string GetStatusName(int id)
        {
            switch (id)
            {
                case 0: return "Chờ xác nhận";
                case 1: return "Đã xác nhận";
                case 2: return "Đang giao";
                case 3: return "Hoàn thành";
                case 4: return "Đã hủy";
                default: return "Khác";
            }
        }

        private string GetStatusColor(int id)
        {
            switch (id)
            {
                case 0: return "#ffc107"; // Vàng (Warning)
                case 1: return "#0dcaf0"; // Xanh dương nhạt (Info)
                case 2: return "#0d6efd"; // Xanh dương đậm (Primary)
                case 3: return "#198754"; // Xanh lá (Success)
                case 4: return "#dc3545"; // Đỏ (Danger)
                default: return "#6c757d";
            }
        }
    }
}