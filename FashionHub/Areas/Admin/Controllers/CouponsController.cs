using FashionHub.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace FashionHub.Areas.Admin.Controllers
{
    public class CouponsController : BaseController
    {
        private QL_SHOPQUANAO_PROEntities db = new QL_SHOPQUANAO_PROEntities();

        // 1. DANH SÁCH
        public ActionResult Index()
        {
            return View(db.MaGiamGias.OrderByDescending(m => m.IDMaGiamGia).ToList());
        }

        // 2. TẠO MỚI (GET)
        public ActionResult Create()
        {
            return View();
        }

        // 3. TẠO MỚI (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(MaGiamGia model)
        {
            if (ModelState.IsValid)
            {
                if (db.MaGiamGias.Any(x => x.MaCode == model.MaCode))
                {
                    ModelState.AddModelError("MaCode", "Mã này đã tồn tại.");
                    return View(model);
                }

                model.DaSuDung = 0;
                model.TrangThai = true;
                db.MaGiamGias.Add(model);
                db.SaveChanges();
                TempData["Success"] = "Tạo mã giảm giá thành công!";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // 4. XÓA (AJAX)
        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                var coupon = db.MaGiamGias.Find(id);
                if (coupon == null) return Json(new { success = false });

                // Nếu mã đã dùng rồi thì không xóa hẳn mà chỉ tắt trạng thái
                if (coupon.DaSuDung > 0)
                {
                    coupon.TrangThai = false; // Tắt hoạt động
                    db.SaveChanges();
                    return Json(new { success = true, message = "Đã tắt hoạt động mã này." });
                }

                db.MaGiamGias.Remove(coupon);
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch { return Json(new { success = false }); }
        }
    }
}