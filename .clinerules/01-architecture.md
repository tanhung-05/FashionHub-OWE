# Architecture Rules

## Nguyên tắc chung
- Project đang trong giai đoạn migrate từ ASP.NET MVC 5 (.NET Framework 4.8, EF6) sang ASP.NET Core MVC (.NET 10, EF Core).
- Project cũ `FashionHub/` chỉ dùng để tham chiếu cho đến khi migration hoàn tất.
- Mọi code mới phải đặt trong project mới `FashionHub2/FashionHub.Web/` theo convention ASP.NET Core MVC.
- Giữ kiến trúc rõ ràng: Controller → Service → Model/ViewModel → View.
- Controller chỉ điều phối request/response, không chứa business logic phức tạp.
- Business logic phải đưa vào `Services/` để tái sử dụng và dễ test.

## ASP.NET Core conventions cho project mới
- Controller kế thừa `Microsoft.AspNetCore.Mvc.Controller`.
- Không dùng `System.Web.Mvc` trong project mới.
- Action trả về `IActionResult`, `ViewResult`, `JsonResult` hoặc result type tương ứng của ASP.NET Core.
- Dependency Injection qua constructor.
- Service phải được đăng ký trong `Program.cs`, ví dụ `builder.Services.AddScoped<IService, Service>()`.
- Cấu hình đọc từ `appsettings.json`, biến môi trường, `IConfiguration` hoặc `IOptions`.
- Không dùng `Web.config`, `Global.asax`, `App_Start/*` cho project mới.
- Static files nằm trong `wwwroot/`, không dùng `Content/` hoặc `Scripts/` của project cũ.

## Controllers
- Controller nên mỏng, dễ đọc, mỗi action có trách nhiệm rõ ràng.
- Action chỉ nên:
  - Validate input.
  - Gọi service/query cần thiết.
  - Chuẩn bị ViewModel.
  - Trả về View/PartialView/Json/Redirect.
- Không nhồi xử lý giỏ hàng, đơn hàng, khuyến mãi, tồn kho trực tiếp trong Controller nếu logic dài.
- Với action cần đăng nhập, dùng `[Authorize]`.
- Với khu vực admin, dùng `[Authorize(Roles = "Admin")]`.

## Services
- Đặt nghiệp vụ dùng lại vào `Services/`.
- Service xử lý các flow như:
  - Giỏ hàng.
  - Đặt hàng.
  - Áp dụng mã giảm giá.
  - Tính giá/tồn kho.
  - Xử lý chat/AI.
- Service không phụ thuộc vào Razor View.
- Service nên phụ thuộc vào interface để dễ test.
- Ưu tiên async với EF Core: `ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`.

## Models và Entity Framework
- Project mới dùng EF Core SQL Server.
- Entity scaffold từ database hiện có đặt trong `Models/Generated/`.
- `ApplicationDbContext` đặt trong `Data/ApplicationDbContext.cs`.
- Không query EF trực tiếp trong Razor View.
- Không đưa `DbContext` vào View.
- Query dữ liệu ở Controller/Service, sau đó map ra ViewModel.
- Không đổi schema nghiệp vụ khi migrate nếu chưa có yêu cầu rõ ràng.

## ViewModels
- Dùng ViewModel cho dữ liệu truyền ra View.
- Không truyền entity EF phức tạp trực tiếp ra View nếu View chỉ cần một phần dữ liệu.
- ViewModel nên được thiết kế theo nhu cầu từng màn hình:
  - ProductCardViewModel.
  - ProductDetailViewModel.
  - CheckoutViewModel.
  - CartItemViewModel.

## Views
- View chỉ render UI và xử lý Razor tối thiểu.
- Không đặt business logic, query database, hoặc xử lý nghiệp vụ trong `.cshtml`.
- Partial view chỉ phục vụ tái sử dụng UI component.
- Trong project mới, ưu tiên Tag Helpers:
  - `<partial name="_PartialName" />`
  - `asp-controller`
  - `asp-action`
  - `asp-for`
- Không dùng `@Scripts.Render` hoặc `@Styles.Render` trong project mới.
- Static file trong project mới dùng:
  - CSS: `~/css/...`
  - JS: `~/js/...`
  - Images: `~/images/...`