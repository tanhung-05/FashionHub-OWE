# Test Fixing Skill

## Purpose
Fix failing tests systematically by analyzing root causes, not just patching symptoms.

## When to Use
- Tests are failing after code changes
- Need to investigate test failures
- Test assertions need updating after refactoring

## Workflow

### 1. Run Tests and Collect Evidence
```powershell
cd FashionHub2

# Run all tests with detailed output
dotnet test --logger "console;verbosity=detailed" 2>&1 | Tee-Object -FilePath test-output.txt

# Count pass/fail
dotnet test --logger "console;verbosity=normal" 2>&1 | Select-String "Passed|Failed"
```

### 2. Analyze Each Failure

For each failing test, collect:
- **Test name**: Full qualified name
- **Expected**: What the test expects
- **Actual**: What actually happened
- **Stack trace**: Where the failure occurred

Example:
```
Test: AccountControllerTests.Register_Get_ReturnsRegisterPage
Expected: content to contain "Đăng ký"
Actual: Content has "Tạo tài khoản" instead
Location: Line 29 in AccountControllerTests.cs
```

### 3. Investigate Root Cause

#### Option A: Test is Wrong (Requirements Changed)
```csharp
// View was updated to use "Tạo tài khoản" instead of "Đăng ký"
// Solution: Update test assertion

// BEFORE
content.Should().Contain("Đăng ký");

// AFTER
content.Should().Contain("Tạo tài khoản");
```

#### Option B: Code is Wrong
```csharp
// Code doesn't match requirements
// Solution: Fix the code, not the test

// Example: Missing .Include() causing null reference
var product = await _context.SanPhams
    .Include(s => s.IddanhMucNavigation) // ADD THIS
    .FirstOrDefaultAsync(s => s.IdsanPham == id);
```

#### Option C: Test Setup is Wrong
```csharp
// Test data not properly seeded
// Solution: Fix test data setup

private static void SeedTestData(ApplicationDbContext db)
{
    // Ensure all required navigation properties are seeded
    var category = new DanhMuc { IddanhMuc = 1, TenDanhMuc = "Test" };
    db.DanhMucs.Add(category);
    
    var product = new SanPham 
    { 
        IdsanPham = 1,
        IddanhMuc = 1, // Link to category
        TenSanPham = "Test Product"
    };
    db.SanPhams.Add(product);
    
    db.SaveChanges();
}
```

### 4. Common Test Failure Patterns

#### JSON Response Parsing
```csharp
// PROBLEM: Expecting plain text but getting JSON
var count = await response.Content.ReadAsStringAsync();
count.Should().Contain("2"); // FAILS: gets {"success":true,"count":2}

// SOLUTION: Parse JSON
var json = await response.Content.ReadAsStringAsync();
var result = JsonSerializer.Deserialize<JsonElement>(json);
result.GetProperty("count").GetInt32().Should().Be(2);
```

#### Text Encoding Issues
```csharp
// PROBLEM: Unicode characters don't match
content.Should().Contain("Đăng ký"); // FAILS with encoding issues

// SOLUTION: Normalize or use less strict assertion
content.Should().Contain("ng k"); // More lenient
// OR fix encoding in response
```

#### Missing Navigation Properties
```csharp
// PROBLEM: NullReferenceException accessing navigation
var category = product.IddanhMucNavigation.TenDanhMuc; // NULL!

// SOLUTION: Add .Include() in query
var product = await _context.SanPhams
    .Include(s => s.IddanhMucNavigation)
    .FirstOrDefaultAsync(s => s.IdsanPham == id);
```

#### Authentication Required
```csharp
// PROBLEM: Test fails because endpoint requires auth

// SOLUTION: Mock authentication
var client = _factory.CreateClient();
client.DefaultRequestHeaders.Add("Cookie", "FashionHub.Auth=test-token");

// OR: Add test authentication in CustomWebApplicationFactory
```

### 5. Fix Tests Systematically

```powershell
# 1. Fix one test at a time
# 2. Run ONLY that test to verify fix
dotnet test --filter "FullyQualifiedName~Register_Get_ReturnsRegisterPage"

# 3. If fixed, run all tests to ensure no regression
dotnet test

# 4. Commit the fix
git add FashionHub2/FashionHub.Tests/Controllers/AccountControllerTests.cs
git commit -m "test(account): fix Register_Get assertion for updated UI text"
```

### 6. Verify No Regression

After fixing tests:
```powershell
# Run full build
dotnet build

# Run all tests
dotnet test --logger "console;verbosity=normal"

# Check test count hasn't decreased
# Expected: X passing, 0 failing

# Try running tests multiple times to catch flaky tests
for ($i=1; $i -le 3; $i++) {
    Write-Host "Test run $i"
    dotnet test --logger "console;verbosity=minimal"
}
```

## Current Known Failures

### Test 1: Register_Get_ReturnsRegisterPage
**Status**: IDENTIFIED
**Cause**: UI text changed from "Đăng ký" to "Tạo tài khoản"
**Solution**: Update assertion
**File**: `FashionHub.Tests/Controllers/AccountControllerTests.cs:29`
**Fix**:
```csharp
// Line 29
content.Should().Contain("Tạo tài khoản");
```

### Test 2: GetCartCount_ReturnsCorrectCount
**Status**: IDENTIFIED
**Cause**: Response is JSON `{"success":true,"count":2}` not plain "2"
**Solution**: Parse JSON response
**File**: `FashionHub.Tests/Controllers/CartControllerTests.cs:133`
**Fix**:
```csharp
var json = await response.Content.ReadAsStringAsync();
var result = JsonSerializer.Deserialize<JsonElement>(json);
result.GetProperty("count").GetInt32().Should().Be(2);
```

### Test 3: CartManagement_AddUpdateRemove
**Status**: IDENTIFIED
**Cause**: Same as Test 2 - JSON parsing issue
**Solution**: Parse JSON response
**File**: `FashionHub.Tests/IntegrationTests/ShoppingFlowTests.cs:102`
**Fix**: Same as Test 2

## Anti-Patterns to Avoid

### ❌ DON'T Just Comment Out Failing Tests
```csharp
// [Fact]
// public async Task Register_Get_ReturnsRegisterPage()
// {
//     // Test implementation...
// }
```

### ❌ DON'T Change Expected Behavior Without Understanding Why
```csharp
// WRONG: Blindly changing assertion
content.Should().Contain("anything"); // Just to make it pass
```

### ❌ DON'T Fix Multiple Unrelated Tests in One Commit
```bash
# BAD
git commit -m "fix tests"

# GOOD
git commit -m "test(account): fix Register_Get assertion for updated UI"
git commit -m "test(cart): parse JSON response in GetCartCount"
```

### ❌ DON'T Skip Investigation
```csharp
// WRONG: Adding try-catch to hide failures
try {
    var result = product.IddanhMucNavigation.TenDanhMuc;
} catch {
    // Ignore - BAD!
}

// RIGHT: Fix root cause
var product = await _context.SanPhams
    .Include(s => s.IddanhMucNavigation)
    .FirstOrDefaultAsync(s => s.IdsanPham == id);
```

## Best Practices

### ✅ Test One Thing at a Time
```csharp
[Fact]
public async Task AddToCart_ValidVariant_AddsToCart() 
{
    // Test ONLY the add functionality
}

[Fact]
public async Task AddToCart_InvalidVariant_ReturnsBadRequest()
{
    // Test ONLY the validation
}
```

### ✅ Use Descriptive Assertions
```csharp
// GOOD: Clear what's expected
content.Should().Contain("Tạo tài khoản", 
    "because the Register page should display Vietnamese heading");

// BETTER: Shows actual vs expected on failure
var title = GetPageTitle(content);
title.Should().Be("Đăng ký - FashionHub");
```

### ✅ Clean Up Test Data
```csharp
public void Dispose()
{
    _context.Database.EnsureDeleted();
    _context.Dispose();
}
```

### ✅ Document Complex Test Setup
```csharp
// This test verifies the complete checkout flow including:
// 1. Cart with multiple items
// 2. Coupon code application
// 3. Address validation
// 4. Payment method selection
// 5. Order creation
[Fact]
public async Task CompleteCheckout_WithCoupon_CreatesOrderWithDiscount()
{
    // Test implementation...
}
```

## Debugging Tips

### Use ITestOutputHelper
```csharp
public class CartControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly ITestOutputHelper _output;

    public CartControllerTests(
        CustomWebApplicationFactory<Program> factory, 
        ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Test()
    {
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {content}"); // Visible in test output
    }
}
```

### Inspect Database State
```csharp
[Fact]
public async Task Test()
{
    // Check what's actually in the database
    var products = await _context.SanPhams.ToListAsync();
    _output.WriteLine($"Products in DB: {products.Count}");
    foreach (var p in products)
    {
        _output.WriteLine($"  - {p.TenSanPham} (ID: {p.IdsanPham})");
    }
}
```

### Use Debugger
- Set breakpoint in test method
- Run test in debug mode
- Step through execution
- Inspect variables

## Reporting Results

After fixing tests, document:
```markdown
## Test Fix Summary

**Date**: 2026-07-29
**Total Tests**: 32
**Status**: 32 passing, 0 failing

### Fixes Applied

1. **AccountControllerTests.Register_Get_ReturnsRegisterPage**
   - Root cause: UI text updated from "Đăng ký" to "Tạo tài khoản"
   - Fix: Updated test assertion to match new UI text
   - Commit: abc123

2. **CartControllerTests.GetCartCount_ReturnsCorrectCount**
   - Root cause: Response format changed to JSON
   - Fix: Added JSON parsing in test
   - Commit: def456

3. **ShoppingFlowTests.CartManagement_AddUpdateRemove**
   - Root cause: Same as #2
   - Fix: Added JSON parsing in test
   - Commit: ghi789

### Verification
```bash
dotnet build
# Output: Build succeeded. 0 Error(s), 24 Warning(s)

dotnet test
# Output: Passed! - Failed: 0, Passed: 32, Skipped: 0, Total: 32
```

### No Regression Confirmed
- All 32 tests passing
- Build warnings unchanged (24 non-critical)
- No new issues introduced
```
