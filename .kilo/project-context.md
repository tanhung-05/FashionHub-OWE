# FashionHub - Project Context

## Tổng quan
**FashionHub (OWE)** - E-commerce platform bán thời trang, đã migrate từ ASP.NET MVC 5 (.NET Framework 4.8) sang ASP.NET Core MVC (.NET 10).

## Mục đích
Portfolio cá nhân cho sinh viên CNTT chuẩn bị hồ sơ thực tập - KHÔNG phải production system với user thật.

## Kiến trúc chính
- **Framework**: ASP.NET Core MVC (.NET 10 LTS)
- **Database**: SQL Server, database-first approach
- **ORM**: Entity Framework Core
- **Authentication**: Cookie Authentication (không dùng JWT)
- **Password Hashing**: BCrypt.Net-Next
- **AI Chat**: Gemini API (gemini-2.0-flash-exp)

## Cấu trúc thư mục
```
FashionHub2/
├── FashionHub.Web/              # Main web application
│   ├── Areas/Admin/             # Admin area
│   ├── Controllers/             # Customer controllers
│   ├── Data/                    # DbContext
│   ├── Models/Generated/        # EF scaffolded models
│   ├── Services/                # Business services
│   ├── ViewComponents/          # Reusable view components
│   ├── ViewModels/              # DTOs for views
│   └── Views/                   # Razor views
└── FashionHub.Tests/            # xUnit integration tests
```

## Controllers (đã xác minh)
**Customer (6)**: Account, Cart, Chat, Home, Order, Products
**Admin (7)**: Categories, Coupons, Dashboard, Orders, Products, Reports, Users

## Trạng thái hiện tại
- **Build**: ✅ SUCCESS (24 warnings - non-critical)
  - 12 CA1416: ImageFeatureService Windows-only APIs (intentional, not used)
  - 11 CS8602/CS0168/CS8629: Nullable warnings
- **Tests**: 29/32 PASS, 3 FAIL
- **Git**: 37 commits, tag v1.0.0
- **Docker**: ✅ docker-compose.yml ready

## Các quyết định kiến trúc KHÔNG được thay đổi
1. **Database-first**: Scaffold từ SQL Server có sẵn
2. **Cookie Authentication**: Đúng cho MVC, không đổi sang JWT
3. **BCrypt password hashing**: Giữ nguyên từ bản gốc
4. **Gemini API**: Công nghệ gốc từ project cũ
5. **ImageFeatureService disabled**: Windows-only, chờ refactor sang ImageSharp
6. **No ManageOrderController**: Chức năng đã gộp vào AccountController

## Bảo mật
- ✅ Không hardcode secrets - dùng User Secrets (dev) / Environment Variables (prod)
- ✅ Gemini API key đã được bảo mật
- ⚠️ CẦN XÁC NHẬN: API key cũ đã rotate và git history đã sạch

## Môi trường phát triển
- **OS**: Windows
- **Shell**: PowerShell 5.1 (dùng `;` để nối lệnh, KHÔNG dùng `&&`)
- **IDE**: VS Code
- **Model**: deepseek-v4-pro (complex) / deepseek-v4-flash (mechanical)
