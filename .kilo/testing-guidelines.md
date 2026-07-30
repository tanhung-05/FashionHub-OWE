# Testing Guidelines

## Test Strategy Overview

### Test Pyramid
```
    /\
   /  \     E2E Tests (Manual, Future)
  /----\    
 /      \   Integration Tests (Current Focus)
/--------\  
|        |  Unit Tests (Controllers, Services)
----------
```

### Current Test Coverage
- **Controllers**: Products, Cart, Order, Account, Admin (Dashboard, Products)
- **Integration**: Shopping flow (browse → cart → checkout → order)
- **Framework**: xUnit with FluentAssertions

## Test Infrastructure

### CustomWebApplicationFactory
```csharp
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> 
    where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove SQL Server
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Add in-memory database
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

        builder.UseEnvironment("Test");
    }
}
```

### Test Data Seeding
```csharp
private static void SeedTestData(ApplicationDbContext db)
{
    if (db.SanPhams.Any()) return;

    // Seed categories
    var category = new DanhMuc { IddanhMuc = 1, TenDanhMuc = "Test Category" };
    db.DanhMucs.Add(category);

    // Seed products
    var product = new SanPham 
    { 
        IdsanPham = 1, 
        TenSanPham = "Test Product",
        IddanhMuc = 1,
        GiaBan = 100000,
        TrangThai = true
    };
    db.SanPhams.Add(product);

    // Seed variants
    var variant = new BienTheSanPham
    {
        IdbienThe = 1,
        IdsanPham = 1,
        SoLuong = 100,
        GiaBan = 100000
    };
    db.BienTheSanPhams.Add(variant);

    db.SaveChanges();
}
```

## Test Naming Convention

### Pattern
```
MethodName_Scenario_ExpectedBehavior
```

### Examples
```csharp
[Fact]
public async Task Index_WithProducts_ReturnsViewWithProducts() { }

[Fact]
public async Task AddToCart_ValidProduct_ReturnsSuccessJson() { }

[Fact]
public async Task Checkout_EmptyCart_ReturnsBadRequest() { }

[Fact]
public async Task Login_InvalidCredentials_ReturnsViewWithError() { }

[Fact]
public async Task Dashboard_WithoutAuth_RedirectsToLogin() { }
```

## Test Structure (AAA Pattern)

### Arrange - Act - Assert
```csharp
[Fact]
public async Task AddToCart_ValidProduct_AddsItemToSession()
{
    // Arrange
    var client = _factory.CreateClient();
    var formData = new Dictionary<string, string>
    {
        { "variantId", "1" },
        { "quantity", "2" }
    };

    // Act
    var response = await client.PostAsync("/Cart/AddToCart", 
        new FormUrlEncodedContent(formData));

    // Assert
    response.EnsureSuccessStatusCode();
    var json = await response.Content.ReadAsStringAsync();
    json.Should().Contain("\"success\":true");
}
```

## Controller Tests

### Testing GET Actions
```csharp
[Fact]
public async Task Index_ReturnsViewWithProducts()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/Products");

    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    content.Should().Contain("Test Product");
}
```

### Testing POST Actions
```csharp
[Fact]
public async Task Create_ValidData_RedirectsToIndex()
{
    // Arrange
    var client = _factory.CreateClient();
    var formData = new Dictionary<string, string>
    {
        { "TenSanPham", "New Product" },
        { "GiaBan", "200000" },
        { "IddanhMuc", "1" }
    };

    // Act
    var response = await client.PostAsync("/Admin/Products/Create", 
        new FormUrlEncodedContent(formData));

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    response.Headers.Location.ToString().Should().Contain("/Admin/Products");
}
```

### Testing Authorization
```csharp
[Fact]
public async Task Dashboard_WithoutAuth_RedirectsToLogin()
{
    // Arrange
    var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    // Act
    var response = await client.GetAsync("/Admin/Dashboard");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    response.Headers.Location.ToString().Should().Contain("/Account/Login");
}
```

### Testing Validation
```csharp
[Fact]
public async Task Create_InvalidData_ReturnsValidationErrors()
{
    // Arrange
    var client = _factory.CreateClient();
    var formData = new Dictionary<string, string>
    {
        { "TenSanPham", "" }, // Empty - should fail validation
        { "GiaBan", "-100" }  // Negative - should fail validation
    };

    // Act
    var response = await client.PostAsync("/Admin/Products/Create", 
        new FormUrlEncodedContent(formData));

    // Assert
    var content = await response.Content.ReadAsStringAsync();
    content.Should().Contain("validation");
}
```

## Integration Tests

### End-to-End Shopping Flow
```csharp
[Fact]
public async Task CompleteShoppingFlow_Success()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act & Assert - Browse products
    var productsResponse = await client.GetAsync("/Products");
    productsResponse.EnsureSuccessStatusCode();

    // Act & Assert - Add to cart
    var addCartData = new Dictionary<string, string>
    {
        { "variantId", "1" },
        { "quantity", "2" }
    };
    var cartResponse = await client.PostAsync("/Cart/AddToCart", 
        new FormUrlEncodedContent(addCartData));
    cartResponse.EnsureSuccessStatusCode();

    // Act & Assert - View cart
    var viewCartResponse = await client.GetAsync("/Cart");
    viewCartResponse.EnsureSuccessStatusCode();

    // Act & Assert - Checkout
    var checkoutResponse = await client.GetAsync("/Order/Checkout");
    checkoutResponse.EnsureSuccessStatusCode();
}
```

## Service Tests

### Testing ChatAiService
```csharp
public class ChatAiServiceTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly IChatAiService _service;
    private readonly ApplicationDbContext _context;

    public ChatAiServiceTests(CustomWebApplicationFactory<Program> factory)
    {
        var scope = factory.Services.CreateScope();
        _service = scope.ServiceProvider.GetRequiredService<IChatAiService>();
        _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    [Fact]
    public async Task GetResponseAsync_OrderQuery_ReturnsOrderStatus()
    {
        // Arrange
        var order = new DonHang { IddonHang = 1, IdtrangThai = 1 };
        _context.DonHangs.Add(order);
        await _context.SaveChangesAsync();

        // Act
        var response = await _service.GetResponseAsync("đơn hàng #1");

        // Assert
        response.Should().Contain("Đơn hàng");
    }
}
```

## FluentAssertions Best Practices

### String Assertions
```csharp
// Check contains
content.Should().Contain("Expected text");

// Check doesn't contain
content.Should().NotContain("Unexpected text");

// Check starts/ends with
content.Should().StartWith("<!DOCTYPE html>");
content.Should().EndWith("</html>");

// Check matches regex
content.Should().MatchRegex(@"\d{3}-\d{3}-\d{4}");
```

### Status Code Assertions
```csharp
// Success codes
response.StatusCode.Should().Be(HttpStatusCode.OK);
response.EnsureSuccessStatusCode(); // Throws if not 2xx

// Redirect
response.StatusCode.Should().Be(HttpStatusCode.Redirect);
response.Headers.Location.Should().NotBeNull();

// Client errors
response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
response.StatusCode.Should().Be(HttpStatusCode.NotFound);

// Authorization
response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
```

### Collection Assertions
```csharp
var products = await _context.SanPhams.ToListAsync();

products.Should().NotBeEmpty();
products.Should().HaveCount(5);
products.Should().Contain(p => p.TenSanPham == "Test Product");
products.Should().OnlyContain(p => p.TrangThai == true);
```

### Async Assertions
```csharp
Func<Task> act = async () => await _service.GetResponseAsync(null);
await act.Should().ThrowAsync<ArgumentNullException>();
```

## Known Test Issues

### Current Failing Tests (3/32)
1. **AccountControllerTests.Register_Get_ReturnsRegisterPage**
   - Issue: Looking for "Đăng ký" but page has "Tạo tài khoản"
   - Fix: Update assertion or view text

2. **CartControllerTests.GetCartCount_ReturnsCorrectCount**
   - Issue: Returns `{"success":true,"count":0}` instead of just "2"
   - Fix: Parse JSON or update assertion

3. **ShoppingFlowTests.CartManagement_AddUpdateRemove**
   - Issue: Same as above - JSON parsing needed
   - Fix: Deserialize JSON response

## Running Tests

### Command Line
```powershell
# Run all tests
cd FashionHub2
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "FullyQualifiedName~CartControllerTests"

# Run tests in specific class
dotnet test --filter "FullyQualifiedName~AccountControllerTests"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Visual Studio / Rider
- Test Explorer panel
- Right-click test method → Run
- Debug tests with breakpoints

## Test Data Management

### Reset Database Between Tests
```csharp
public void Dispose()
{
    // Clean up test data after each test
    _context.Database.EnsureDeleted();
}
```

### Isolated Test Data
```csharp
[Fact]
public async Task Test_WithIsolatedData()
{
    // Create data specific to this test
    var product = new SanPham { /* ... */ };
    _context.SanPhams.Add(product);
    await _context.SaveChangesAsync();

    // Test logic...

    // Cleanup happens in Dispose()
}
```

## Mocking External Dependencies

### Mocking IConfiguration
```csharp
var mockConfig = new Mock<IConfiguration>();
mockConfig.Setup(c => c["GeminiAI:ApiKey"]).Returns("test-key");

var service = new ChatAiService(_context, mockConfig.Object, _httpClientFactory, _logger);
```

### Mocking HttpClient
```csharp
var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
mockHttpMessageHandler
    .Protected()
    .Setup<Task<HttpResponseMessage>>("SendAsync", 
        ItExpr.IsAny<HttpRequestMessage>(), 
        ItExpr.IsAny<CancellationToken>())
    .ReturnsAsync(new HttpResponseMessage
    {
        StatusCode = HttpStatusCode.OK,
        Content = new StringContent("{\"response\":\"test\"}")
    });

var httpClient = new HttpClient(mockHttpMessageHandler.Object);
```

## Continuous Integration

### GitHub Actions Example
```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '10.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
      working-directory: ./FashionHub2
    
    - name: Build
      run: dotnet build --no-restore
      working-directory: ./FashionHub2
    
    - name: Test
      run: dotnet test --no-build --verbosity normal
      working-directory: ./FashionHub2
```

## Test Maintenance

### Regular Reviews
- Review failing tests weekly
- Update tests when requirements change
- Remove obsolete tests
- Refactor duplicated test code

### Test Coverage Goals
- Controllers: 80%+ action coverage
- Services: 90%+ method coverage
- Critical paths: 100% coverage (cart, checkout, payment)

### When to Skip Tests
```csharp
[Fact(Skip = "Waiting for ImageSharp migration")]
public async Task SearchByImage_ValidImage_ReturnsResults()
{
    // Test implementation...
}
```

## Debugging Failed Tests

### 1. Read the error message carefully
```
Expected content "..." to contain "Đăng ký".
```

### 2. Inspect actual response
```csharp
var content = await response.Content.ReadAsStringAsync();
_output.WriteLine(content); // Requires ITestOutputHelper
```

### 3. Check test data
```csharp
var products = await _context.SanPhams.ToListAsync();
_output.WriteLine($"Products count: {products.Count}");
```

### 4. Run test in isolation
```powershell
dotnet test --filter "FullyQualifiedName~Register_Get_ReturnsRegisterPage"
```

### 5. Debug with breakpoints
- Use IDE debugger
- Step through test execution
- Inspect variables
