# Coding Standards

## Language & Framework
- **C#**: Use latest C# features supported by .NET 10
- **Nullable Reference Types**: Enabled - handle nullability properly
- **Framework**: ASP.NET Core MVC best practices

## Naming Conventions

### C# Code
```csharp
// Classes, Interfaces, Methods: PascalCase
public class ProductController { }
public interface IChatAiService { }
public async Task<IActionResult> GetProduct(int id) { }

// Local variables, parameters: camelCase
int productId = 1;
string userName = "test";

// Private fields: _camelCase (with underscore)
private readonly ApplicationDbContext _context;
private readonly ILogger<HomeController> _logger;

// Constants: PascalCase
public const int MaxPageSize = 100;
```

### Database (Vietnamese naming)
- Tables: PascalCase Vietnamese (SanPham, DonHang, NguoiDung)
- Columns: PascalCase with ID prefix (IDSanPham, TenSanPham, TrangThai)
- Foreign Keys: ID + NavigationPropertyName (IDNguoiDung, IDDonHang)

### Files & Folders
- Controllers: `{Entity}Controller.cs` (ProductsController.cs)
- Views: Match action name (Index.cshtml, Details.cshtml)
- Partials: Prefix with underscore (_Layout.cshtml, _HeaderPartial.cshtml)
- ViewModels: Suffix with purpose (ProductDetailsViewModel.cs)

## Code Organization

### Controller Structure
```csharp
[Area("Admin")] // If in Admin area
[Authorize(Roles = "Admin")] // Authorization at class level when applicable
public class ProductsController : Controller
{
    // 1. Fields (private, readonly)
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductsController> _logger;

    // 2. Constructor (dependency injection)
    public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // 3. GET actions first
    [HttpGet]
    public async Task<IActionResult> Index() { }

    // 4. POST actions after corresponding GET
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductViewModel model) { }

    // 5. Helper methods at the end (private)
    private bool ProductExists(int id) { }
}
```

### Service Structure
```csharp
public class ChatAiService : IChatAiService
{
    // 1. Fields
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    
    // 2. Constructor
    public ChatAiService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // 3. Public methods (interface implementation)
    public async Task<string> GetResponseAsync(string message, int? userId = null)
    {
        // Implementation
    }

    // 4. Private helper methods
    private async Task<string> CallGeminiApiAsync(string prompt)
    {
        // Implementation
    }
}
```

## Entity Framework Patterns

### Query Patterns
```csharp
// ✅ GOOD: Include navigation properties when needed
var product = await _context.SanPhams
    .Include(s => s.IddanhMucNavigation)
    .Include(s => s.BienTheSanPhams)
        .ThenInclude(b => b.IdmauSacNavigation)
    .FirstOrDefaultAsync(s => s.IdsanPham == id);

// ✅ GOOD: AsNoTracking for read-only queries
var products = await _context.SanPhams
    .AsNoTracking()
    .Where(s => s.TrangThai == true)
    .ToListAsync();

// ❌ BAD: N+1 query problem
var products = await _context.SanPhams.ToListAsync();
foreach (var p in products)
{
    var category = await _context.DanhMucs.FindAsync(p.IddanhMuc); // Separate query!
}

// ✅ GOOD: Projection when you don't need full entity
var productNames = await _context.SanPhams
    .Where(s => s.TrangThai == true)
    .Select(s => new { s.IdsanPham, s.TenSanPham })
    .ToListAsync();
```

### Transaction Pattern
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // Multiple operations
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## Async/Await Best Practices

```csharp
// ✅ GOOD: Async all the way
public async Task<IActionResult> Index()
{
    var products = await _context.SanPhams.ToListAsync();
    return View(products);
}

// ❌ BAD: Mixing sync and async
public IActionResult Index()
{
    var products = _context.SanPhams.ToListAsync().Result; // BLOCKS!
    return View(products);
}

// ✅ GOOD: ConfigureAwait(false) in library code
private async Task<string> CallApiAsync()
{
    var response = await httpClient.GetAsync(url).ConfigureAwait(false);
    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
}
```

## Error Handling

### Controller Exception Handling
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(ProductViewModel model)
{
    if (!ModelState.IsValid)
        return View(model);

    try
    {
        // Business logic
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "Error creating product");
        ModelState.AddModelError("", "Không thể tạo sản phẩm. Vui lòng thử lại.");
        return View(model);
    }
}
```

### Service Exception Handling
```csharp
public async Task<string> GetResponseAsync(string message, int? userId = null)
{
    try
    {
        // API call
        return await CallGeminiApiAsync(message);
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Gemini API call failed");
        return "Xin lỗi, hệ thống AI tạm thời không khả dụng.";
    }
}
```

## Validation

### Model Validation
```csharp
public class ProductViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự.")]
    public string TenSanPham { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0.")]
    public decimal GiaBan { get; set; }
}
```

### Controller Validation
```csharp
if (!ModelState.IsValid)
{
    return View(model);
}

// Additional business validation
if (await _context.SanPhams.AnyAsync(s => s.TenSanPham == model.TenSanPham))
{
    ModelState.AddModelError(nameof(model.TenSanPham), "Tên sản phẩm đã tồn tại.");
    return View(model);
}
```

## Security Best Practices

### NEVER Hardcode Secrets
```csharp
// ❌ BAD
string apiKey = "AIzaSyABC123...";

// ✅ GOOD
string apiKey = _configuration["GeminiAI:ApiKey"];
if (string.IsNullOrEmpty(apiKey))
    throw new InvalidOperationException("Gemini API key not configured");
```

### SQL Injection Prevention
```csharp
// ✅ GOOD: EF Core parameterized queries (automatic)
var products = await _context.SanPhams
    .Where(s => s.TenSanPham.Contains(searchTerm))
    .ToListAsync();

// ❌ BAD: Raw SQL with string concatenation
var products = await _context.SanPhams
    .FromSqlRaw($"SELECT * FROM SanPham WHERE TenSanPham LIKE '%{searchTerm}%'")
    .ToListAsync();

// ✅ GOOD: Raw SQL with parameters (if needed)
var products = await _context.SanPhams
    .FromSqlRaw("SELECT * FROM SanPham WHERE TenSanPham LIKE {0}", $"%{searchTerm}%")
    .ToListAsync();
```

### XSS Prevention
```cshtml
<!-- ✅ GOOD: Razor auto-encodes -->
<p>@Model.UserInput</p>

<!-- ⚠️ DANGEROUS: Raw HTML (only for trusted content) -->
<p>@Html.Raw(Model.TrustedHtmlContent)</p>
```

### CSRF Protection
```cshtml
<!-- ✅ GOOD: Antiforgery token in forms -->
<form method="post" asp-action="Create">
    @Html.AntiForgeryToken()
    <!-- form fields -->
</form>
```

```csharp
// ✅ GOOD: Validate token in POST actions
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(ProductViewModel model) { }
```

## Comments & Documentation

### XML Documentation
```csharp
/// <summary>
/// Retrieves AI chatbot response for user message
/// </summary>
/// <param name="userMessage">User's input message</param>
/// <param name="userId">Optional user ID for personalization</param>
/// <returns>AI-generated response text</returns>
public async Task<string> GetResponseAsync(string userMessage, int? userId = null)
{
    // Implementation
}
```

### Inline Comments
```csharp
// ✅ GOOD: Explain WHY, not WHAT
// Hash password with BCrypt to maintain compatibility with old system
string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

// ❌ BAD: Obvious comment
// Create a new product
var product = new SanPham();

// ✅ GOOD: Complex business logic explanation
// Apply discount only if:
// 1. Coupon is valid (within date range)
// 2. Order total meets minimum requirement
// 3. User hasn't exceeded usage limit
if (IsValidCoupon(coupon, orderTotal, userId))
{
    // Apply discount
}
```

### TODO Comments
```csharp
// TODO: Refactor to use service layer (tracked in issue #123)
// HACK: Temporary workaround until ImageSharp migration
// FIXME: This breaks when product has no variants
```

## Razor View Best Practices

### View Structure
```cshtml
@model ProductDetailsViewModel
@{
    ViewData["Title"] = Model.TenSanPham;
}

<!-- Content -->
<div class="product-details">
    <h1>@Model.TenSanPham</h1>
    <!-- More HTML -->
</div>

@section Scripts {
    <script src="~/js/product-details.js"></script>
}
```

### Partial Views
```cshtml
<!-- Strongly-typed partial -->
@await Html.PartialAsync("_ProductCardPartial", product)

<!-- ViewComponent -->
@await Component.InvokeAsync("CartIcon")
```

### Form Tag Helpers
```cshtml
<!-- ✅ GOOD: Use tag helpers -->
<form asp-controller="Products" asp-action="Create" method="post">
    <input asp-for="TenSanPham" class="form-control" />
    <span asp-validation-for="TenSanPham" class="text-danger"></span>
    <button type="submit">Tạo sản phẩm</button>
</form>

<!-- ❌ BAD: Manual URLs -->
<form action="/Products/Create" method="post">
    <input name="TenSanPham" class="form-control" />
    <button type="submit">Tạo sản phẩm</button>
</form>
```

## Performance Considerations

### Avoid N+1 Queries
```csharp
// ✅ GOOD: Single query with Include
var orders = await _context.DonHangs
    .Include(d => d.ChiTietDonHangs)
    .ToListAsync();

// ❌ BAD: N+1 queries
var orders = await _context.DonHangs.ToListAsync();
foreach (var order in orders)
{
    var details = await _context.ChiTietDonHangs
        .Where(c => c.IddonHang == order.IddonHang)
        .ToListAsync();
}
```

### Use Pagination
```csharp
public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
{
    var products = await _context.SanPhams
        .OrderBy(s => s.TenSanPham)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    return View(products);
}
```

## Testing Conventions

### Test Naming
```csharp
// Pattern: MethodName_Scenario_ExpectedBehavior
[Fact]
public async Task AddToCart_ValidProduct_ReturnsSuccessJson() { }

[Fact]
public async Task Checkout_EmptyCart_ReturnsBadRequest() { }
```

### Test Structure (AAA Pattern)
```csharp
[Fact]
public async Task Register_ValidData_CreatesUserAndRedirects()
{
    // Arrange
    var client = _factory.CreateClient();
    var formData = new Dictionary<string, string>
    {
        { "Email", "test@example.com" },
        { "Password", "Test123!" }
    };

    // Act
    var response = await client.PostAsync("/Account/Register", 
        new FormUrlEncodedContent(formData));

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Redirect);
}
```

## Git Commit Message Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types
- **feat**: New feature
- **fix**: Bug fix
- **refactor**: Code refactoring
- **test**: Add or update tests
- **docs**: Documentation changes
- **style**: Code style changes (formatting)
- **perf**: Performance improvements
- **chore**: Maintenance tasks

### Examples
```
feat(cart): add coupon code validation

Implement server-side coupon validation with:
- Date range check
- Minimum order value check
- Usage limit per user

Closes #42

---

fix(products): resolve null reference in Details action

Add null check before accessing navigation properties
in ProductsController.Details()

---

refactor(services): extract pricing logic to PricingService

Move price calculation from OrderController to dedicated
PricingService for better testability and reuse
```
