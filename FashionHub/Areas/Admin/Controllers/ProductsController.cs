using FashionHub.Areas.Admin.Controllers;
using FashionHub.Areas.Admin.ViewModels;
using FashionHub.Models;
using FashionHub.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace FashionHub.Areas.Admin.Controllers
{
    public class ProductsController : BaseController
    {
        private QL_SHOPQUANAO_PROEntities db = new QL_SHOPQUANAO_PROEntities();

        // 1. DANH SÁCH SẢN PHẨM
        public ActionResult Index(string searchString, int? categoryId, int? brandId, int? status)
        {
            var products = db.SanPhams
                .Include(p => p.DanhMuc)
                .Include(p => p.ThuongHieu)
                .Include(p => p.BienTheSanPhams)
                .Include(p => p.BienTheSanPhams.Select(b => b.HinhAnh_BienThe.Select(h => h.HinhAnh)))
                .AsQueryable();

            // --- LOGIC LỌC ---
            if (!string.IsNullOrEmpty(searchString))
                products = products.Where(p => p.TenSanPham.Contains(searchString));

            if (categoryId.HasValue)
                products = products.Where(p => p.IDDanhMuc == categoryId.Value);

            if (brandId.HasValue)
                products = products.Where(p => p.IDThuongHieu == brandId.Value);

            if (status.HasValue)
                products = products.Where(p => p.TrangThai == (status.Value == 1));

            products = products.OrderByDescending(p => p.IDSanPham);


            ViewBag.DanhMucs = GetCategoryTree(categoryId);

            ViewBag.ThuongHieux = db.ThuongHieux.ToList();

            return View(products.ToList());
        }

        // TẠO SẢN PHẨM 
        public ActionResult Create()
        {
            ViewBag.IDDanhMuc = GetCategoryTree();
            ViewBag.IDThuongHieu = new SelectList(db.ThuongHieux, "IDThuongHieu", "TenThuongHieu");
            return View();
        }

        //TẠO SẢN PHẨM (POST)
        [HttpPost]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                var product = new SanPham
                {
                    TenSanPham = model.TenSanPham,
                    MoTa = model.MoTa,
                    IDDanhMuc = model.IDDanhMuc,
                    IDThuongHieu = model.IDThuongHieu,
                    TrangThai = true,
                    Gia = model.Gia 
                };
                db.SanPhams.Add(product);
                db.SaveChanges();

                TempData["Success"] = "Tạo sản phẩm thành công! Hãy thêm biến thể.";

                return RedirectToAction("Edit", new { id = product.IDSanPham });
            }

            ViewBag.IDDanhMuc = GetCategoryTree(model.IDDanhMuc);
            ViewBag.IDThuongHieu = new SelectList(db.ThuongHieux, "IDThuongHieu", "TenThuongHieu", model.IDThuongHieu);
            return View(model);
        }

        // Trong Areas/Admin/Controllers/ProductsController.cs

        private List<SelectListItem> GetCategoryTree(int? selectedId = null)
        {
            var items = new List<SelectListItem>();

            // Lấy danh mục gốc (Cha)
            var rootCategories = db.DanhMucs.Where(c => c.IDDanhMucCha == null).ToList();

            foreach (var cat in rootCategories)
            {
                // Thêm Cha (Nhưng DISABLE để không cho chọn)
                var groupItem = new SelectListItem
                {
                    Value = cat.IDDanhMuc.ToString(),
                    Text = cat.TenDanhMuc.ToUpper(),
                    Disabled = true
                };
                items.Add(groupItem);

                // Lấy Con
                var childCategories = db.DanhMucs.Where(c => c.IDDanhMucCha == cat.IDDanhMuc).ToList();
                foreach (var child in childCategories)
                {
                    items.Add(new SelectListItem
                    {
                        Value = child.IDDanhMuc.ToString(),
                        Text = "|__ " + child.TenDanhMuc,
                        Selected = (selectedId == child.IDDanhMuc)
                    });
                }
            }
            return items;
        }
        public ActionResult Export()
        {
            // Lấy toàn bộ dữ liệu kèm biến thể
            var data = db.BienTheSanPhams
                .Include(b => b.SanPham)
                .Include(b => b.SanPham.DanhMuc)
                .Include(b => b.SanPham.ThuongHieu)
                .Include(b => b.MauSac)
                .Include(b => b.KichThuoc)
                .OrderBy(b => b.IDSanPham)
                .ToList();

            var sb = new StringBuilder();
            // Header
            sb.AppendLine("Mã SP,Tên Sản Phẩm,Danh Mục,Thương Hiệu,SKU,Màu Sắc,Kích Thước,Giá Bán,Giá KM,Tồn Kho,Trạng Thái");

            foreach (var item in data)
            {
                // Xử lý dấu phẩy trong tên để tránh lỗi CSV
                string tenSP = item.SanPham.TenSanPham.Replace(",", " ");
                string danhMuc = item.SanPham.DanhMuc?.TenDanhMuc ?? "";
                string thuongHieu = item.SanPham.ThuongHieu?.TenThuongHieu ?? "";
                string trangThai = (item.SanPham.TrangThai ?? false) ? "Đang bán" : "Ngừng bán";

                sb.AppendLine($"{item.IDSanPham},{tenSP},{danhMuc},{thuongHieu},{item.SKU},{item.MauSac.TenMau},{item.KichThuoc.TenKichThuoc},{item.SoLuongTon},{trangThai}");
            }

            // Xử lý BOM để hiển thị tiếng Việt trong Excel
            byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());
            byte[] bom = Encoding.UTF8.GetPreamble();
            byte[] result = new byte[bom.Length + buffer.Length];
            Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
            Buffer.BlockCopy(buffer, 0, result, bom.Length, buffer.Length);

            return File(result, "text/csv", $"DanhSachSanPham_{DateTime.Now:yyyyMMdd}.csv");
        }



        // TRANG CHỈNH SỬA & QUẢN LÝ BIẾN THỂ 
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var product = db.SanPhams.Find(id);
            if (product == null) return HttpNotFound();

            var model = new ProductViewModel
            {
                IDSanPham = product.IDSanPham,
                TenSanPham = product.TenSanPham,
                MoTa = product.MoTa,
                IDDanhMuc = product.IDDanhMuc ?? 0,
                IDThuongHieu = product.IDThuongHieu ?? 0,
                TrangThai = product.TrangThai ?? true,

                // --- QUAN TRỌNG: LẤY GIÁ TỪ DB LÊN ---
                Gia = product.Gia
            };

            // Load Dropdown (Dùng hàm GetCategoryTree mới sửa ở trên)
            ViewBag.IDDanhMuc = GetCategoryTree(product.IDDanhMuc);
            ViewBag.IDThuongHieu = new SelectList(db.ThuongHieux, "IDThuongHieu", "TenThuongHieu", product.IDThuongHieu);

            ViewBag.Colors = new SelectList(db.MauSacs, "IDMauSac", "TenMau");
            ViewBag.Sizes = new SelectList(db.KichThuocs, "IDKichThuoc", "TenKichThuoc");

            ViewBag.ExistingVariants = db.BienTheSanPhams
                .Where(v => v.IDSanPham == id)
                .Include(v => v.MauSac)
                .Include(v => v.KichThuoc)
                .Include(v => v.HinhAnh_BienThe.Select(h => h.HinhAnh))
                .ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                var product = db.SanPhams.Find(model.IDSanPham);
                if (product == null) return HttpNotFound();

                product.TenSanPham = model.TenSanPham;
                product.MoTa = model.MoTa;
                product.IDDanhMuc = model.IDDanhMuc;
                product.IDThuongHieu = model.IDThuongHieu;
                product.Gia = model.Gia; // Cập nhật giá mới (nếu có sửa)

                db.SaveChanges();
                TempData["Success"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("Edit", new { id = model.IDSanPham });
            }

            // Load lại dữ liệu nếu lỗi
            ViewBag.IDDanhMuc = GetCategoryTree(model.IDDanhMuc); // Dùng hàm Tree
            ViewBag.IDThuongHieu = new SelectList(db.ThuongHieux, "IDThuongHieu", "TenThuongHieu", model.IDThuongHieu);
            ViewBag.Colors = new SelectList(db.MauSacs, "IDMauSac", "TenMau");
            ViewBag.Sizes = new SelectList(db.KichThuocs, "IDKichThuoc", "TenKichThuoc");

            ViewBag.ExistingVariants = db.BienTheSanPhams
                .Where(v => v.IDSanPham == model.IDSanPham)
                .Include(v => v.MauSac)
                .Include(v => v.KichThuoc)
                .Include(v => v.HinhAnh_BienThe.Select(h => h.HinhAnh))
                .ToList();

            return View(model);
        }

        // 4. AJAX: THÊM BIẾN THỂ (Được gọi từ Modal trong trang Edit)
        [HttpPost]
        public JsonResult AddVariant(ProductVariantViewModel model)
        {
            try
            {
                // Kiểm tra trùng lặp (Cùng SP, Cùng Màu, Cùng Size)
                bool exists = db.BienTheSanPhams.Any(x => x.IDSanPham == model.IDSanPham && x.IDMauSac == model.IDMauSac && x.IDKichThuoc == model.IDKichThuoc);
                if (exists)
                {
                    return Json(new { success = false, message = "Size hoặc màu này có trong danh sách biến thể của sản phẩm này ròi" });
                }

                if (model.SoLuongTon < 0)
                {
                    return Json(new { success = false, message = "Số lượng tồn không được nhỏ hơn 0" });
                }
                string sku = $"SP{model.IDSanPham}-C{model.IDMauSac}-S{model.IDKichThuoc}-{DateTime.Now.Ticks.ToString().Substring(10)}";

                var variant = new BienTheSanPham
                {
                    IDSanPham = model.IDSanPham,
                    IDMauSac = model.IDMauSac,
                    IDKichThuoc = model.IDKichThuoc,
                    SoLuongTon = model.SoLuongTon,
                    SKU = sku
                };
                db.BienTheSanPhams.Add(variant);
                db.SaveChanges(); // Lưu để lấy IDBienThe

                // Xử lý upload ảnh (Nếu có)
                if (model.UploadImage != null && model.UploadImage.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(model.UploadImage.FileName);
                    // Đặt tên file duy nhất để tránh trùng
                    string uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                    string path = Path.Combine(Server.MapPath("~/Content/images/products/"), uniqueFileName);

                    // Lưu file vào server
                    model.UploadImage.SaveAs(path);

                    // Lưu vào bảng HinhAnh
                    var hinhAnh = new HinhAnh { DuongDan = "/Content/images/products/" + uniqueFileName };
                    db.HinhAnhs.Add(hinhAnh);
                    db.SaveChanges();

                    // Liên kết HinhAnh với BienThe
                    var link = new HinhAnh_BienThe { IDHinhAnh = hinhAnh.IDHinhAnh, IDBienThe = variant.IDBienThe, LaAnhChinh = true };
                    db.HinhAnh_BienThe.Add(link);
                    db.SaveChanges();
                }

                return Json(new { success = true, message = "Thêm biến thể thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // 5. AJAX: XÓA BIẾN THỂ
        [HttpPost]
        public JsonResult DeleteVariant(int id)
        {
            var variant = db.BienTheSanPhams.Find(id);
            if (variant != null)
            {
                db.BienTheSanPhams.Remove(variant);
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Không tìm thấy biến thể." });
        }


        [HttpGet]
        public JsonResult GetVariantImages(int variantId)
        {
            db.Configuration.ProxyCreationEnabled = false; 

            var images = db.HinhAnh_BienThe
                .Where(x => x.IDBienThe == variantId)
                .Select(x => new {
                    x.IDHinhAnh,
                    x.HinhAnh.DuongDan,
                    x.LaAnhChinh
                })
                .ToList();

            return Json(new { success = true, data = images }, JsonRequestBehavior.AllowGet);
        }

        // 7. AJAX: UPLOAD THÊM ẢNH CHO BIẾN THỂ
        [HttpPost]
        public JsonResult UploadImage(int variantId, HttpPostedFileBase file)
        {
            try
            {
                if (file != null && file.ContentLength > 0)
                {
                    // 1. Lưu file
                    string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                    string path = Path.Combine(Server.MapPath("~/Content/images/products/"), fileName);
                    file.SaveAs(path);

                    // 2. Tạo dữ liệu HinhAnh
                    var hinhAnh = new HinhAnh { DuongDan = "/Content/images/products/" + fileName };
                    db.HinhAnhs.Add(hinhAnh);
                    db.SaveChanges();

                    // 3. Kiểm tra xem biến thể này đã có ảnh nào chưa
                    bool hasImage = db.HinhAnh_BienThe.Any(x => x.IDBienThe == variantId);

                    // 4. Liên kết (Nếu chưa có ảnh nào thì ảnh này tự động là ảnh chính)
                    var link = new HinhAnh_BienThe
                    {
                        IDHinhAnh = hinhAnh.IDHinhAnh,
                        IDBienThe = variantId,
                        LaAnhChinh = !hasImage // True nếu chưa có ảnh, False nếu đã có
                    };
                    db.HinhAnh_BienThe.Add(link);
                    db.SaveChanges();

                    return Json(new { success = true, message = "Upload thành công!" });
                }
                return Json(new { success = false, message = "Vui lòng chọn file." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // ĐẶT LÀM ẢNH CHÍNH 
        [HttpPost]
        public JsonResult SetMainImage(int variantId, int imageId)
        {
            try
            {
                // 1. Lấy tất cả các liên kết ảnh của biến thể này
                var links = db.HinhAnh_BienThe.Where(x => x.IDBienThe == variantId).ToList();

                // 2. Reset tất cả về false
                foreach (var item in links)
                {
                    item.LaAnhChinh = false;
                }

                // 3. Set ảnh được chọn thành true
                var target = links.FirstOrDefault(x => x.IDHinhAnh == imageId);
                if (target != null)
                {
                    target.LaAnhChinh = true;
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Không tìm thấy ảnh." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // 9. XÓA ẢNH
        [HttpPost]
        public JsonResult DeleteImage(int variantId, int imageId)
        {
            try
            {
                var link = db.HinhAnh_BienThe.FirstOrDefault(x => x.IDBienThe == variantId && x.IDHinhAnh == imageId);
                if (link != null)
                {
                    // Nếu xóa ảnh đang là ảnh chính -> Phải chỉ định ảnh khác làm ảnh chính (nếu còn ảnh)
                    bool wasMain = link.LaAnhChinh ?? false;

                    db.HinhAnh_BienThe.Remove(link);
                    // (Tùy chọn: Xóa luôn record trong bảng HinhAnh và file vật lý nếu muốn dọn rác triệt để)

                    db.SaveChanges();

                    // Logic tự động gán ảnh chính mới nếu vừa xóa ảnh chính
                    if (wasMain)
                    {
                        var nextImage = db.HinhAnh_BienThe.FirstOrDefault(x => x.IDBienThe == variantId);
                        if (nextImage != null)
                        {
                            nextImage.LaAnhChinh = true;
                            db.SaveChanges();
                        }
                    }

                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Không tìm thấy ảnh." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                var product = db.SanPhams.Find(id);
                if (product == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm." });

                // KIỂM TRA RÀNG BUỘC ĐƠN HÀNG
                // Kiểm tra xem có biến thể nào của sản phẩm này nằm trong ChiTietDonHang không
                bool isSold = db.ChiTietDonHangs.Any(ct => ct.BienTheSanPham.IDSanPham == id);

                if (isSold)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không thể xóa! Sản phẩm này đã có đơn hàng. Hãy chuyển trạng thái sang 'Ngừng bán' thay vì xóa."
                    });
                }

                //  NẾU CHƯA BÁN -> XÓA SẠCH

                // Lấy danh sách biến thể
                var variants = db.BienTheSanPhams.Where(v => v.IDSanPham == id).ToList();
                foreach (var v in variants)
                {
                    // Xóa liên kết ảnh
                    var images = db.HinhAnh_BienThe.Where(h => h.IDBienThe == v.IDBienThe).ToList();
                    db.HinhAnh_BienThe.RemoveRange(images);
                }

                // Xóa biến thể
                db.BienTheSanPhams.RemoveRange(variants);

                // Xóa sản phẩm
                db.SanPhams.Remove(product);

                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ImportStock(int variantId, int quantityToAdd)
        {
            try
            {
                if (quantityToAdd <= 0)
                    return Json(new { success = false, message = "Số lượng nhập phải lớn hơn 0." });

                var variant = db.BienTheSanPhams.Find(variantId);
                if (variant == null)
                    return Json(new { success = false, message = "Không tìm thấy biến thể." });

                // Cộng dồn số lượng
                variant.SoLuongTon += quantityToAdd;
                db.SaveChanges();

                return Json(new { success = true, message = "Nhập kho thành công!", newStock = variant.SoLuongTon });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ApplyDiscount(int id, int discountPercent, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var product = db.SanPhams.Find(id);
                if (product == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm." });

                if (discountPercent < 0 || discountPercent > 100)
                    return Json(new { success = false, message = "% giảm giá không hợp lệ." });

                if (discountPercent == 0)
                {
                    // Nếu nhập 0% -> Hủy khuyến mãi
                    product.GiaKhuyenMai = null;
                    product.NgayBatDauKM = null;
                    product.NgayKetThucKM = null;
                }
                else
                {
                    // Tính giá sau giảm
                    decimal giamGia = product.Gia * discountPercent / 100;
                    product.GiaKhuyenMai = product.Gia - giamGia;

                    // Lưu thời gian (Nếu không chọn thì mặc định là hôm nay đến 30 ngày sau)
                    product.NgayBatDauKM = startDate ?? DateTime.Now;
                    product.NgayKetThucKM = endDate ?? DateTime.Now.AddDays(30);
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Cập nhật chương trình khuyến mãi thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        //public ActionResult GenerateImageFeatures()
        //{
        //    var service = new ImageFeatureService(Server.MapPath("~/App_Data/resnet18-v2-7.onnx"));
        //    var images = db.HinhAnhs.Where(h => h.VectorDacTrung == null).ToList(); // Chỉ làm những ảnh chưa có

        //    foreach (var img in images)
        //    {
        //        try
        //        {
        //            string fullPath = Server.MapPath(img.DuongDan);
        //            if (System.IO.File.Exists(fullPath))
        //            {
        //                // 1. Trích xuất vector
        //                float[] vector = service.GetFeatureVector(fullPath);

        //                // 2. Chuyển thành chuỗi JSON/String để lưu DB
        //                // Ví dụ: "0.123,0.456,0.789..."
        //                img.VectorDacTrung = string.Join(",", vector);
        //            }
        //        }
        //        catch { /* Bỏ qua lỗi nếu ảnh hỏng */ }
        //    }

        //    db.SaveChanges();
        //    return Content($"Đã cập nhật vector cho {images.Count} hình ảnh.");
        //}
        // Areas/Admin/Controllers/ProductsController.cs

        [HttpGet]
        public ActionResult GenerateImageFeatures()
        {
            // 1. Tìm file model
            string modelFileName = "resnet18-v2-7.onnx";
            string modelPath = "";
            var possiblePaths = new System.Collections.Generic.List<string>
    {
        Server.MapPath($"~/App_Data/{modelFileName}"),
        System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", modelFileName),
        System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, modelFileName)
    };

            foreach (var path in possiblePaths)
            {
                if (System.IO.File.Exists(path)) { modelPath = path; break; }
            }

            if (string.IsNullOrEmpty(modelPath))
            {
                return Content($"<h3 style='color:red'>LỖI NGHIÊM TRỌNG: Không tìm thấy file {modelFileName}</h3>");
            }

            // 2. Khởi tạo Service
            FashionHub.Services.ImageFeatureService service;
            try
            {
                service = new FashionHub.Services.ImageFeatureService(modelPath);
            }
            catch (Exception ex)
            {
                return Content($"<h3 style='color:red'>Lỗi khởi tạo ML.NET: {ex.Message}</h3><pre>{ex.StackTrace}</pre>");
            }

            // 3. Lấy danh sách ảnh (Lấy TOÀN BỘ để kiểm tra, không chỉ cái null)
            var images = db.HinhAnhs.ToList();

            var log = new System.Text.StringBuilder();
            log.Append("<h3>BÁO CÁO QUÁ TRÌNH HỌC ẢNH</h3>");
            log.Append("<table border='1' cellpadding='5' style='border-collapse:collapse;'>");
            log.Append("<tr><th>ID</th><th>Đường dẫn ảnh</th><th>Trạng thái File</th><th>Kết quả AI</th></tr>");

            int successCount = 0;

            foreach (var img in images)
            {
                log.Append($"<tr><td>{img.IDHinhAnh}</td><td>{img.DuongDan}</td>");

                try
                {
                    // Kiểm tra đường dẫn vật lý
                    string fullPath = Server.MapPath(img.DuongDan);

                    if (!System.IO.File.Exists(fullPath))
                    {
                        log.Append($"<td style='color:red'>KHÔNG TÌM THẤY FILE<br><small>{fullPath}</small></td><td>Bỏ qua</td>");
                    }
                    else
                    {
                        log.Append($"<td style='color:green'>OK</td>");

                        // Chạy AI
                        float[] vector = service.GetFeatureVector(fullPath);

                        if (vector != null && vector.Length > 0)
                        {
                            img.VectorDacTrung = string.Join(",", vector);
                            log.Append($"<td style='color:green'><b>Thành công!</b><br>Vector dài: {vector.Length}</td>");
                            successCount++;
                        }
                        else
                        {
                            log.Append($"<td style='color:orange'>Vector rỗng (Lỗi lạ)</td>");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // In chi tiết lỗi ra để bạn đọc
                    log.Append($"<td style='color:red'>LỖI AI: {ex.Message}<br><small>{ex.InnerException?.Message}</small></td>");
                }
                log.Append("</tr>");
            }

            log.Append("</table>");

            // Lưu DB
            if (successCount > 0)
            {
                db.SaveChanges();
                log.Insert(0, $"<h2 style='color:green'>Đã lưu thành công {successCount} ảnh vào Database!</h2>");
            }
            else
            {
                log.Insert(0, $"<h2 style='color:red'>Không lưu được ảnh nào!</h2>");
            }

            return Content(log.ToString());
        }



    }
}