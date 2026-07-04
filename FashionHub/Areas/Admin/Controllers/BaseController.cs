using FashionHub.Models;
using System.Web.Mvc;
using System.Web.Routing;

namespace FashionHub.Areas.Admin.Controllers
{
    public class BaseController : Controller
    {
        /// Phương thức này được tự động gọi trước khi bất kỳ Action nào trong một Controller
        /// kế thừa từ BaseController được thực thi.
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var user = Session["User"] as NguoiDung;

            if (user == null)
            {
                // Nếu 'user' là null, nghĩa là chưa đăng nhập (hoặc Session đã hết hạn).
                // Chuyển hướng người dùng về trang đăng nhập.
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new
                    {
                        controller = "Account", // Controller xử lý đăng nhập
                        action = "Login",
                        area = "" // Quan trọng: Chỉ định area rỗng để thoát ra khỏi Admin Area
                    })
                );
            }
            else
            {
                // KIỂM TRA PHÂN QUYỀN (CÓ PHẢI LÀ ADMIN KHÔNG?)
                // Nếu người dùng đã đăng nhập, chúng ta tiếp tục kiểm tra vai trò của họ.

                if (user.IDVaiTro != 1)
                {
                    // Nếu IDVaiTro không phải là 1, người này không phải Admin.
                    // Chặn truy cập và hiển thị một thông báo lỗi.

                    // Tùy chọn 1: Chuyển hướng về trang chủ client
                    filterContext.Result = new RedirectToRouteResult(
                        new RouteValueDictionary(new
                        {
                            controller = "Home",
                            action = "Index",
                            area = ""
                        })
                    );

                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
}