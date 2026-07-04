using FashionHub.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace FashionHub.Areas.Admin.Controllers
{
    public class OrdersController : BaseController
    {
        private QL_SHOPQUANAO_PROEntities db = new QL_SHOPQUANAO_PROEntities();

        // 1. DANH SÁCH ĐƠN HÀNG (CÓ LỌC & TÌM KIẾM)
        public ActionResult Index(string searchString, int? statusId, string fromDate, string toDate)
        {
            var orders = db.DonHangs
                .Include(d => d.NguoiDung)
                .Include(d => d.TrangThaiDonHang)
                .Include(d => d.PhuongThucThanhToan)
                .OrderByDescending(d => d.NgayTao)
                .AsQueryable();

            // 1. Tìm kiếm (Mã đơn hoặc Tên khách)
            if (!string.IsNullOrEmpty(searchString))
            {
                // ìm theo ID (nếu nhập số) hoặc Tên
                int id;
                if (int.TryParse(searchString, out id))
                {
                    orders = orders.Where(d => d.IDDonHang == id);
                }
                else
                {
                    orders = orders.Where(d => d.TenNguoiNhan.Contains(searchString));
                }
            }

            // 2. Lọc trạng thái
            if (statusId.HasValue)
            {
                orders = orders.Where(d => d.IDTrangThai == statusId.Value);
            }

            // 3. Lọc theo ngày
            if (!string.IsNullOrEmpty(fromDate))
            {
                DateTime dtFrom = DateTime.Parse(fromDate);
                orders = orders.Where(d => d.NgayTao >= dtFrom);
            }
            if (!string.IsNullOrEmpty(toDate))
            {
                DateTime dtTo = DateTime.Parse(toDate).AddDays(1); // Lấy hết ngày đó
                orders = orders.Where(d => d.NgayTao < dtTo);
            }

            ViewBag.StatusList = db.TrangThaiDonHangs.ToList();
            return View(orders.ToList());
        }

        // 2. CHI TIẾT ĐƠN HÀNG
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            var order = db.DonHangs
                .Include(d => d.ChiTietDonHangs.Select(ct => ct.BienTheSanPham.SanPham))
                .Include(d => d.ChiTietDonHangs.Select(ct => ct.BienTheSanPham.MauSac))
                .Include(d => d.ChiTietDonHangs.Select(ct => ct.BienTheSanPham.KichThuoc))
                .Include(d => d.TrangThaiDonHang)
                .Include(d => d.PhuongThucThanhToan)
                .Include(d => d.MaGiamGia)
                .FirstOrDefault(d => d.IDDonHang == id);

            if (order == null) return HttpNotFound();

            ViewBag.StatusList = new SelectList(db.TrangThaiDonHangs, "IDTrangThai", "TenTrangThai", order.IDTrangThai);
            return View(order);
        }

        // 3. CẬP NHẬT TRẠNG THÁI (LOGIC QUAN TRỌNG)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int id, int idTrangThai)
        {
            var order = db.DonHangs.Include(d => d.ChiTietDonHangs).FirstOrDefault(d => d.IDDonHang == id);
            if (order != null)
            {
                int oldStatus = order.IDTrangThai;

                // Nếu trạng thái mới là HỦY (4) và trạng thái cũ KHÔNG PHẢI HỦY
                // => HOÀN TRẢ TỒN KHO
                if (idTrangThai == 4 && oldStatus != 4)
                {
                    foreach (var item in order.ChiTietDonHangs)
                    {
                        var variant = db.BienTheSanPhams.Find(item.IDBienThe);
                        if (variant != null)
                        {
                            variant.SoLuongTon += item.SoLuong; // Cộng lại kho
                        }
                    }
                }
                // Nếu trạng thái cũ là HỦY (4) mà lại chuyển sang trạng thái khác (Khôi phục đơn)
                // => TRỪ LẠI TỒN KHO
                else if (oldStatus == 4 && idTrangThai != 4)
                {
                    foreach (var item in order.ChiTietDonHangs)
                    {
                        var variant = db.BienTheSanPhams.Find(item.IDBienThe);
                        if (variant != null)
                        {
                            if (variant.SoLuongTon >= item.SoLuong)
                            {
                                variant.SoLuongTon -= item.SoLuong;
                            }
                            else
                            {
                                TempData["Error"] = $"Không thể khôi phục đơn. Sản phẩm {item.TenSanPham} không đủ tồn kho!";
                                return RedirectToAction("Details", new { id = id });
                            }
                        }
                    }
                }

                order.IDTrangThai = idTrangThai;
                db.SaveChanges();
                TempData["Success"] = "Cập nhật trạng thái thành công!";
            }
            return RedirectToAction("Details", new { id = id });
        }

        // 4. IN HÓA ĐƠN (INVOICE)
        public ActionResult Invoice(int id)
        {
            var order = db.DonHangs
               .Include(d => d.ChiTietDonHangs.Select(ct => ct.BienTheSanPham.SanPham))
               .Include(d => d.ChiTietDonHangs.Select(ct => ct.BienTheSanPham.MauSac))
               .Include(d => d.ChiTietDonHangs.Select(ct => ct.BienTheSanPham.KichThuoc))
               .FirstOrDefault(d => d.IDDonHang == id);

            if (order == null) return HttpNotFound();

            return View(order);
        }
        public ActionResult ExportExcel()
        {
            var data = db.DonHangs
                .Include(d => d.NguoiDung)
                .Include(d => d.TrangThaiDonHang)
                .OrderByDescending(d => d.NgayTao)
                .ToList();

            var sb = new StringBuilder();
            // Header
            sb.AppendLine("Mã Đơn,Khách Hàng,SĐT,Ngày Đặt,Tổng Tiền,Trạng Thái,Địa Chỉ Giao");

            foreach (var item in data)
            {
                string khachHang = item.TenNguoiNhan.Replace(",", " ");
                string diaChi = item.DiaChiGiao.Replace(",", " - ");
                string trangThai = item.TrangThaiDonHang.TenTrangThai;

                sb.AppendLine($"{item.IDDonHang},{khachHang},{item.SoDienThoai},{item.NgayTao:dd/MM/yyyy},{item.TongThanhToan},{trangThai},{diaChi}");
            }

            // Xử lý tiếng Việt
            byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());
            byte[] bom = Encoding.UTF8.GetPreamble();
            byte[] result = new byte[bom.Length + buffer.Length];
            Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
            Buffer.BlockCopy(buffer, 0, result, bom.Length, buffer.Length);

            return File(result, "text/csv", $"DanhSachDonHang_{DateTime.Now:yyyyMMdd}.csv");
        }

        // 6. IN HÓA ĐƠN HÀNG LOẠT 
        [HttpPost]
        public ActionResult BulkPrint(int[] selectedIds)
        {
            if (selectedIds == null || selectedIds.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một đơn hàng để in.";
                return RedirectToAction("Index");
            }

            // Lấy danh sách đơn hàng theo ID đã chọn
            var orders = db.DonHangs
               .Include(d => d.ChiTietDonHangs.Select(ct => ct.BienTheSanPham.SanPham))
               .Include(d => d.ChiTietDonHangs.Select(ct => ct.BienTheSanPham.MauSac))
               .Include(d => d.ChiTietDonHangs.Select(ct => ct.BienTheSanPham.KichThuoc))
               .Where(d => selectedIds.Contains(d.IDDonHang))
               .OrderBy(d => d.IDDonHang)
               .ToList();

            return View(orders); // Trả về View BulkPrint
        }

        [HttpPost]
        public JsonResult ConfirmOrder(int id)
        {
            try
            {
                var order = db.DonHangs.Find(id);
                if (order == null) return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

                if (order.IDTrangThai == 0) // Chỉ xác nhận nếu đang là "Chờ xác nhận"
                {
                    order.IDTrangThai = 1; // Chuyển sang "Đã xác nhận"
                    db.SaveChanges();
                    return Json(new { success = true, message = "Đã xác nhận đơn hàng #" + id });
                }
                return Json(new { success = false, message = "Đơn hàng không ở trạng thái chờ." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // 2. XÁC NHẬN TẤT CẢ ĐƠN CHỜ (AJAX)
        [HttpPost]
        public JsonResult ConfirmAllPending()
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Lấy tất cả đơn đang "Chờ xác nhận" (IDTrangThai = 0)
                    var pendingOrders = db.DonHangs.Where(o => o.IDTrangThai == 0).ToList();
                    int count = pendingOrders.Count;

                    if (count == 0) return Json(new { success = false, message = "Không có đơn nào cần xác nhận." });

                    foreach (var order in pendingOrders)
                    {
                        order.IDTrangThai = 1; // Cập nhật sang "Đã xác nhận"
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = $"Đã xác nhận thành công {count} đơn hàng!" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
                }
            }
        }
    }
}