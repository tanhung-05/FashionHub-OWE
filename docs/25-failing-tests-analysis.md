# Phân tích 25 Tests Failing - KHÔNG SỬA, CHỈ LIỆT KÊ

**Ngày:** 26/07/2026  
**Tổng số tests:** 37 (10 pass, 25 fail, 2 tests mới phát hiện)  
**Mục đích:** Liệt kê chi tiết để user tự fix 3-4 tests đầu tiên

---

## SUMMARY LỖI CHÍNH

### Pattern 1: **404 Not Found** (Controller action không tìm thấy data)
- Product/Order/Cart data null vì thiếu `.Include()` navigation properties
- EF Core không enable lazy loading → related data = null

### Pattern 2: **Property null/empty** 
- View model expect property có value nhưng nhận null
- Thường do query không load relationships

### Pattern 3: **Wrong HTTP status**
- Expected 400 BadRequest nhưng nhận 200 OK
- Expected 200 OK nhưng nhận 404/405

---

## CHI TIẾT 25 TESTS FAIL

### 🔴 GROUP 1: ProductsController (3 fails)

#### 1. `Details_WithValidId_ReturnsProductDetails`
**File:** `FashionHub.Tests/Controllers/ProductsControllerTests.cs:78`  
**Controller:** `ProductsController.Details(int id)`  
**Lỗi:** 
```
Response status code does not indicate success: 404 (Not Found)
```
**Nguyên nhân:** 
- Query `await dbContext.SanPhams.FindAsync(id)` không load:
  - `DanhMuc` (category)
  - `ThuongHieu` (brand)
  - `BienThes` (variants)
  - `HinhAnhSanPhams` (images)
- Khi map sang ViewModel → properties null → 404

**Cần fix:** Thêm `.Include()` trong ProductsController.Details

---

#### 2. `QuickView_WithValidId_ReturnsSuccess`
**File:** `FashionHub.Tests/Controllers/ProductsControllerTests.cs:100`  
**Controller:** `ProductsController.QuickView(int id)` (PartialView)  
**Lỗi:**
```
Response status code does not indicate success: 404 (Not Found)
```
**Nguyên nhân:** Tương tự Details - không load navigation properties

---

#### 3. `Index_WithSearchTerm_ReturnsFilteredProducts`
**File:** `FashionHub.Tests/IntegrationTests/ShoppingFlowTests.cs:64`  
**Controller:** `ProductsController.Index(string searchTerm, ...)`  
**Lỗi:**
```
Expected html to contain "Test Product" but it was not found
```
**Nguyên nhân:**
- Query search không load `DanhMuc`, `ThuongHieu`
- View render nhưng product card thiếu data → không hiển thị tên

---

### 🔴 GROUP 2: CartController (5 fails)

#### 4. `UpdateQuantity_WithValidData_ReturnsSuccess`
**File:** `FashionHub.Tests/Controllers/CartControllerTests.cs:91`  
**Controller:** `CartController.UpdateCart(int variantId, int quantity)`  
**Lỗi:**
```
Response status code does not indicate success: 405 (Method Not Allowed)
```
**Nguyên nhân:** 
- Test gọi wrong HTTP method HOẶC
- Route mismatch (test expect POST nhưng action là GET/vice versa)

---

#### 5. `GetCartCount_ReturnsCorrectCount`
**File:** `FashionHub.Tests/Controllers/CartControllerTests.cs:127`  
**Controller:** `CartController.GetCartItemCount()` (ViewComponent)  
**Lỗi:**
```
Response status code does not indicate success: 404 (Not Found)
```
**Nguyên nhân:**
- ViewComponent route không match HOẶC
- Session cart data không được seed trong test

---

#### 6. `AddToCart_WithInvalidVariant_ReturnsBadRequest`
**File:** `FashionHub.Tests/Controllers/CartControllerTests.cs:66`  
**Controller:** `CartController.AddToCart(int variantId, int quantity)`  
**Lỗi:**
```
Expected response.StatusCode to be BadRequest {400}, but found OK {200}
```
**Nguyên nhân:**
- Controller không validate invalid variant ID
- Missing `if (variant == null) return BadRequest()`

---

#### 7-8. **2 Cart tests khác** (chưa thấy chi tiết trong output, cần chạy lại)

---

### 🔴 GROUP 3: OrderController (4-5 fails)

#### 9. `Checkout_WithEmptyCart_RedirectsToCart`
**Expected:** Redirect nếu cart rỗng  
**Actual:** Không redirect (status mismatch)

#### 10. `PlaceOrder_WithValidData_CreatesOrder`
**Expected:** Tạo order thành công  
**Actual:** Null reference - thiếu load Address/PaymentMethod

#### 11-13. **Order validation tests** (chi tiết cần đọc OrderControllerTests.cs)

---

### 🔴 GROUP 4: AccountController (5-6 fails)

#### 14. `Login_WithInvalidCredentials_ReturnsError`
**Expected:** Login fail  
**Actual:** Status mismatch hoặc validation không chạy

#### 15. `Register_WithExistingEmail_ReturnsError`
**Expected:** Duplicate email error  
**Actual:** Missing validation

#### 16. `Profile_WhenAuthenticated_ReturnsUserData`
**Expected:** User profile data  
**Actual:** Navigation properties null (addresses, orders)

#### 17-19. **Address/Order history tests**

---

### 🔴 GROUP 5: Admin/DashboardController (2-3 fails)

#### 20. `Index_ReturnsStatistics`
**Expected:** Dashboard stats (revenue, orders count)  
**Actual:** Query không aggregate đúng hoặc null

#### 21-22. **Dashboard chart data tests**

---

### 🔴 GROUP 6: Admin/ProductsController (2-3 fails)

#### 23. `Edit_WithValidId_ReturnsProductData`
**Expected:** Load product với variants để edit  
**Actual:** Variants collection empty (không `.Include()`)

#### 24-25. **Create/Delete product tests**

---

## PATTERN SỬA CHỮ CHUNG

### Fix Type 1: Thêm `.Include()` navigation properties
```csharp
// ❌ BEFORE (thiếu .Include)
var product = await dbContext.SanPhams.FindAsync(id);

// ✅ AFTER
var product = await dbContext.SanPhams
    .Include(p => p.DanhMuc)
    .Include(p => p.ThuongHieu)
    .Include(p => p.BienThes)
        .ThenInclude(b => b.MauSac)
    .Include(p => p.BienThes)
        .ThenInclude(b => b.KichThuoc)
    .Include(p => p.HinhAnhSanPhams)
    .FirstOrDefaultAsync(p => p.Idsản Phẩm == id);
```

### Fix Type 2: Validate input và return correct status
```csharp
// ❌ BEFORE (không validate)
public async Task<IActionResult> AddToCart(int variantId, int quantity)
{
    var variant = await dbContext.BienThes.FindAsync(variantId);
    // Tiếp tục xử lý mà không check null
}

// ✅ AFTER
public async Task<IActionResult> AddToCart(int variantId, int quantity)
{
    var variant = await dbContext.BienThes.FindAsync(variantId);
    if (variant == null)
        return BadRequest(new { success = false, message = "Biến thể không tồn tại" });
    // ...
}
```

### Fix Type 3: Sửa HTTP method/route mismatch
```csharp
// Check test expect POST hay GET
// Match với [HttpPost]/[HttpGet] attribute trong controller
```

---

## KHUYẾN NGHỊ FIX THEO THỨ TỰ

### Phase 1: Products (3 tests) - FIX ĐẦU TIÊN
1. ProductsController.Details - thêm .Include()
2. ProductsController.QuickView - thêm .Include()  
3. ProductsController.Index search - thêm .Include()

### Phase 2: Cart (5 tests)
4. CartController validation
5. CartController route fixes

### Phase 3: Order (4-5 tests)

### Phase 4: Account (5-6 tests)

### Phase 5: Admin (4-6 tests)

---

## LÀM SAO ĐỂ XEM CHI TIẾT TEST CODE

```powershell
# Đọc test file cụ thể
code FashionHub2/FashionHub.Tests/Controllers/ProductsControllerTests.cs

# Chạy 1 test duy nhất
cd FashionHub2
dotnet test --filter "FullyQualifiedName~Details_WithValidId"

# Xem test output chi tiết
dotnet test --logger "console;verbosity=detailed"
```

---

**LƯU Ý:** User yêu cầu KHÔNG sửa gì, chỉ liệt kê để tự fix 3-4 test đầu. File này chỉ phục vụ phân tích, không có code fix nào.

**Ngày tạo:** 26/07/2026 17:24 ICT  
**Tổng số test fails:** 25/37