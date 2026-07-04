using FashionHub.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace FashionHub.Controllers
{
    [Authorize] 
    public class ManageOrderController : Controller
    {
        private QL_SHOPQUANAO_PROEntities db = new QL_SHOPQUANAO_PROEntities();

        public ActionResult Index()
        {
            var user = Session["User"] as NguoiDung;
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "ManageOrder") });
            }

            ViewBag.AllCount = db.DonHangs.Count(o => o.IDNguoiDung == user.IDNguoiDung);
            ViewBag.WaitingCount = db.DonHangs.Count(o => o.IDNguoiDung == user.IDNguoiDung && o.IDTrangThai == 0);
            ViewBag.ProcessingCount = db.DonHangs.Count(o => o.IDNguoiDung == user.IDNguoiDung && o.IDTrangThai == 1);
            ViewBag.ShippingCount = db.DonHangs.Count(o => o.IDNguoiDung == user.IDNguoiDung && o.IDTrangThai == 2);
            ViewBag.CompletedCount = db.DonHangs.Count(o => o.IDNguoiDung == user.IDNguoiDung && o.IDTrangThai == 3);
            ViewBag.CancelledCount = db.DonHangs.Count(o => o.IDNguoiDung == user.IDNguoiDung && o.IDTrangThai == 4);

            return View();
        }
        public PartialViewResult GetOrdersByStatus(int? statusId)
        {
            var user = Session["User"] as NguoiDung;
            IQueryable<DonHang> orders = db.DonHangs
                                           .Where(o => o.IDNguoiDung == user.IDNguoiDung)
                                           .Include(o => o.ChiTietDonHangs); 

            if (statusId.HasValue)
            {
                orders = orders.Where(o => o.IDTrangThai == statusId.Value);
            }

            return PartialView("_OrderListPartial", orders.OrderByDescending(o => o.NgayTao).ToList());
        }

        // GET: /ManageOrder/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var user = Session["User"] as NguoiDung;
            var order = db.DonHangs
                          .Include(o => o.ChiTietDonHangs)
                          .Include(o => o.TrangThaiDonHang)
                          .Include(o => o.PhuongThucThanhToan)
                          .FirstOrDefault(o => o.IDDonHang == id && o.IDNguoiDung == user.IDNguoiDung);

            if (order == null)
            {
                return HttpNotFound(); // Đảm bảo người dùng chỉ xem được đơn hàng của chính mình
            }

            return View(order);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CancelOrder(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var user = Session["User"] as NguoiDung;
                    var order = db.DonHangs.FirstOrDefault(o => o.IDDonHang == id && o.IDNguoiDung == user.IDNguoiDung);


                    if (order == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
                    }
                    if (order.IDTrangThai != 0) 
                    {
                        return Json(new { success = false, message = "Không thể hủy đơn hàng ở trạng thái này." });
                    }

                    order.IDTrangThai = 4; 
                    db.Entry(order).State = EntityState.Modified;


                    var orderDetails = db.ChiTietDonHangs.Where(d => d.IDDonHang == id).ToList();
                    foreach (var detail in orderDetails)
                    {

                        if (detail.IDBienThe.HasValue)
                        {
                            var variant = db.BienTheSanPhams.Find(detail.IDBienThe.Value);
                            if (variant != null)
                            {
                                variant.SoLuongTon += detail.SoLuong;
                                db.Entry(variant).State = EntityState.Modified;
                            }
                        }
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = "Hủy đơn hàng thành công!" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine("LỖI KHI HỦY ĐƠN HÀNG: " + ex.ToString());
                    return Json(new { success = false, message = "Đã xảy ra lỗi. Vui lòng thử lại." });
                }
            }
        }
        public JsonResult GetOrderCounts()
        {
            var user = Session["User"] as NguoiDung;
            if (user == null)
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }

            var counts = new
            {
                all = db.DonHangs.Count(o => o.IDNguoiDung == user.IDNguoiDung),
                waiting = db.DonHangs.Count(o => o.IDNguoiDung == user.IDNguoiDung && o.IDTrangThai == 0),
                processing = db.DonHangs.Count(o => o.IDNguoiDung == user.IDNguoiDung && o.IDTrangThai == 1),
                shipping = db.DonHangs.Count(o => o.IDNguoiDung == user.IDNguoiDung && o.IDTrangThai == 2),
                completed = db.DonHangs.Count(o => o.IDNguoiDung == user.IDNguoiDung && o.IDTrangThai == 3),
                cancelled = db.DonHangs.Count(o => o.IDNguoiDung == user.IDNguoiDung && o.IDTrangThai == 4)
            };

            return Json(new { success = true, data = counts }, JsonRequestBehavior.AllowGet);
        }
    }
}