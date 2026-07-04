using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using FashionHub.Models;

namespace FashionHub.Areas.Admin.Controllers
{
    // 1. Kế thừa BaseController để bảo mật
    public class CategoriesController : BaseController
    {
        private QL_SHOPQUANAO_PROEntities db = new QL_SHOPQUANAO_PROEntities();

        // GET: Admin/Categories
        // Thêm tham số searchString để tìm kiếm
        public ActionResult Index(string searchString)
        {
            var danhMucs = db.DanhMucs
                                     .Include(d => d.DanhMuc2)
                                     .Include(d => d.SanPhams)
                                     .AsQueryable();

            // 2. Logic Tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                danhMucs = danhMucs.Where(d => d.TenDanhMuc.Contains(searchString));
            }
            else
            {
                // Nếu không tìm kiếm, sắp xếp theo Cha trước -> Con sau để hiển thị đẹp
                danhMucs = danhMucs.OrderBy(d => d.IDDanhMucCha).ThenBy(d => d.IDDanhMuc);
            }

            return View(danhMucs.ToList());
        }
        public ActionResult Export()
        {
            var data = db.DanhMucs.Include(d => d.DanhMuc2).ToList();

            var sb = new StringBuilder();
            // Header của file CSV
            sb.AppendLine("ID,Tên Danh Mục,Danh Mục Cha,Ngày Tạo");

            foreach (var item in data)
            {
                string parentName = item.DanhMuc2 != null ? item.DanhMuc2.TenDanhMuc : "Gốc";

                string tenDanhMuc = item.TenDanhMuc.Contains(",") ? $"\"{item.TenDanhMuc}\"" : item.TenDanhMuc;

                sb.AppendLine($"{item.IDDanhMuc},{tenDanhMuc},{parentName},{DateTime.Now:dd/MM/yyyy}");
            }

            byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());

            // 2. Lấy chữ ký BOM (Byte Order Mark) của UTF8
            byte[] bom = Encoding.UTF8.GetPreamble();

            // 3. Tạo mảng byte mới = BOM + Dữ liệu
            byte[] result = new byte[bom.Length + buffer.Length];

            // Copy BOM vào đầu mảng
            Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
            // Copy dữ liệu vào sau BOM
            Buffer.BlockCopy(buffer, 0, result, bom.Length, buffer.Length);

            // Trả về file kết quả
            return File(result, "text/csv", $"DanhSachDanhMuc_{DateTime.Now:yyyyMMdd}.csv");
        }

        // GET: Admin/Categories/Create
        public ActionResult Create()
        {
            // Sử dụng hàm tạo cây danh mục (Logic ở cuối file)
            ViewBag.IDDanhMucCha = GetCategoryTree();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "IDDanhMuc,TenDanhMuc,IDDanhMucCha")] DanhMuc danhMuc)
        {
            if (ModelState.IsValid)
            {
                db.DanhMucs.Add(danhMuc);
                db.SaveChanges();
                TempData["Success"] = "Thêm danh mục thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.IDDanhMucCha = GetCategoryTree(danhMuc.IDDanhMucCha);
            return View(danhMuc);
        }

        // GET: Admin/Categories/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            DanhMuc danhMuc = db.DanhMucs.Find(id);
            if (danhMuc == null) return HttpNotFound();

            // Loại bỏ chính nó khỏi dropdown cha để tránh vòng lặp
            ViewBag.IDDanhMucCha = GetCategoryTree(danhMuc.IDDanhMucCha, excludeId: id);
            return View(danhMuc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "IDDanhMuc,TenDanhMuc,IDDanhMucCha")] DanhMuc danhMuc)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra: Không được chọn chính mình làm cha
                if (danhMuc.IDDanhMuc == danhMuc.IDDanhMucCha)
                {
                    ModelState.AddModelError("IDDanhMucCha", "Không thể chọn chính danh mục này làm cha.");
                }
                else
                {
                    db.Entry(danhMuc).State = EntityState.Modified;
                    db.SaveChanges();
                    TempData["Success"] = "Cập nhật thành công!";
                    return RedirectToAction("Index");
                }
            }
            ViewBag.IDDanhMucCha = GetCategoryTree(danhMuc.IDDanhMucCha, excludeId: danhMuc.IDDanhMuc);
            return View(danhMuc);
        }

        // POST: Admin/Categories/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Delete(int id)
        {
            try
            {
                var danhMuc = db.DanhMucs.Find(id);
                if (danhMuc == null) return Json(new { success = false, message = "Không tìm thấy danh mục." });

                // 4. Kiểm tra ràng buộc trước khi xóa
                // Kiểm tra có danh mục con không?
                bool hasChild = db.DanhMucs.Any(d => d.IDDanhMucCha == id);
                if (hasChild) return Json(new { success = false, message = "Không thể xóa: Danh mục này đang chứa danh mục con." });

                // Kiểm tra có sản phẩm không?
                bool hasProduct = db.SanPhams.Any(p => p.IDDanhMuc == id);
                if (hasProduct) return Json(new { success = false, message = "Không thể xóa: Đang có sản phẩm thuộc danh mục này." });

                db.DanhMucs.Remove(danhMuc);
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Lỗi hệ thống không thể xóa." });
            }
        }

        private List<SelectListItem> GetCategoryTree(int? selectedId = null, int? excludeId = null)
        {
            var items = new List<SelectListItem>();
            items.Add(new SelectListItem { Value = "", Text = "-- Là Danh Mục Gốc --" });

            var rootCategories = db.DanhMucs.Where(c => c.IDDanhMucCha == null).ToList();

            foreach (var cat in rootCategories)
            {
                if (excludeId.HasValue && cat.IDDanhMuc == excludeId.Value) continue;

                items.Add(new SelectListItem
                {
                    Value = cat.IDDanhMuc.ToString(),
                    Text = cat.TenDanhMuc.ToUpper(), 
                    Selected = (selectedId == cat.IDDanhMuc)
                });

                var childCategories = db.DanhMucs.Where(c => c.IDDanhMucCha == cat.IDDanhMuc).ToList();
                foreach (var child in childCategories)
                {
                    if (excludeId.HasValue && child.IDDanhMuc == excludeId.Value) continue;

                    items.Add(new SelectListItem
                    {
                        Value = child.IDDanhMuc.ToString(),
                        Text = "|__ " + child.TenDanhMuc, // Thụt đầu dòng
                        Selected = (selectedId == child.IDDanhMuc)
                    });
                }
            }
            return items;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}