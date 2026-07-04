# Tech Context — FashionHub / OWE

## Project cũ hiện tại

### Nền tảng
- ASP.NET MVC 5.3.0
- .NET Framework 4.8
- Entity Framework 6.5.1
- SQL Server
- Razor Views (`.cshtml`)
- `Global.asax` và `App_Start/*`
- Cấu hình qua `Web.config`

### Frontend/static files
- CSS chính: `FashionHub/Content/site.css`
- JavaScript chính: `FashionHub/Scripts/site.js`
- Views: `FashionHub/Views/**`
- Shared layout/partials: `FashionHub/Views/Shared/**`
- Bootstrap 5.3.x qua CDN/local reference tuỳ layout hiện tại
- jQuery 3.7.x

### Vai trò project cũ
Project cũ `FashionHub/` là nguồn tham chiếu nghiệp vụ, UI hiện tại, Controller/View/Service cũ trong quá trình migrate. Không tiếp tục mở rộng code mới dài hạn trong project này trừ khi có yêu cầu rõ ràng.

## Project đích

### Nền tảng mục tiêu
- ASP.NET Core MVC trên .NET 10
- EF Core SQL Server
- Cookie Authentication
- BCrypt.Net-Next
- X.PagedList.Mvc.Core
- xUnit cho testing
- Docker và docker-compose

### Thư mục mục tiêu
```text
FashionHub2/
├── FashionHub.Web/
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Areas/Admin/
│   ├── Controllers/
│   ├── Models/Generated/
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

## Mapping kỹ thuật khi migrate

| Thành phần cũ | Thành phần mới |
|---|---|
| `System.Web.Mvc.Controller` | `Microsoft.AspNetCore.Mvc.Controller` |
| `ActionResult` | `IActionResult`, `ViewResult`, `JsonResult` |
| `Global.asax.cs`, `RouteConfig`, `FilterConfig`, `BundleConfig` | `Program.cs` |
| `Web.config` | `appsettings.json`, environment variables, `IConfiguration`, `IOptions` |
| Entity Framework 6 | EF Core SQL Server |
| Forms Authentication/Membership | Cookie Authentication |
| `Content/` | `wwwroot/css/`, `wwwroot/images/` |
| `Scripts/` | `wwwroot/js/` |
| `@Scripts.Render`, `@Styles.Render` | explicit `<script>` and `<link>` tags |
| `@Html.Partial(...)` | `<partial name="..." />` Tag Helper |
| `PagedList.Mvc` | `X.PagedList.Mvc.Core` |

## Quy tắc code cho project mới
- Không dùng `System.Web.Mvc`.
- Không dùng `Web.config`, `Global.asax`, `App_Start/*`.
- Controller mỏng, logic nghiệp vụ đưa vào `Services/`.
- Dependency Injection qua constructor.
- Service đăng ký trong `Program.cs`.
- Dùng async EF Core khi phù hợp.
- Không query database trong Razor View.
- Dùng ViewModel cho dữ liệu View.
- Static files dùng đường dẫn:
  - CSS: `~/css/...`
  - JS: `~/js/...`
  - Images: `~/images/...`

## Database
- Database SQL Server hiện có được giữ nguyên.
- Ưu tiên Database First bằng `dotnet ef dbcontext scaffold`.
- Entity scaffold đặt trong `Models/Generated/`.
- `ApplicationDbContext` đặt trong `Data/ApplicationDbContext.cs`.
- Sau scaffold cần đối chiếu schema/model sinh ra với Models cũ.

## Authentication
- Migrate sang Cookie Authentication của ASP.NET Core.
- Giữ bảng `NguoiDung`.
- Giữ BCrypt.Net-Next nếu mật khẩu hiện tại đang hash bằng BCrypt để tránh phá dữ liệu đăng nhập cũ.
- Sử dụng `ClaimsPrincipal` cho thông tin đăng nhập.
- Dùng `[Authorize]` và `[Authorize(Roles = "Admin")]` ở action/khu vực cần bảo vệ.

## UI/UX
UI/UX sẽ được hiện đại hoá sau migration, không ưu tiên sửa trực tiếp trên project cũ. Các hạng mục sau migration:
- Chuẩn hoá layout/header/footer trên ASP.NET Core.
- Dọn inline style.
- Chuẩn hoá component CSS trong `wwwroot/css/site.css`.
- Cart responsive/mobile-friendly.
- Filter offcanvas trên mobile.
- Toast/modal thay cho `alert()`/`confirm()`.
- Accessibility: label, aria-label, alt text, focus state.
- Dùng Tag Helpers khi chỉnh View.

## Testing và Deploy
- Unit test đặt trong `FashionHub2/FashionHub.Tests`.
- Dùng xUnit.
- Dùng EF Core InMemory cho test service.
- Dockerfile multi-stage cho `FashionHub.Web`.
- `docker-compose.yml` gồm web + SQL Server db.
- Deploy sau khi Dockerize, có thể chọn Render/Railway/Fly.io/Azure Linux App Service/VPS.