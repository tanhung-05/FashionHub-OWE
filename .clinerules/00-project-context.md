# Project Context — FashionHub (OWE)

## Tổng quan
FashionHub là website bán quần áo/thời trang, UI chính bằng tiếng Việt.

Project hiện tại là ASP.NET MVC 5 trên .NET Framework 4.8. Định hướng mới là migrate sang ASP.NET Core MVC trên .NET 10 bằng project mới `FashionHub2/FashionHub.Web`, làm song song và không xoá project cũ cho tới khi migration hoàn tất.

## Trạng thái kỹ thuật hiện tại
- Project cũ: `FashionHub/`
  - ASP.NET MVC 5.3.0 trên .NET Framework 4.8
  - Entity Framework 6.5.1
  - Razor Views (`.cshtml`)
  - CSS chính: `FashionHub/Content/site.css`
  - JavaScript chính: `FashionHub/Scripts/site.js`
- Project đích: `FashionHub2/FashionHub.Web/`
  - ASP.NET Core MVC trên .NET 10
  - EF Core SQL Server
  - Cookie Authentication
  - Static files trong `wwwroot/`

## Quy tắc quan trọng cho code mới
Mọi code mới cho project migrate phải viết theo convention ASP.NET Core MVC/.NET 10:

- Controller kế thừa `Microsoft.AspNetCore.Mvc.Controller`.
- Action trả về `IActionResult`, `ViewResult`, `JsonResult` hoặc kiểu result tương ứng của ASP.NET Core.
- Không dùng `System.Web.Mvc` trong project mới.
- Dependency Injection qua constructor.
- Đăng ký service trong `Program.cs`.
- Không dùng static class cho business logic.
- Cấu hình đọc từ `appsettings.json`, biến môi trường, `IConfiguration` hoặc `IOptions`.
- Không dùng `Web.config` cho project mới.
- Static file nằm trong `wwwroot/`:
  - CSS: `~/css/...`
  - JS: `~/js/...`
  - Images: `~/images/...`
- Không dùng đường dẫn cũ `~/Content/...` hoặc `~/Scripts/...` trong project mới.

## Kiến trúc thư mục project mới
- `FashionHub2/FashionHub.Web/Program.cs`: thay `Global.asax` và `App_Start/*`.
- `FashionHub2/FashionHub.Web/appsettings.json`: cấu hình thay `Web.config`.
- `FashionHub2/FashionHub.Web/Controllers/`: nhận request, điều hướng flow, gọi service/model/viewmodel.
- `FashionHub2/FashionHub.Web/Models/Generated/`: entity scaffold từ database hiện có.
- `FashionHub2/FashionHub.Web/Data/ApplicationDbContext.cs`: EF Core DbContext.
- `FashionHub2/FashionHub.Web/ViewModels/`: dữ liệu chuyên biệt truyền ra View.
- `FashionHub2/FashionHub.Web/Views/`: Razor UI, layout, partial view.
- `FashionHub2/FashionHub.Web/Services/`: business logic, xử lý nghiệp vụ dùng lại.
- `FashionHub2/FashionHub.Web/wwwroot/css/`: CSS.
- `FashionHub2/FashionHub.Web/wwwroot/js/`: JavaScript.
- `FashionHub2/FashionHub.Web/wwwroot/images/`: hình ảnh/static assets.
- `FashionHub2/FashionHub.Web/Areas/Admin/`: khu vực quản trị.
- `FashionHub2/FashionHub.Tests/`: xUnit tests.

## Entity nghiệp vụ chính
Giữ nguyên domain nghiệp vụ khi migrate:
- `SanPham`: sản phẩm thời trang.
- `DanhMuc`: danh mục sản phẩm.
- `BienThe`: biến thể sản phẩm theo màu/kích thước/tồn kho.
- `MauSac`, `KichThuoc`: thuộc tính biến thể.
- `DonHang`, `ChiTietDonHang`: đơn hàng và chi tiết đơn.
- `NguoiDung`, `DiaChi`: người dùng và địa chỉ.
- `GioHang`, `ChiTietGioHang`: giỏ hàng.
- `ThuongHieu`, `HinhAnhSanPham`: thương hiệu và hình ảnh.
- `KhuyenMai`, `MaGiamGia`: khuyến mãi và mã giảm giá.

## Thứ tự ưu tiên hiện tại
1. Migrate nền tảng sang ASP.NET Core MVC .NET 10.
2. Scaffold EF Core từ database SQL Server hiện có.
3. Chuyển Authentication, Services, Controllers, Views theo từng nhóm chức năng.
4. Sau khi migration ổn định mới tối ưu UI/UX.
5. Viết test, Dockerize và deploy.