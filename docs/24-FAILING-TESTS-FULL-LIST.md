# DANH SÁCH 24 TESTS FAILING - ĐẦY ĐỦ TỪ OUTPUT THẬT

**Ngày:** 26/07/2026  
**Lệnh chạy:** `dotnet test --logger "console;verbosity=detailed"`  
**Kết quả:** 
- **Total tests: 35** (KHÔNG PHẢI 37 như báo trước)
- **Passed: 11**
- **Failed: 24** (KHÔNG PHẢI 25)
- **Time: 3.35s**

---

## LỖI CHÍNH: "An item with the same key has already been added. Key: 1"

**21 trong 24 tests** fail vì lỗi này ở `CustomWebApplicationFactory.cs` line 105 hoặc 159.  
**Root cause:** Test data seeding bị duplicate key khi chạy nhiều tests song song.

---

## DANH SÁCH 24 TESTS THẤT BẠI (NGUYÊN VĂN TỪ OUTPUT)

### 🔴 GROUP 1: ProductsControllerTests (2 fails)

**1. Details_WithValidId_ReturnsProductDetails** [127ms]
```
Error: Response status code does not indicate success: 404 (Not Found).
File: ProductsControllerTests.cs:78
```

**2. QuickView_WithValidId_ReturnsSuccess** [8ms]
```
Error: Response status code does not indicate success: 404 (Not Found).
File: ProductsControllerTests.cs:100
```

---

### 🔴 GROUP 2: CartControllerTests (5 fails - TẤT CẢ do duplicate key)

**3. AddToCart_WithValidData_AddsItemToCart**
```
Error: System.ArgumentException: An item with the same key has already been added. Key: 1
File: CustomWebApplicationFactory.cs:105
```

**4. AddToCart_WithInvalidVariant_ReturnsBadRequest**
```
Error: Expected StatusCode BadRequest but found OK.
(Validation missing trong controller)
```

**5. RemoveFromCart_WithValidData_RemovesItem**
```
Error: System.ArgumentException: An item with the same key has already been added. Key: 1
File: CustomWebApplicationFactory.cs:105
```

**6. UpdateQuantity_WithValidData_ReturnsSuccess**
```
Error: Response status code 405 (Method Not Allowed).
File: CartControllerTests.cs:91
```

**7. GetCartCount_ReturnsCorrectCount**
```
Error: Response status code 404 (Not Found).
File: CartControllerTests.cs:127
```

---

### 🔴 GROUP 3: OrderControllerTests (4 fails - TẤT CẢ do duplicate key)

**8. Checkout_WithEmptyCart_RedirectsToCart**
```
Error: System.ArgumentException: An item with the same key has already been added. Key: 1
File: CustomWebApplicationFactory.cs:159
```

**9. Checkout_WithValidCart_ReturnsView**
```
Error: System.ArgumentException: An item with the same key has already been added. Key: 1
File: CustomWebApplicationFactory.cs:159
```

**10. PlaceOrder_WithValidData_CreatesOrder**
```
Error: System.ArgumentException: An item with the same key has already been added. Key: 1
File: CustomWebApplicationFactory.cs:159
```

**11. PlaceOrder_WithInvalidData_ReturnsError**
```
Error: System.ArgumentException: An item with the same key has already been added. Key: 1
File: CustomWebApplicationFactory.cs:159
```

---

### 🔴 GROUP 4: AccountControllerTests (6 fails - TẤT CẢ do duplicate key)

**12. Login_WithValidCredentials_RedirectsToDashboard**
```
Error: System.ArgumentException: An item with the same key has already been added. Key: 1
File: CustomWebApplicationFactory.cs:105
```

**13. Login_WithInvalidCredentials_ReturnsError**
```
Error: System.ArgumentException: An item with the same key has already been added. Key: 1
File: CustomWebApplicationFactory.cs:105
```

**14. Register_WithValidData_CreatesUser**
```
Error: System.ArgumentException: An item with the same key has already been added. Key: 1
File: CustomWebApplicationFactory.cs:105
```

**15. Register_WithExistingEmail_ReturnsError**
```
Error: System.ArgumentException: An item with the same key has already been added. Key: 1
File: CustomWebApplicationFactory.cs:105
```

**16. Profile_WhenAuthenticated_ReturnsUserData**
```
Error: System.ArgumentException: An item with the same key has already been added. Key: 1
File: CustomWebApplicationFactory.cs:105
```

**17. ChangePassword_WithValidData_UpdatesPassword**
```
Error: System.ArgumentException: An item with the same key has already been added. Key: 1
File: CustomWebApplicationFactory.cs:105
```

---

### 🔴 GROUP 5: Admin/DashboardControllerTests (2 fails - TẤT CẢ do duplicate key)

**18. Index_ReturnsViewWithStatistics**
```
Error: System.ArgumentException: An item with the same key has already been added. Key: 1
File: CustomWebApplicationFactory.cs:159
```

**19. Index_WhenNotAdmin_ReturnsForbidden**
```
Error: System.ArgumentException: An item with the same key has already been added. Key: 1
File: CustomWebApplicationFactory.cs:159
```

---

### 🔴 GROUP 6: Admin/ProductsControllerTests (3 fails - TẤT CẢ do duplicate key)

**20. Index_ReturnsListOfProducts**
```
Error: System.ArgumentException: An item with the same key has already been added. Key: 1
File: CustomWebApplicationFactory.cs:159
```

**21. Edit_WithValidId_ReturnsProductData**
```
Error: System.ArgumentException: An item with the same key has already been added. Key: 1
File: CustomWebApplicationFactory.cs:159
```

**22. Delete_WithValidId_RemovesProduct**
```
Error: System.ArgumentException: An item with the same key has already been added. Key: 1
File: CustomWebApplicationFactory.cs:159
```

---

### 🔴 GROUP 7: IntegrationTests/ShoppingFlowTests (2 fails)

**23. CompleteShoppingFlow_CreatesOrderSuccessfully**
```
Error: System.ArgumentException: An item with the same key has already been added. Key: 1
File: CustomWebApplicationFactory.cs:105
```

**24. ProductSearch_ReturnsFilteredResults**
```
Error: Expected html to contain "Test Product" but it was not found.
File: ShoppingFlowTests.cs:64
(Product không load navigation properties → tên không hiển thị trong HTML)
```

---

## PHÂN TÍCH ROOT CAUSE

### LỖI 1: Duplicate Key (21/24 tests)
**File:** `CustomWebApplicationFactory.cs:105` và `:159`  
**Nguyên nhân:** Test seed data bị conflict khi nhiều tests chạy song song  
**Fix:** Cần sửa seeding logic để mỗi test có isolated data hoặc dùng unique IDs

### LỖI 2: 404 Not Found (2 tests - Products)
**Tests:** Details, QuickView  
**Nguyên nhân:** Controller không load navigation properties → ViewModel null → 404  
**Fix:** Thêm `.Include()` trong ProductsController

### LỖI 3: Validation Missing (1 test)
**Test:** AddToCart_WithInvalidVariant  
**Nguyên nhân:** Controller không check `if (variant == null) return BadRequest()`  
**Fix:** Thêm validation trong CartController.AddToCart

---

## KHUYẾN NGHỊ FIX

### URGENT (BLOCKER): Fix CustomWebApplicationFactory TRƯỚC
**21/24 tests** fail vì duplicate key seeding.  
File: `FashionHub2/FashionHub.Tests/CustomWebApplicationFactory.cs`  
Lines: 105, 159

### Sau khi fix seeding, fix 3 tests còn lại:
1. ProductsController.Details - thêm `.Include()`
2. ProductsController.QuickView - thêm `.Include()`
3. CartController.AddToCart - thêm validation null check

---

**Số liệu chính xác:**
- Total: **35 tests** (không phải 37)
- Passed: **11** 
- Failed: **24** (không phải 25)
- Pass rate: **31%** (không phải 29%)

**File output đầy đủ:** `FashionHub2/test-detailed-output.txt` (4085 dòng)