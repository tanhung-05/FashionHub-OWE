using FashionHub.Web.Areas.Admin.ViewModels;
using FashionHub.Web.Data;
using FashionHub.Web.Models.Generated;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CouponsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CouponsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Coupons
        public async Task<IActionResult> Index()
        {
            var coupons = await _context.MaGiamGia
                .OrderByDescending(m => m.IdmaGiamGia)
                .ToListAsync();

            var viewModels = coupons.Select(c => new CouponViewModel
            {
                IdmaGiamGia = c.IdmaGiamGia,
                MaCode = c.MaCode,
                TenChuongTrinh = c.TenChuongTrinh,
                LoaiGiamGia = c.LoaiGiamGia,
                GiaTri = c.GiaTri,
                DonHangToiThieu = c.DonHangToiThieu,
                GiamToiDa = c.GiamToiDa,
                SoLuong = c.SoLuong,
                DaSuDung = c.DaSuDung,
                NgayBatDau = c.NgayBatDau,
                NgayKetThuc = c.NgayKetThuc,
                TrangThai = c.TrangThai
            }).ToList();

            return View(viewModels);
        }

        // GET: Admin/Coupons/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Coupons/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CouponViewModel model)
        {
            ValidateCouponDates(model);

            if (ModelState.IsValid)
            {
                // Kiểm tra mã đã tồn tại chưa
                if (await _context.MaGiamGia.AnyAsync(x => x.MaCode == model.MaCode))
                {
                    ModelState.AddModelError("MaCode", "Mã này đã tồn tại.");
                    return View(model);
                }

                var coupon = new MaGiamGium
                {
                    MaCode = model.MaCode,
                    TenChuongTrinh = model.TenChuongTrinh,
                    LoaiGiamGia = model.LoaiGiamGia,
                    GiaTri = model.GiaTri,
                    DonHangToiThieu = model.DonHangToiThieu ?? 0,
                    GiamToiDa = model.GiamToiDa,
                    SoLuong = model.SoLuong,
                    DaSuDung = 0,
                    NgayBatDau = model.NgayBatDau!.Value,
                    NgayKetThuc = model.NgayKetThuc!.Value,
                    NgayTao = DateTime.Now,
                    TrangThai = true
                };

                _context.MaGiamGia.Add(coupon);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Tạo mã giảm giá thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Admin/Coupons/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var coupon = await _context.MaGiamGia.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }

            var viewModel = new CouponViewModel
            {
                IdmaGiamGia = coupon.IdmaGiamGia,
                MaCode = coupon.MaCode,
                TenChuongTrinh = coupon.TenChuongTrinh,
                LoaiGiamGia = coupon.LoaiGiamGia,
                GiaTri = coupon.GiaTri,
                DonHangToiThieu = coupon.DonHangToiThieu,
                GiamToiDa = coupon.GiamToiDa,
                SoLuong = coupon.SoLuong,
                DaSuDung = coupon.DaSuDung,
                NgayBatDau = coupon.NgayBatDau,
                NgayKetThuc = coupon.NgayKetThuc,
                TrangThai = coupon.TrangThai
            };

            return View(viewModel);
        }

        // POST: Admin/Coupons/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CouponViewModel model)
        {
            if (id != model.IdmaGiamGia)
            {
                return NotFound();
            }

            ValidateCouponDates(model);

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra mã đã tồn tại chưa (trừ bản ghi hiện tại)
                    if (await _context.MaGiamGia.AnyAsync(x => x.MaCode == model.MaCode && x.IdmaGiamGia != id))
                    {
                        ModelState.AddModelError("MaCode", "Mã này đã tồn tại.");
                        return View(model);
                    }

                    var coupon = await _context.MaGiamGia.FindAsync(id);
                    if (coupon == null)
                    {
                        return NotFound();
                    }

                    coupon.MaCode = model.MaCode;
                    coupon.TenChuongTrinh = model.TenChuongTrinh;
                    coupon.LoaiGiamGia = model.LoaiGiamGia;
                    coupon.GiaTri = model.GiaTri;
                    coupon.DonHangToiThieu = model.DonHangToiThieu ?? 0;
                    coupon.GiamToiDa = model.GiamToiDa;
                    coupon.SoLuong = model.SoLuong;
                    coupon.NgayBatDau = model.NgayBatDau!.Value;
                    coupon.NgayKetThuc = model.NgayKetThuc!.Value;
                    coupon.TrangThai = model.TrangThai;

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật mã giảm giá thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await CouponExists(model.IdmaGiamGia))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            return View(model);
        }

        // POST: Admin/Coupons/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var coupon = await _context.MaGiamGia.FindAsync(id);
                if (coupon == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy mã giảm giá" });
                }

                // Nếu mã đã dùng rồi thì không xóa hẳn mà chỉ tắt trạng thái
                if (coupon.DaSuDung > 0)
                {
                    coupon.TrangThai = false;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Đã tắt hoạt động mã này." });
                }

                _context.MaGiamGia.Remove(coupon);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã xóa mã giảm giá." });
            }
            catch
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa mã giảm giá" });
            }
        }

        // POST: Admin/Coupons/ToggleStatus
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var coupon = await _context.MaGiamGia.FindAsync(id);
            if (coupon == null)
            {
                return Json(new { success = false, message = "Không tìm thấy mã giảm giá" });
            }

            coupon.TrangThai = !coupon.TrangThai;
            await _context.SaveChangesAsync();

            return Json(new { success = true, newStatus = coupon.TrangThai });
        }

        private async Task<bool> CouponExists(int id)
        {
            return await _context.MaGiamGia.AnyAsync(e => e.IdmaGiamGia == id);
        }

        private void ValidateCouponDates(CouponViewModel model)
        {
            if (!model.NgayBatDau.HasValue)
            {
                ModelState.AddModelError(nameof(model.NgayBatDau), "Ngày bắt đầu là bắt buộc.");
            }

            if (!model.NgayKetThuc.HasValue)
            {
                ModelState.AddModelError(nameof(model.NgayKetThuc), "Ngày kết thúc là bắt buộc.");
            }

            if (model.NgayBatDau.HasValue
                && model.NgayKetThuc.HasValue
                && model.NgayBatDau.Value > model.NgayKetThuc.Value)
            {
                ModelState.AddModelError(nameof(model.NgayKetThuc), "Ngày kết thúc phải sau ngày bắt đầu.");
            }
        }
    }
}
