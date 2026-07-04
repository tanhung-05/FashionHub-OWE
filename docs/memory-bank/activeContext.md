# Active Context — FashionHub / OWE

## Trạng thái hiện tại
Dự án đã được phân tích ở trạng thái ASP.NET MVC 5 (.NET Framework 4.8). Kết luận định hướng mới là không tiếp tục sửa UI/UX trực tiếp trên project cũ trước, mà migrate nền tảng sang ASP.NET Core MVC .NET 10 trước.

Project cũ `FashionHub/` vẫn được giữ nguyên để tham chiếu trong quá trình migrate.

Đã hoàn thành bước khởi tạo project migrate rỗng:
- Tạo solution mới `FashionHub2/FashionHub2.slnx` nằm cạnh project cũ `FashionHub/`.
- Tạo project `FashionHub2/FashionHub.Web` bằng template ASP.NET Core MVC target `net10.0`.
- Tạo project `FashionHub2/FashionHub.Tests` bằng xUnit target `net10.0`.
- Thêm reference từ `FashionHub.Tests` tới `FashionHub.Web`.
- Thêm cả hai project vào solution `FashionHub2`.
- Cài package nền tảng vào `FashionHub.Web`:
  - `Microsoft.EntityFrameworkCore.SqlServer`
  - `Microsoft.EntityFrameworkCore.Tools`
  - `Microsoft.EntityFrameworkCore.Design`
  - `BCrypt.Net-Next`
  - `X.PagedList.Mvc.Core`
- Đã build/test solution thành công.
- Đã chạy thử `FashionHub.Web` bằng `dotnet run` và xác nhận trang mặc định trả HTTP 200 tại `http://localhost:5099/`.

Đã hoàn thành bước Database First ban đầu cho project mới:
- Cài `dotnet-ef` global tool version `10.0.9`.
- Đọc connection string từ `FashionHub/Web.config`.
- Scaffold EF Core entity từ database `QL_SHOPQUANAO_PRO` vào `FashionHub2/FashionHub.Web/Models/Generated`.
- Scaffold `ApplicationDbContext` vào `FashionHub2/FashionHub.Web/Data/ApplicationDbContext.cs`.
- Thêm connection string `DefaultConnection` vào `FashionHub2/FashionHub.Web/appsettings.Development.json`.
- Đăng ký `ApplicationDbContext` trong `FashionHub2/FashionHub.Web/Program.cs` bằng `AddDbContext` và `UseSqlServer`.

## Quyết định kỹ thuật hiện tại
- Ưu tiên migrate trước, UI/UX sau.
- Tạo project mới `FashionHub2/FashionHub.Web` thay vì chỉnh trực tiếp project cũ.
- Dùng ASP.NET Core MVC trên .NET 10.
- Dùng EF Core SQL Server theo hướng Database First từ database hiện có.
- Dùng Cookie Authentication thay Forms Authentication/Membership cũ.
- Giữ BCrypt.Net-Next để mật khẩu cũ tiếp tục đăng nhập được nếu database hiện tại đang dùng BCrypt.
- Sau migration mới áp dụng roadmap UI/UX: cart mobile, offcanvas filter, accessibility, component consistency, toast/modal.

## Ghi nhận đối chiếu model scaffold
Các entity nghiệp vụ chính đã được scaffold:
- `BienTheSanPham`
- `ChiTietDonHang`
- `DanhMuc`
- `DiaChi`
- `DonHang`
- `GioHang`
- `HinhAnh`
- `HinhAnhBienThe`
- `KichThuoc`
- `MaGiamGium`
- `MauSac`
- `NguoiDung`
- `PhuongThucThanhToan`
- `SanPham`
- `ThuongHieu`
- `TrangThaiDonHang`
- `VaiTro`

Khác biệt đáng chú ý so với EF6 model cũ:
- EF Core scaffold đổi casing tên cột theo kiểu `IdsanPham`, `IddanhMuc`, `IdbienThe` thay vì `IDSanPham`, `IDDanhMuc`, `IDBienThe`.
- Navigation property được đặt theo pattern EF Core như `IdsanPhamNavigation`, `IddanhMucNavigation`, `IdmaGiamGiaNavigation` thay vì tên domain ngắn hơn như `SanPham`, `DanhMuc`, `MaGiamGia`.
- Bảng/link entity `HinhAnh_BienThe` được scaffold thành class `HinhAnhBienThe`.
- Entity `MaGiamGia` bị EF Core singularize thành `MaGiamGium`; cần cân nhắc rename bằng partial/config hoặc chỉnh scaffold nếu muốn giữ tên domain Việt nhất quán.
- Database hiện tại có một số cột chưa có trong EF6 model cũ:
  - `BienTheSanPham.Gia`
  - `SanPham.VectorDacTrung`
  - `DiaChi.PhuongXa`, `DiaChi.QuanHuyen`, `DiaChi.TinhThanh` thay cho tên EF6 cũ `PhuongXa1`, `QuanHuyen1`, `TinhThanh1`.
- EF Core bật nullable reference types theo schema/nullability: nhiều property string nullable được biểu diễn bằng `string?`.

## File/tài liệu đã cập nhật
- `FashionHub-AI-Agent-Roadmap.md`: roadmap tổng thể v2 theo thứ tự migrate → UI/UX → testing → Docker/deploy.
- `.clinerules/00-project-context.md`: context mới cho stack ASP.NET Core MVC .NET 10.
- `.clinerules/01-architecture.md`: architecture rules mới cho project migrate.
- `docs/memory-bank/projectbrief.md`: cập nhật mục tiêu và ưu tiên mới.
- `docs/memory-bank/activeContext.md`: context đang làm hiện tại và ghi nhận bước khởi tạo `FashionHub2` + Database First.
- `docs/memory-bank/progress.md`: cập nhật tiến độ khởi tạo project migrate và Database First.
- `FashionHub2/FashionHub.Web/appsettings.Development.json`: connection string local development.
- `FashionHub2/FashionHub.Web/Program.cs`: đăng ký EF Core DbContext.
- `FashionHub2/FashionHub.Web/Data/ApplicationDbContext.cs`: DbContext scaffold từ database.
- `FashionHub2/FashionHub.Web/Models/Generated/**`: entity scaffold từ database.

## Việc cần làm tiếp theo
1. Build/verify lại solution sau khi scaffold EF Core.
2. Nếu build ổn định, commit riêng cho task Database First.
3. Sau đó chuyển sang giai đoạn Authentication:
   - cấu hình Cookie Authentication,
   - dựng lại AccountController theo ASP.NET Core MVC,
   - dùng `NguoiDung`/`VaiTro` từ EF Core,
   - giữ BCrypt.Net-Next nếu mật khẩu database đang dùng BCrypt.

## Lưu ý quan trọng cho các task sau
- Không xoá, di chuyển hoặc refactor project cũ `FashionHub/` khi chưa có yêu cầu rõ ràng.
- Mọi code mới phải đi vào `FashionHub2/FashionHub.Web/`.
- Không dùng `System.Web.Mvc` trong project mới.
- Không dùng đường dẫn static cũ `~/Content/...`, `~/Scripts/...` trong project mới.
- Khi migrate View, cần chuyển dần sang Tag Helpers của ASP.NET Core.
- UI/UX guidelines cũ vẫn có giá trị, nhưng áp dụng sau khi migration ổn định.
- Cần đặc biệt chú ý khác biệt tên property scaffold (`Id...`, `...Navigation`, `MaGiamGium`) khi port controller/service/viewmodel từ project cũ.