# Progress — FashionHub / OWE

## Trạng thái tổng quan
Dự án đã hoàn thành bước phân tích project ASP.NET MVC 5 hiện tại và đã chốt lại chiến lược mới: migrate sang ASP.NET Core MVC .NET 10 trước, sau đó mới tối ưu UI/UX, testing, Dockerize và deploy.

Bước khởi tạo project migrate rỗng đã hoàn tất trong `FashionHub2/`. Project cũ `FashionHub/` chưa bị copy/chỉnh sửa trong bước này.

Bước Database First ban đầu cho project mới đã hoàn tất: EF Core đã scaffold entity và `ApplicationDbContext` từ database SQL Server hiện có vào `FashionHub2/FashionHub.Web`.

Bước Authentication nền tảng cho project mới đã hoàn tất: đã cấu hình Cookie Authentication, thêm `AccountController` ASP.NET Core MVC, thêm `LoginViewModel`/`RegisterViewModel`, dùng EF Core + BCrypt.Net-Next và xác thực bằng claims cookie.

## Đã hoàn thành
- [x] Phân tích project cũ `FashionHub/`.
- [x] Xác định stack hiện tại:
  - ASP.NET MVC 5.3.0
  - .NET Framework 4.8
  - Entity Framework 6.5.1
  - Razor Views
  - Bootstrap 5.3.x
  - jQuery 3.7.x
- [x] Phân tích UI/UX hiện tại.
- [x] Xác định không nên sửa UI/UX lớn trên project cũ trước migration.
- [x] Chốt roadmap v2: migrate → UI/UX → testing → Docker/deploy.
- [x] Tạo/cập nhật `FashionHub-AI-Agent-Roadmap.md`.
- [x] Cập nhật `.clinerules/00-project-context.md`.
- [x] Cập nhật `.clinerules/01-architecture.md`.
- [x] Cập nhật `docs/memory-bank/projectbrief.md`.
- [x] Cập nhật `docs/memory-bank/activeContext.md`.
- [x] Cập nhật `docs/memory-bank/techContext.md`.
- [x] Cập nhật `docs/memory-bank/progress.md`.
- [x] Tạo solution mới `FashionHub2/FashionHub2.slnx` nằm cạnh project cũ.
- [x] Tạo project `FashionHub2/FashionHub.Web` bằng template ASP.NET Core MVC target `net10.0`.
- [x] Tạo project `FashionHub2/FashionHub.Tests` bằng xUnit target `net10.0`.
- [x] Thêm reference từ `FashionHub.Tests` tới `FashionHub.Web`.
- [x] Thêm hai project vào solution `FashionHub2`.
- [x] Cài package nền tảng vào `FashionHub.Web`:
  - `Microsoft.EntityFrameworkCore.SqlServer`
  - `Microsoft.EntityFrameworkCore.Tools`
  - `Microsoft.EntityFrameworkCore.Design`
  - `BCrypt.Net-Next`
  - `X.PagedList.Mvc.Core`
- [x] Build solution `FashionHub2/FashionHub2.slnx` thành công.
- [x] Chạy test project `FashionHub.Tests` thành công.
- [x] Chạy thử `FashionHub.Web` bằng `dotnet run` và xác nhận trang mặc định trả HTTP 200 tại `http://localhost:5099/`.
- [x] Cài `dotnet-ef` global tool version `10.0.9`.
- [x] Đọc connection string từ `FashionHub/Web.config`.
- [x] Scaffold EF Core entity vào `FashionHub2/FashionHub.Web/Models/Generated`.
- [x] Scaffold `ApplicationDbContext` vào `FashionHub2/FashionHub.Web/Data/ApplicationDbContext.cs`.
- [x] Thêm connection string `DefaultConnection` vào `FashionHub2/FashionHub.Web/appsettings.Development.json`.
- [x] Đăng ký `ApplicationDbContext` trong `FashionHub2/FashionHub.Web/Program.cs`.
- [x] Đối chiếu sơ bộ model scaffold với EF6 model cũ và ghi nhận khác biệt chính.
- [x] Cấu hình Cookie Authentication trong `FashionHub2/FashionHub.Web/Program.cs`.
- [x] Dựng lại `AccountController` theo ASP.NET Core MVC.
- [x] Thêm `LoginViewModel` và `RegisterViewModel` cho project mới.
- [x] Giữ BCrypt.Net-Next cho login/register với mật khẩu BCrypt hiện có.
- [x] Chạy `dotnet test FashionHub2\FashionHub.Tests\FashionHub.Tests.csproj --no-restore` thành công sau bước Authentication.

## Đang làm
- Chuẩn bị commit riêng cho task Authentication nền tảng.

## Việc cần làm tiếp theo

### Giai đoạn 1 — Khởi tạo project mới
- [x] Tạo solution mới `FashionHub2` nằm cạnh project cũ.
- [x] Tạo project `FashionHub.Web` bằng template ASP.NET Core MVC target .NET 10.
- [x] Tạo project test `FashionHub.Tests` bằng xUnit.
- [x] Thêm reference từ `FashionHub.Tests` tới `FashionHub.Web`.
- [x] Cài package nền tảng:
  - `Microsoft.EntityFrameworkCore.SqlServer`
  - `Microsoft.EntityFrameworkCore.Tools`
  - `Microsoft.EntityFrameworkCore.Design`
  - `BCrypt.Net-Next`
  - `X.PagedList.Mvc.Core`
- [x] Chạy thử project rỗng.

### Giai đoạn 2 — Database First
- [x] Nhận/đọc connection string SQL Server từ project cũ.
- [x] Scaffold entity vào `FashionHub.Web/Models/Generated`.
- [x] Scaffold `ApplicationDbContext` vào `FashionHub.Web/Data`.
- [x] Đăng ký `ApplicationDbContext` trong `Program.cs`.
- [x] Đối chiếu model scaffold với model cũ.
- [x] Build/verify solution sau scaffold.
- [ ] Commit riêng cho task Database First.

### Giai đoạn 3 — Authentication
- [x] Cấu hình Cookie Authentication trong `Program.cs`.
- [x] Dựng lại AccountController.
- [x] Giữ BCrypt.Net-Next.
- [ ] Migrate Account Views cho Login/Register/AccessDenied.
- [ ] Bổ sung các action/màn hình tài khoản còn lại: hồ sơ, địa chỉ, đổi mật khẩu.
- [ ] Thêm Authorize/Role cho khu vực cần bảo vệ.

### Giai đoạn 4 — Services
- [ ] Chuyển service cũ sang `FashionHub.Web/Services`.
- [ ] Thay EF6 bằng EF Core.
- [ ] Đăng ký service qua interface trong `Program.cs`.

### Giai đoạn 5 — Controllers + Views
- [ ] Chuyển Home.
- [ ] Chuyển Products.
- [ ] Chuyển Cart.
- [ ] Chuyển Order.
- [ ] Chuyển Account.
- [ ] Chuyển Admin/ManageOrder.
- [ ] Chuyển Chat nếu tiếp tục dùng.

### Giai đoạn 6 — Sau migration
- [ ] Đối chiếu route/action cũ và mới.
- [ ] Áp dụng roadmap UI/UX trên code mới.
- [ ] Viết unit test.
- [ ] Dockerize.
- [ ] Deploy.
- [ ] Hoàn thiện README/demo/CV.

## Ghi chú kỹ thuật Database First
- Các entity scaffold chính: `BienTheSanPham`, `ChiTietDonHang`, `DanhMuc`, `DiaChi`, `DonHang`, `GioHang`, `HinhAnh`, `HinhAnhBienThe`, `KichThuoc`, `MaGiamGium`, `MauSac`, `NguoiDung`, `PhuongThucThanhToan`, `SanPham`, `ThuongHieu`, `TrangThaiDonHang`, `VaiTro`.
- Khác biệt tên đáng chú ý:
  - `IDSanPham`/`IDDanhMuc`/`IDBienThe` trong EF6 thành `IdsanPham`/`IddanhMuc`/`IdbienThe` trong EF Core.
  - Navigation property EF Core có hậu tố `Navigation`.
  - `HinhAnh_BienThe` thành `HinhAnhBienThe`.
  - `MaGiamGia` thành `MaGiamGium`.
- Cột mới/khác so với EF6 model cũ:
  - `BienTheSanPham.Gia`
  - `SanPham.VectorDacTrung`
  - `DiaChi.PhuongXa`, `DiaChi.QuanHuyen`, `DiaChi.TinhThanh`

## Lưu ý
- Không xoá project cũ `FashionHub/` khi chưa hoàn tất migration.
- Không đưa code mới dài hạn vào project cũ nếu task thuộc migration.
- Không dùng pattern ASP.NET MVC 5 trong project mới.
- UI/UX improvement sẽ thực hiện sau khi nền tảng ASP.NET Core MVC .NET 10 đã ổn định.