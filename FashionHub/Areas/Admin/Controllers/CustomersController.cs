using FashionHub.Models;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace FashionHub.Areas.Admin.Controllers
{
    public class CustomersController : BaseController
    {
        private QL_SHOPQUANAO_PROEntities db = new QL_SHOPQUANAO_PROEntities();

        //  DANH SÁCH KHÁCH HÀNG
        public ActionResult Index(string searchString)
        {
            // Chỉ lấy VaiTro = 2 (Khách hàng), không lấy Admin
            var customers = db.NguoiDungs.Where(u => u.IDVaiTro == 2).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                customers = customers.Where(u => u.HoTen.Contains(searchString) || u.Email.Contains(searchString) || u.SoDienThoai.Contains(searchString));
            }

            return View(customers.OrderByDescending(u => u.NgayTao).ToList());
        }

        // CHI TIẾT KHÁCH HÀNG & LỊCH SỬ ĐƠN HÀNG
        public ActionResult Details(int id)
        {
            var customer = db.NguoiDungs.Find(id);
            if (customer == null) return HttpNotFound();

            // Lấy lịch sử đơn hàng của khách này
            ViewBag.OrderHistory = db.DonHangs
                .Where(d => d.IDNguoiDung == id)
                .OrderByDescending(d => d.NgayTao)
                .Include(d => d.TrangThaiDonHang)
                .ToList();

            // Tính tổng chi tiêu
            ViewBag.TotalSpent = db.DonHangs
                .Where(d => d.IDNguoiDung == id && d.IDTrangThai == 3) // Chỉ tính đơn hoàn thành
                .Sum(d => (decimal?)d.TongThanhToan) ?? 0;

            return View(customer);
        }

        // MỞ KHÓA TÀI KHOẢN 
        [HttpPost]
        public JsonResult ToggleStatus(int id)
        {
            var user = db.NguoiDungs.Find(id);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy user" });

            // Đảo ngược trạng thái (Đang mở -> Khóa, Đang khóa -> Mở)
            bool currentStatus = user.TrangThai ?? true;
            user.TrangThai = !currentStatus;

            db.SaveChanges();

            return Json(new { success = true, newStatus = user.TrangThai });
        }
    }
}