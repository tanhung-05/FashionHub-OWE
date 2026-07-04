using FashionHub.Models;
using FashionHub.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security; 

namespace FashionHub.Controllers
{
    public class AccountController : Controller
    {
        private QL_SHOPQUANAO_PROEntities db = new QL_SHOPQUANAO_PROEntities();

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var user = db.NguoiDungs.FirstOrDefault(u => u.Email == model.Email);
                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.MatKhauHash))
                {
                    Session["User"] = user;
                    FormsAuthentication.SetAuthCookie(user.Email, model.RememberMe);

                    if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
            }
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult Register(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                if (db.NguoiDungs.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                    return View(model);
                }

                var newUser = new NguoiDung
                {
                    HoTen = model.FullName,
                    Email = model.Email,
                    MatKhauHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    IDVaiTro = 2,
                    NgayTao = DateTime.Now
                };
                db.NguoiDungs.Add(newUser);
                db.SaveChanges();

                Session["User"] = newUser;
                FormsAuthentication.SetAuthCookie(newUser.Email, false);

                if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear();
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public ActionResult UserProfile() 
        {
            var user = Session["User"] as NguoiDung;
            var currentUser = db.NguoiDungs.Find(user.IDNguoiDung); 

            var model = new ProfileViewModel
            {
                HoTen = currentUser.HoTen,
                SoDienThoai = currentUser.SoDienThoai,
                Email = currentUser.Email
            };
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UserProfile(ProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userSession = Session["User"] as NguoiDung;
                var userInDb = db.NguoiDungs.Find(userSession.IDNguoiDung);

                userInDb.HoTen = model.HoTen;
                userInDb.SoDienThoai = model.SoDienThoai;
                db.SaveChanges();

                Session["User"] = userInDb; 
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("UserProfile");
            }
            return View(model);
        }

        [Authorize]
        public ActionResult Addresses()
        {
            var user = Session["User"] as NguoiDung;
            var addresses = db.DiaChis.Where(a => a.IDNguoiDung == user.IDNguoiDung).ToList();
            return View(addresses);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult AddAddressAjax(string tenNguoiNhan, string soDienThoai, string chiTiet, string tinhThanh, string quanHuyen, string phuongXa)
        {
            try
            {
                var user = Session["User"] as NguoiDung;
                if (user == null) return Json(new { success = false, message = "Hết phiên đăng nhập." });

                bool isDefault = !db.DiaChis.Any(d => d.IDNguoiDung == user.IDNguoiDung);

                var newAddress = new DiaChi
                {
                    IDNguoiDung = user.IDNguoiDung,
                    TenNguoiNhan = tenNguoiNhan,
                    SoDienThoai = soDienThoai,
                    ChiTiet = chiTiet,
                    TinhThanh1 = tinhThanh,
                    QuanHuyen1 = quanHuyen,
                    PhuongXa1 = phuongXa,
                    LaMacDinh = isDefault
                };

                db.DiaChis.Add(newAddress);
                db.SaveChanges();

                var addressVM = new AddressViewModel
                {
                    IDDiaChi = newAddress.IDDiaChi,
                    TenNguoiNhan = newAddress.TenNguoiNhan,
                    SoDienThoai = newAddress.SoDienThoai,
                    LaMacDinh = newAddress.LaMacDinh ?? false,
                    FullAddress = $"{newAddress.ChiTiet}, {newAddress.PhuongXa1}, {newAddress.QuanHuyen1}, {newAddress.TinhThanh1}"
                };

                return Json(new { success = true, message = "Thêm địa chỉ thành công!", newAddress = addressVM });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteAddress(int id)
        {
            var user = Session["User"] as NguoiDung;
            var address = db.DiaChis.FirstOrDefault(a => a.IDDiaChi == id && a.IDNguoiDung == user.IDNguoiDung);
            if (address != null)
            {
                db.DiaChis.Remove(address);
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SetDefaultAddress(int id)
        {
            var user = Session["User"] as NguoiDung;
            var oldDefaults = db.DiaChis.Where(a => a.IDNguoiDung == user.IDNguoiDung && a.LaMacDinh == true).ToList();
            foreach (var item in oldDefaults) item.LaMacDinh = false;

            var newDefault = db.DiaChis.FirstOrDefault(a => a.IDDiaChi == id && a.IDNguoiDung == user.IDNguoiDung);
            if (newDefault != null)
            {
                newDefault.LaMacDinh = true;
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userSession = Session["User"] as NguoiDung;
            var userInDb = db.NguoiDungs.Find(userSession.IDNguoiDung);

            if (BCrypt.Net.BCrypt.Verify(model.OldPassword, userInDb.MatKhauHash))
            {
                userInDb.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("UserProfile");
            }
            else
            {
                ModelState.AddModelError("OldPassword", "Mật khẩu hiện tại không đúng.");
                return View(model);
            }
        }
    }
}