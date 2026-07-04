# FashionHub (OWE) — Kế hoạch tổng thể v2: Migrate .NET 10 → Hoàn thiện UI/UX → Deploy

> Bản này thay thế thứ tự giai đoạn trước đó.
> Nội dung thiết lập `.clinerules`, Memory Bank và các prompt UI/UX/testing/CV cũ vẫn dùng được, nhưng áp dụng sau khi migration xong thay vì áp dụng ngay trên .NET Framework.

---

## 0. Vì sao migrate trước

- Sửa UI/UX ở Razor View + CSS/JS sẽ phải rà lại nếu làm trước khi chuyển sang ASP.NET Core.
- ASP.NET Core thay đổi nhiều điểm: `@Scripts.Render` không còn, static files chuyển từ `Content/`/`Scripts/` sang `wwwroot/`, partial view nên dùng Tag Helper.
- Không có deadline gấp nên ưu tiên thứ tự kỹ thuật: migration trước, hoàn thiện UI/UX sau, cuối cùng deploy.

---

## 1. Kiến trúc đích

Mục tiêu: ASP.NET Core MVC trên .NET 10, giữ nguyên database SQL Server hiện có.

```text
FashionHub2/
├── FashionHub.Web/
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Areas/Admin/
│   ├── Controllers/
│   ├── Models/
│   ├── ViewModels/
│   ├── Views/
│   ├── Services/
│   ├── Data/ApplicationDbContext.cs
│   └── wwwroot/
│       ├── css/
│       ├── js/
│       └── images/
├── FashionHub.Tests/
└── docker-compose.yml
```

## 2. Mapping công nghệ

| Cũ (.NET Framework 4.8) | Mới (.NET 10) |
|---|---|
| `System.Web.Mvc` | `Microsoft.AspNetCore.Mvc` |
| `Global.asax.cs`, `App_Start/*` | `Program.cs` |
| `Web.config` | `appsettings.json` + biến môi trường |
| Entity Framework 6 | EF Core SQL Server |
| Forms Authentication | Cookie Authentication |
| `PagedList.Mvc` | `X.PagedList.Mvc.Core` |
| `Content/`, `Scripts/` | `wwwroot/css/`, `wwwroot/js/` |

---

## 3. Giai đoạn migration

### 3.1. Khởi tạo project mới
- Tạo solution `FashionHub2` nằm cạnh project cũ.
- Tạo `FashionHub.Web` bằng template ASP.NET Core MVC target .NET 10.
- Tạo `FashionHub.Tests` bằng xUnit, tham chiếu `FashionHub.Web`.
- Cài package: EF Core SQL Server/Tools/Design, BCrypt.Net-Next, X.PagedList.Mvc.Core.
- Chạy thử trang mặc định, chưa copy code cũ.

### 3.2. Scaffold database
- Dùng `dotnet ef dbcontext scaffold` từ database SQL Server hiện có.
- Sinh entity vào `Models/Generated`.
- Sinh `ApplicationDbContext` vào `Data/`.
- Đối chiếu tên model/kiểu dữ liệu với project cũ trước khi tiếp tục.

### 3.3. Đăng nhập
- Dựng Cookie Authentication trong `Program.cs`.
- Giữ bảng `NguoiDung` và BCrypt.Net-Next để mật khẩu cũ vẫn dùng được.
- Viết lại `AccountController` với Login/Register/Logout bằng `ClaimsPrincipal`.
- Dùng `[Authorize]` và `[Authorize(Roles = "Admin")]` đúng nơi cần.

### 3.4. Chuyển Services
- Chuyển EF6 DbContext sang EF Core `ApplicationDbContext`.
- Ưu tiên async: `ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`.
- Đăng ký service qua interface trong `Program.cs`.
- Không đổi logic nghiệp vụ nếu chưa cần.

### 3.5. Chuyển Controller + View
Thứ tự: Home → Products → Cart → Order → Account → Admin/ManageOrder → Chat.

Khi chuyển:
- `ActionResult` → `IActionResult`/`ViewResult`.
- `System.Web.Mvc` → `Microsoft.AspNetCore.Mvc`.
- `~/Content/...` → `~/css/...`.
- `~/Scripts/...` → `~/js/...`.
- `@Html.Partial(...)` → `<partial name="..." />`.
- `@Scripts.Render`/`@Styles.Render` → thẻ `<script>`/`<link>` trực tiếp.
- PagedList chuyển sang `X.PagedList.Mvc.Core`.

### 3.6. Kiểm tra chéo
- So sánh route/action giữa project cũ và mới.
- Liệt kê action còn thiếu.
- Chỉ archive project cũ sau khi migration đủ chức năng.

---

## 4. UI/UX sau migration

Áp dụng roadmap UI/UX trên code mới:
- Dọn inline style và `<style>` trong partial.
- Làm cart mobile responsive.
- Làm filter offcanvas trên mobile.
- Chuẩn hóa toast/modal, bỏ `alert()`/`confirm()`.
- Cải thiện accessibility cơ bản.
- Dùng Tag Helpers (`asp-for`, `asp-action`) ở các View được sửa.

Đường dẫn mới:
- Views: `FashionHub2/FashionHub.Web/Views/...`
- CSS: `FashionHub2/FashionHub.Web/wwwroot/css/site.css`
- JS: `FashionHub2/FashionHub.Web/wwwroot/js/site.js`

---

## 5. Testing

- Dùng `FashionHub.Tests` với xUnit.
- Test service: tính giá khuyến mãi, áp coupon, kiểm tra tồn kho.
- Dùng EF Core InMemory provider cho unit test, không cần SQL Server thật.

---

## 6. Docker & Deploy

### Docker
- Tạo Dockerfile multi-stage cho `FashionHub.Web`.
- Tạo `docker-compose.yml` gồm `web` và `db` SQL Server.
- Web đọc connection string từ biến môi trường.

### Deploy
Có thể chọn:
- Render/Railway
- Fly.io
- Azure Linux App Service
- VPS + Nginx

Tạo `README-DEPLOY.md` sau khi chọn nền tảng.

---

## 7. Đóng gói cho CV

Điểm nhấn nên ghi:
> Migrate hệ thống từ ASP.NET MVC 5 (.NET Framework) sang ASP.NET Core MVC (.NET 10), Dockerize và deploy production.

---

## Checklist tổng v2

- [ ] Cập nhật `.clinerules` cho stack .NET 10
- [ ] Dựng project `FashionHub.Web` (.NET 10) + `FashionHub.Tests` rỗng, chạy được
- [ ] Scaffold Models + DbContext từ database có sẵn
- [ ] Dựng lại đăng nhập bằng Cookie Authentication
- [ ] Chuyển Services từ EF6 sang EF Core
- [ ] Chuyển Controller + View: Home → Products → Cart → Order → Account → Admin → Chat
- [ ] Đối chiếu route cũ/mới, archive project cũ
- [ ] Áp dụng lại roadmap UI/UX trên code mới
- [ ] Viết unit test bằng xUnit + EF Core InMemory
- [ ] Dockerize
- [ ] Chọn nền tảng và deploy
- [ ] Hoàn thiện README/demo/mô tả CV