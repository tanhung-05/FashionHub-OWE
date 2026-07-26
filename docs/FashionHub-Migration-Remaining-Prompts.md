# FashionHub Migration — Remaining Prompts

**Ngày tạo:** 2026-07-26  
**Tiến độ hiện tại:** 80% (16/20 prompts done)  
**Còn lại:** 4 prompts + 1 urgent fix

---

## 🚨 URGENT FIX: Hardcoded API Key (Priority P0)

**Phải fix TRƯỚC KHI làm bất kỳ prompt nào khác!**

### Prompt: Fix Security Regression — Hardcoded API Key

```
Fix hardcoded Gemini API key trong ChatAiService — security regression nghiêm trọng:

1. Đọc file FashionHub2/FashionHub.Web/Services/ChatAiService.cs

2. Tại line 140, tìm và thay thế:
   ```csharp
   var apiKey = _configuration["GeminiAI:ApiKey"] ?? "AIzaSyCwSDhu2KY92SEAnHvs1RgZnXAKsAVnHrE";
   ```
   
   Thay bằng:
   ```csharp
   var apiKey = _configuration["GeminiAI:ApiKey"];
   if (string.IsNullOrEmpty(apiKey))
   {
       throw new InvalidOperationException(
           "Gemini API key is not configured. Please set 'GeminiAI:ApiKey' in User Secrets or appsettings.");
   }
   ```

3. Verify User Secrets có API key:
   - Check file có tồn tại không: `%APPDATA%\Microsoft\UserSecrets\<user-secrets-id>\secrets.json`
   - Nếu chưa có, hướng dẫn user cấu hình: `dotnet user-secrets set "GeminiAI:ApiKey" "YOUR_ACTUAL_KEY"`

4. Build để verify không có compile error

5. Test chat feature với actual API key từ User Secrets

6. Commit với message: `fix: remove hardcoded Gemini API key (security regression)`

QUAN TRỌNG: Đây là security vulnerability — API key đang exposed trong source code và git history. 
Fix này phải được merge trước khi deploy.
```

---

## Prompt 16: CSS/JS Migration & UI Polish (Comprehensive Review)

**Status:** Partial (50%) — CSS/JS đã copy, cần comprehensive review  
**Estimated effort:** 1 ngày  
**Dependencies:** None

### Prompt 16: Complete UI/UX Polish & Comprehensive Review

```
Hoàn thiện UI/UX polish cho FashionHub2/FashionHub.Web — comprehensive review toàn bộ CSS/JS:

HIỆN TRẠNG:
- CSS đã copy từ FashionHub/Content/site.css sang FashionHub2/FashionHub.Web/wwwroot/css/site.css
- JS đã copy từ FashionHub/Scripts/site.js sang FashionHub2/FashionHub.Web/wwwroot/js/site.js
- Bootstrap 5.3 đã được thêm vào _Layout
- Có SQL script để fix image paths

CẦN LÀM:

## 1. Verify Design Tokens
Kiểm tra tất cả CSS custom properties trong site.css có đang được dùng đúng không:
- `--owe-black`, `--owe-ink`, `--owe-muted`, `--owe-soft`, `--owe-surface`, `--owe-border`, `--owe-sale`
- `--owe-radius-sm`, `--owe-radius-md`, `--owe-radius-lg`
- `--owe-shadow-sm`, `--owe-shadow-md`

Tìm và thay thế mọi hardcoded color/size không dùng token.

## 2. Responsive Testing Checklist
Test và fix responsive trên các breakpoints:
- Mobile (< 576px): Product grid, cart, filter, footer
- Tablet (576px - 992px): Navigation, product cards
- Desktop (> 992px): Full layout

Đảm bảo:
- Product grid responsive (1-2-3-4 columns tùy breakpoint)
- Cart offcanvas hoạt động tốt trên mobile
- Filter có offcanvas/collapse trên màn hình nhỏ
- Forms không bị vỡ layout
- Tables responsive hoặc có scroll horizontal

## 3. Accessibility (WCAG 2.1 Level AA)
- Tất cả buttons chỉ có icon phải có `aria-label`
- Images phải có `alt` descriptive
- Form controls cần label rõ ràng
- Error messages phải accessible
- Color contrast ratio >= 4.5:1 cho text
- Focus indicators rõ ràng cho keyboard navigation

## 4. JavaScript Interactions
Verify tất cả JS interactions hoạt động:
- Toast notifications (success/error/info/warning)
- Cart add/update/remove AJAX
- Product quick view modal
- Chat widget toggle
- Address modal (add new address)
- Coupon apply
- Product variant selection với image change

## 5. Admin Panel UI
Kiểm tra Admin area UI:
- Dashboard cards & charts
- Tables pagination & sorting
- Forms validation feedback
- Modal confirmations
- Image upload UI
- Bulk actions

## 6. Performance
- Minify CSS/JS trong production (add to Program.cs nếu chưa có)
- Lazy load images nếu có nhiều products
- Optimize font loading

## 7. Cross-browser Testing
Test trên:
- Chrome/Edge (Chromium)
- Firefox
- Safari (nếu có Mac)

Tạo checklist trong docs/ui-comprehensive-review-checklist.md với status từng item.

Fix tất cả issues tìm thấy, commit theo từng nhóm:
- `style: fix responsive layout for mobile/tablet`
- `a11y: add aria-labels and improve accessibility`
- `perf: optimize CSS/JS loading`
- `fix: correct JavaScript interactions`

Kết thúc bằng commit: `chore: complete UI/UX comprehensive review (Prompt 16)`
```

---

## Prompt 17: User Profile & Order History

**Status:** Not Started (0%)  
**Estimated effort:** 1-2 ngày  
**Dependencies:** Prompt 16 (CSS/JS)

### Prompt 17A: User Profile Management

```
Migrate User Profile management sang FashionHub2/FashionHub.Web:

## 1. ViewModels
Tạo FashionHub2/FashionHub.Web/ViewModels/Account/ProfileViewModel.cs:
```csharp
public class ProfileViewModel
{
    [Required]
    [Display(Name = "Họ tên")]
    public string HoTen { get; set; }

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Phone]
    [Display(Name = "Số điện thoại")]
    public string? SoDienThoai { get; set; }

    public string? Avatar { get; set; }
}

public class ChangePasswordViewModel
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Compare("NewPassword")]
    [Display(Name = "Xác nhận mật khẩu mới")]
    public string ConfirmPassword { get; set; }
}
```

Tạo ViewModels/Account/AddressManagementViewModel.cs cho quản lý địa chỉ.

## 2. Controller Actions
Thêm vào FashionHub2/FashionHub.Web/Controllers/AccountController.cs:

```csharp
[Authorize]
public async Task<IActionResult> Profile()
{
    var userId = GetCurrentUserId();
    var user = await _context.NguoiDungs.FindAsync(userId);
    
    var model = new ProfileViewModel
    {
        HoTen = user.HoTen,
        Email = user.Email,
        SoDienThoai = user.SoDienThoai,
        Avatar = user.Avatar
    };
    
    return View(model);
}

[HttpPost]
[Authorize]
public async Task<IActionResult> Profile(ProfileViewModel model)
{
    if (!ModelState.IsValid)
        return View(model);
    
    var userId = GetCurrentUserId();
    var user = await _context.NguoiDungs.FindAsync(userId);
    
    user.HoTen = model.HoTen;
    user.Email = model.Email;
    user.SoDienThoai = model.SoDienThoai;
    
    await _context.SaveChangesAsync();
    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
    
    return RedirectToAction(nameof(Profile));
}

[Authorize]
public IActionResult ChangePassword()
{
    return View();
}

[HttpPost]
[Authorize]
public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
{
    // Implement password change logic with verification
}

[Authorize]
public async Task<IActionResult> Addresses()
{
    var userId = GetCurrentUserId();
    var addresses = await _context.DiaChis
        .Where(d => d.IdNguoiDung == userId)
        .ToListAsync();
    
    return View(addresses);
}

// CRUD actions for addresses: CreateAddress, EditAddress, DeleteAddress, SetDefaultAddress
```

## 3. Views
Tạo FashionHub2/FashionHub.Web/Views/Account/Profile.cshtml:
- Form hiển thị thông tin user
- Avatar upload (optional)
- Button để change password
- Link đến quản lý addresses

Tạo Views/Account/ChangePassword.cshtml:
- Form đổi mật khẩu với validation

Tạo Views/Account/Addresses.cshtml:
- List địa chỉ
- Mark default address
- CRUD operations

## 4. Update _MenuPartial
Thêm link "Tài khoản" trong menu dropdown khi user đã login, link đến Profile.

Build, test, commit: `feat: add user profile management (Prompt 17A)`
```

### Prompt 17B: Order History for Customers

```
Migrate Order History cho customer sang FashionHub2/FashionHub.Web:

## 1. ViewModels
Tạo FashionHub2/FashionHub.Web/ViewModels/Account/OrderHistoryViewModel.cs:
```csharp
public class OrderHistoryViewModel
{
    public int IdDonHang { get; set; }
    public DateTime NgayDatHang { get; set; }
    public decimal TongTien { get; set; }
    public string TrangThai { get; set; }
    public string MauTrangThai { get; set; } // badge color
    public int SoLuongSanPham { get; set; }
}

public class OrderDetailViewModel
{
    public DonHang Order { get; set; }
    public List<ChiTietDonHang> OrderDetails { get; set; }
    public DiaChi ShippingAddress { get; set; }
    public string PaymentMethod { get; set; }
    public List<OrderStatusHistory> StatusHistory { get; set; }
}
```

## 2. Controller Actions
Thêm vào AccountController:

```csharp
[Authorize]
public async Task<IActionResult> OrderHistory(int page = 1, int? statusFilter = null)
{
    var userId = GetCurrentUserId();
    var pageSize = 10;
    
    var query = _context.DonHangs
        .Where(d => d.IdNguoiDung == userId)
        .Include(d => d.IdTrangThaiNavigation)
        .OrderByDescending(d => d.NgayDatHang);
    
    if (statusFilter.HasValue)
        query = query.Where(d => d.IdTrangThai == statusFilter.Value);
    
    var totalOrders = await query.CountAsync();
    var orders = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(d => new OrderHistoryViewModel
        {
            IdDonHang = d.IdDonHang,
            NgayDatHang = d.NgayDatHang,
            TongTien = d.TongTien,
            TrangThai = d.IdTrangThaiNavigation.TenTrangThai,
            MauTrangThai = GetStatusBadgeColor(d.IdTrangThai),
            SoLuongSanPham = d.ChiTietDonHangs.Sum(ct => ct.SoLuong)
        })
        .ToListAsync();
    
    ViewBag.CurrentPage = page;
    ViewBag.TotalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);
    ViewBag.StatusFilter = statusFilter;
    
    return View(orders);
}

[Authorize]
public async Task<IActionResult> OrderDetail(int id)
{
    var userId = GetCurrentUserId();
    
    var order = await _context.DonHangs
        .Include(d => d.ChiTietDonHangs)
            .ThenInclude(ct => ct.IdBienTheNavigation)
                .ThenInclude(bt => bt.IdSanPhamNavigation)
        .Include(d => d.IdDiaChiNavigation)
        .Include(d => d.IdPhuongThucThanhToanNavigation)
        .Include(d => d.IdTrangThaiNavigation)
        .FirstOrDefaultAsync(d => d.IdDonHang == id && d.IdNguoiDung == userId);
    
    if (order == null)
        return NotFound();
    
    var model = new OrderDetailViewModel
    {
        Order = order,
        OrderDetails = order.ChiTietDonHangs.ToList(),
        ShippingAddress = order.IdDiaChiNavigation,
        PaymentMethod = order.IdPhuongThucThanhToanNavigation.TenPhuongThuc
    };
    
    return View(model);
}

[HttpPost]
[Authorize]
public async Task<IActionResult> CancelOrder(int id, string reason)
{
    var userId = GetCurrentUserId();
    var order = await _context.DonHangs
        .FirstOrDefaultAsync(d => d.IdDonHang == id && d.IdNguoiDung == userId);
    
    if (order == null)
        return NotFound();
    
    // Only allow cancel if order is pending (IdTrangThai = 1)
    if (order.IdTrangThai != 1)
        return BadRequest("Không thể hủy đơn hàng ở trạng thái hiện tại");
    
    order.IdTrangThai = 5; // Cancelled
    order.GhiChu = $"Hủy bởi khách hàng. Lý do: {reason}";
    await _context.SaveChangesAsync();
    
    return Json(new { success = true });
}

private string GetStatusBadgeColor(int statusId)
{
    return statusId switch
    {
        1 => "warning",  // Pending
        2 => "info",     // Confirmed
        3 => "primary",  // Shipping
        4 => "success",  // Delivered
        5 => "danger",   // Cancelled
        _ => "secondary"
    };
}
```

## 3. Views
Tạo Views/Account/OrderHistory.cshtml:
- Table/cards hiển thị orders với pagination
- Filter theo trạng thái
- Link đến detail page
- Responsive cho mobile

Tạo Views/Account/OrderDetail.cshtml:
- Thông tin đơn hàng đầy đủ
- Sản phẩm trong đơn
- Địa chỉ giao hàng
- Status history/timeline
- Button "Hủy đơn hàng" nếu status = Pending
- Button "Mua lại" để add tất cả items vào cart

## 4. Update Navigation
Thêm link "Đơn hàng của tôi" vào menu user dropdown.

## 5. Styling
Dùng design tokens đã có, đảm bảo UI nhất quán với phần còn lại.

Build, test trên nhiều trạng thái order, commit: `feat: add order history for customers (Prompt 17B)`

Tổng kết Prompt 17: `docs: update progress after completing Prompt 17 (User Profile & Order History)`
```

---

## Prompt 18: Integration Testing

**Status:** Not Started (0%)  
**Estimated effort:** 2-3 ngày  
**Dependencies:** Prompts 16, 17

### Prompt 18: Add Integration Tests with xUnit

```
Setup integration testing cho FashionHub2/FashionHub.Web với xUnit:

## 1. Test Project Setup
```bash
cd FashionHub2
dotnet new xunit -n FashionHub.Tests
cd FashionHub.Tests
dotnet add reference ../FashionHub.Web/FashionHub.Web.csproj
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package FluentAssertions
```

Thêm project vào solution:
```bash
cd ..
dotnet sln add FashionHub.Tests/FashionHub.Tests.csproj
```

## 2. WebApplicationFactory Setup
Tạo FashionHub.Tests/CustomWebApplicationFactory.cs:
```csharp
public class CustomWebApplicationFactory<TProgram> 
    : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove real DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);
            
            // Add InMemory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });
            
            // Seed test data
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedTestData(db);
        });
    }
    
    private void SeedTestData(ApplicationDbContext db)
    {
        // Add test categories, products, users, etc.
    }
}
```

## 3. Controller Tests
Tạo tests cho từng controller area:

### ProductsControllerTests.cs
```csharp
public class ProductsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public ProductsControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task Index_ReturnsSuccessAndCorrectContentType()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/Products");
        
        // Assert
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType.ToString()
            .Should().Contain("text/html");
    }
    
    [Fact]
    public async Task Index_WithSearchFilter_ReturnsFilteredProducts()
    {
        // Test search, filter, pagination
    }
    
    [Fact]
    public async Task Details_WithValidId_ReturnsProductDetails()
    {
        // Test product details page
    }
    
    [Fact]
    public async Task Details_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/Products/Details/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

### CartControllerTests.cs
```csharp
public class CartControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    [Fact]
    public async Task AddToCart_WithValidVariant_AddsItemToCart()
    {
        // Test add to cart flow
    }
    
    [Fact]
    public async Task UpdateCart_WithValidQuantity_UpdatesCartItem()
    {
        // Test update quantity
    }
    
    [Fact]
    public async Task RemoveFromCart_RemovesItemSuccessfully()
    {
        // Test remove item
    }
}
```

### OrderControllerTests.cs
```csharp
public class OrderControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    [Fact]
    public async Task Checkout_WithoutAuth_RedirectsToLogin()
    {
        // Test authorization
    }
    
    [Fact]
    public async Task PlaceOrder_WithValidData_CreatesOrder()
    {
        // Test order placement
    }
    
    [Fact]
    public async Task ApplyCoupon_WithValidCode_AppliesDiscount()
    {
        // Test coupon application
    }
}
```

### AccountControllerTests.cs
```csharp
public class AccountControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    [Fact]
    public async Task Login_WithValidCredentials_RedirectsToHome()
    {
        // Test login
    }
    
    [Fact]
    public async Task Register_WithValidData_CreatesUser()
    {
        // Test registration
    }
    
    [Fact]
    public async Task Profile_WithoutAuth_RedirectsToLogin()
    {
        // Test authorization
    }
}
```

## 4. Admin Controllers Tests
Tạo tests cho Admin area:

### Admin/OrdersControllerTests.cs
### Admin/ProductsControllerTests.cs
### Admin/DashboardControllerTests.cs
Etc.

## 5. Integration Tests cho Main Flows
Tạo FashionHub.Tests/IntegrationTests/CheckoutFlowTests.cs:
```csharp
public class CheckoutFlowTests
{
    [Fact]
    public async Task CompleteCheckoutFlow_FromBrowseToOrderSuccess()
    {
        // 1. Browse products
        // 2. Add to cart
        // 3. Login/Register
        // 4. Checkout with address
        // 5. Apply coupon
        // 6. Place order
        // 7. Verify order created
        // 8. Verify order success page
    }
}
```

## 6. Test Coverage
Mục tiêu coverage:
- Controllers: >= 80%
- Services: >= 90%
- Critical paths: 100%

Chạy tests:
```bash
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

## 7. CI/CD Integration
Tạo .github/workflows/tests.yml (nếu dùng GitHub):
```yaml
name: Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - run: dotnet test --configuration Release
```

Commit: `test: add integration tests with xUnit (Prompt 18)`
```

---

## Prompt 19: Dockerize Application

**Status:** Not Started (0%)  
**Estimated effort:** 1 ngày  
**Dependencies:** Prompts 16, 17, 18

### Prompt 19: Docker & Docker Compose Setup

```
Dockerize FashionHub2/FashionHub.Web với multi-stage build:

## 1. Dockerfile
Tạo FashionHub2/FashionHub.Web/Dockerfile:
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["FashionHub.Web/FashionHub.Web.csproj", "FashionHub.Web/"]
RUN dotnet restore "FashionHub.Web/FashionHub.Web.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/FashionHub.Web"
RUN dotnet build "FashionHub.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "FashionHub.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FashionHub.Web.dll"]
```

## 2. .dockerignore
Tạo FashionHub2/.dockerignore:
```
**/.dockerignore
**/.env
**/.git
**/.gitignore
**/.vs
**/.vscode
**/bin
**/obj
**/*.trx
**/*.md
LICENSE
README.md
**/node_modules
```

## 3. Docker Compose
Tạo FashionHub2/docker-compose.yml:
```yaml
version: '3.8'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: fashionhub-sqlserver
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Passw0rd
      - MSSQL_PID=Developer
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
    networks:
      - fashionhub-network
    healthcheck:
      test: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -Q "SELECT 1" || exit 1
      interval: 10s
      timeout: 3s
      retries: 10
      start_period: 10s

  web:
    build:
      context: .
      dockerfile: FashionHub.Web/Dockerfile
    container_name: fashionhub-web
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=FashionHub;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;
      - GeminiAI__ApiKey=${GEMINI_API_KEY}
    ports:
      - "5167:8080"
    depends_on:
      sqlserver:
        condition: service_healthy
    networks:
      - fashionhub-network
    restart: unless-stopped

volumes:
  sqlserver_data:

networks:
  fashionhub-network:
    driver: bridge
```

## 4. Environment Variables
Tạo FashionHub2/.env.example:
```env
# SQL Server
SA_PASSWORD=YourStrong@Passw0rd

# Gemini AI
GEMINI_API_KEY=your-gemini-api-key-here

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
```

Tạo .env thật (không commit):
```bash
cp .env.example .env
# Edit .env với actual values
```

## 5. Database Initialization
Tạo FashionHub2/init-db.sh:
```bash
#!/bin/bash
set -e

echo "Waiting for SQL Server to be ready..."
sleep 30s

echo "Running database migrations..."
dotnet ef database update --project FashionHub.Web

echo "Database initialized successfully!"
```

Hoặc tạo SQL script FashionHub2/init-db.sql để seed initial data.

## 6. Production Configuration
Cập nhật FashionHub.Web/appsettings.Production.json:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver;Database=FashionHub;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"
  }
}
```

## 7. Build và Run
```bash
# Build images
docker-compose build

# Start services
docker-compose up -d

# View logs
docker-compose logs -f web

# Stop services
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

## 8. Health Checks
Thêm health check endpoint vào Program.cs:
```csharp
app.MapHealthChecks("/health");
```

Add package:
```bash
dotnet add package Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore
```

## 9. Documentation
Tạo docs/docker-deployment.md với hướng dẫn:
- Prerequisites
- Build instructions
- Environment variables
- Troubleshooting
- Production considerations

## 10. Security
- Không commit .env vào git
- Dùng secrets management cho production (Azure Key Vault, AWS Secrets Manager, etc.)
- Scan images với `docker scan`

Commit: `feat: add Docker and Docker Compose setup (Prompt 19)`
```

---

## Prompt 20: Final Review & Cleanup

**Status:** Not Started (0%)  
**Estimated effort:** 1-2 ngày  
**Dependencies:** All previous prompts

### Prompt 20: Production Readiness — Final Review

```
Final review và cleanup trước khi production deploy:

## 1. Security Audit

### Code Security
- [ ] Verify NO hardcoded secrets/API keys trong code
- [ ] Verify User Secrets được dùng cho development
- [ ] Verify appsettings.Production.json không chứa sensitive data
- [ ] Check SQL injection vulnerabilities (dùng parameterized queries)
- [ ] Check XSS vulnerabilities (proper encoding trong Razor)
- [ ] Check CSRF protection (antiforgery tokens)
- [ ] Verify authentication/authorization rules đúng

### Dependencies Security
```bash
dotnet list package --vulnerable
dotnet list package --outdated
```

Update vulnerable packages nếu có.

### HTTPS & Security Headers
Verify Program.cs có:
- HTTPS redirection
- HSTS
- Security headers (X-Frame-Options, X-Content-Type-Options, etc.)

## 2. Performance Optimization

### Database
- [ ] Add missing indexes trên các foreign keys
- [ ] Review query performance với execution plans
- [ ] Implement caching cho frequently accessed data (IMemoryCache/IDistributedCache)
- [ ] Add database connection pooling configuration

### Response Compression & Static Files
Thêm vào Program.cs:
```csharp
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "text/css", "application/javascript" });
});

// In production
if (app.Environment.IsProduction())
{
    app.UseResponseCompression();
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000");
        }
    });
}
```

### Image Optimization
- Verify images có correct format (WebP for modern browsers)
- Implement lazy loading cho product images
- Consider CDN cho static assets

## 3. Code Cleanup

### Remove Unused Code
```bash
# Find unused using statements
dotnet format analyzers --verify-no-changes

# Remove commented code
# Review TODOs and resolve/document them
```

### Code Quality
- [ ] Run code analysis: `dotnet build /p:EnableNETAnalyzers=true`
- [ ] Fix all warnings
- [ ] Review and resolve all TODO comments
- [ ] Ensure consistent naming conventions
- [ ] Remove debug/console logging statements

### Documentation
- [ ] Update README.md với setup instructions
- [ ] Document environment variables
- [ ] API documentation (nếu có public APIs)
- [ ] Deployment guide
- [ ] Troubleshooting guide

## 4. Configuration Management

### appsettings Review
Verify appsettings.json structure:
- Development: detailed logging, test data
- Production: minimal logging, real connection strings (from environment)

### Environment Variables
Document all required environment variables trong README:
- ConnectionStrings__DefaultConnection
- GeminiAI__ApiKey
- ASPNETCORE_ENVIRONMENT
- Etc.

## 5. Error Handling & Logging

### Global Exception Handler
Verify có proper error handling:
```csharp
if (app.Environment.IsProduction())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");
}
```

### Logging
- [ ] Structured logging setup (Serilog recommended)
- [ ] Log levels configured correctly
- [ ] Sensitive data NOT logged
- [ ] Performance logging cho slow queries

## 6. Database

### Migrations
```bash
# Generate final migration nếu có schema changes
dotnet ef migrations add FinalSchemaUpdate

# Verify migration scripts
dotnet ef migrations script
```

### Data Validation
- [ ] All foreign keys have indexes
- [ ] Constraints in place (NOT NULL, UNIQUE, CHECK)
- [ ] Default values set where appropriate

### Backup Strategy
Document backup/restore procedures trong docs/database-backup.md

## 7. Monitoring & Observability

### Health Checks
Implement comprehensive health checks:
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddCheck("GeminiAI", () => 
    {
        // Check if API key configured
        var apiKey = configuration["GeminiAI:ApiKey"];
        return string.IsNullOrEmpty(apiKey) 
            ? HealthCheckResult.Unhealthy("Gemini API key not configured")
            : HealthCheckResult.Healthy();
    });
```

### Application Insights (Optional)
Consider adding telemetry cho production monitoring.

## 8. Final Testing

### Manual Testing Checklist
Tạo docs/final-testing-checklist.md:
- [ ] User registration/login
- [ ] Browse products với filters
- [ ] Add to cart, update, remove
- [ ] Checkout flow complete
- [ ] Order history view
- [ ] Profile management
- [ ] Admin login
- [ ] Admin CRUD operations
- [ ] Reports generation
- [ ] Chat AI functionality

### Load Testing (Optional)
Consider load testing critical endpoints:
```bash
# Using Apache Bench
ab -n 1000 -c 10 http://localhost:5167/Products

# Using k6
k6 run load-test.js
```

## 9. Deployment Preparation

### Pre-deployment Checklist
- [ ] All tests passing (unit + integration)
- [ ] No hardcoded secrets
- [ ] Environment-specific configs ready
- [ ] Database migration scripts tested
- [ ] Backup/rollback plan documented
- [ ] SSL certificates configured
- [ ] Firewall rules configured
- [ ] Monitoring/alerting setup

### Deployment Documentation
Tạo docs/deployment-guide.md:
- Infrastructure requirements
- Step-by-step deployment
- Post-deployment verification
- Rollback procedures
- Common issues & solutions

## 10. Post-Deployment

### Smoke Tests
Document smoke tests để verify sau deploy:
- Homepage loads
- Login works
- Critical flows functional
- Admin panel accessible

### Monitoring
- Application logs
- Error rates
- Response times
- Database performance

## 11. Final Commits

Commit cleanup changes theo nhóm:
```bash
git commit -m "refactor: remove unused code and fix warnings"
git commit -m "docs: update README and deployment guides"
git commit -m "perf: add response compression and caching"
git commit -m "chore: final cleanup before production (Prompt 20)"
```

## 12. Version Tagging
```bash
git tag -a v1.0.0 -m "FashionHub 2.0 - ASP.NET Core migration complete"
git push origin v1.0.0
```

---

## COMPLETION CHECKLIST

After Prompt 20, verify:
- [ ] ✅ Build succeeds with 0 errors, 0 warnings
- [ ] ✅ All tests pass (unit + integration)
- [ ] ✅ Security audit passed (no hardcoded secrets, SQL injection, XSS)
- [ ] ✅ Performance acceptable (response times < 200ms for most requests)
- [ ] ✅ Docker build works and runs
- [ ] ✅ Documentation complete and up-to-date
- [ ] ✅ Deployment guide tested
- [ ] ✅ Monitoring/logging configured
- [ ] ✅ Backup/restore procedures documented

**MIGRATION COMPLETE! 🎉**
```

---

## Summary

**Prompts còn lại:** 5 (1 urgent fix + 4 planned prompts)

**Total estimated time:** 7-11 ngày

**Critical path:**
1. 🚨 Fix API Key (0.5 ngày) — MUST DO FIRST
2. Prompt 16: UI/UX Polish (1 ngày)
3. Prompt 17: User Profile & Order History (1-2 ngày)
4. Prompt 18: Integration Testing (2-3 ngày)
5. Prompt 19: Dockerize (1 ngày)
6. Prompt 20: Final Review & Cleanup (1-2 ngày)

**Recommended order:**
- API Key Fix → Prompt 16 → Prompt 17 → Prompt 18 → Prompt 19 → Prompt 20

**Or parallel approach:**
- API Key Fix (immediate)
- Prompt 16 + 17 (can work in parallel nếu có 2 devs)
- Prompt 18 (after 16+17 done)
- Prompt 19 (after tests pass)
- Prompt 20 (final review)
