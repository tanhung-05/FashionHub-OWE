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

## Quyết định kỹ thuật hiện tại
- Ưu tiên migrate trước, UI/UX sau.
- Tạo project mới `FashionHub2/FashionHub.Web` thay vì chỉnh trực tiếp project cũ.
- Dùng ASP.NET Core MVC trên .NET 10.
- Dùng EF Core SQL Server theo hướng Database First từ database hiện có.
- Dùng Cookie Authentication thay Forms Authentication/Membership cũ.
- Giữ BCrypt.Net-Next để mật khẩu cũ tiếp tục đăng nhập được nếu database hiện tại đang dùng BCrypt.
- Sau migration mới áp dụng roadmap UI/UX: cart mobile, offcanvas filter, accessibility, component consistency, toast/modal.

## File/tài liệu đã cập nhật
- `FashionHub-AI-Agent-Roadmap.md`: roadmap tổng thể v2 theo thứ tự migrate → UI/UX → testing → Docker/deploy.
- `.clinerules/00-project-context.md`: context mới cho stack ASP.NET Core MVC .NET 10.
- `.clinerules/01-architecture.md`: architecture rules mới cho project migrate.
- `docs/memory-bank/projectbrief.md`: cập nhật mục tiêu và ưu tiên mới.
- `docs/memory-bank/activeContext.md`: context đang làm hiện tại và ghi nhận bước khởi tạo `FashionHub2`.
- `docs/memory-bank/progress.md`: cập nhật tiến độ khởi tạo project migrate.

## Việc cần làm tiếp theo
1. Bắt đầu giai đoạn Database First cho project mới:
   - nhận connection string SQL Server từ người dùng,
   - scaffold entity vào `FashionHub2/FashionHub.Web/Models/Generated`,
   - scaffold `ApplicationDbContext` vào `FashionHub2/FashionHub.Web/Data`,
   - đối chiếu entity scaffold với domain/model cũ.
2. Sau khi scaffold ổn định, chuyển dần Authentication, Services, Controllers và Views theo từng nhóm chức năng.

## Lưu ý quan trọng cho các task sau
- Không xoá, di chuyển hoặc refactor project cũ `FashionHub/` khi chưa có yêu cầu rõ ràng.
- Mọi code mới phải đi vào `FashionHub2/FashionHub.Web/`.
- Không dùng `System.Web.Mvc` trong project mới.
- Không dùng đường dẫn static cũ `~/Content/...`, `~/Scripts/...` trong project mới.
- Khi migrate View, cần chuyển dần sang Tag Helpers của ASP.NET Core.
- UI/UX guidelines cũ vẫn có giá trị, nhưng áp dụng sau khi migration ổn định.