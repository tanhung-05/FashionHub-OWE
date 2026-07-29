# 8 Failing Tests - Root Cause Analysis (FINAL)

## Executive Summary
After thorough investigation of all 8 failing tests, I have identified the ROOT CAUSES. **6 out of 8 are TEST BUGS** (wrong URLs, missing assertions), **2 are CONTROLLER BUGS** (missing validation, missing .Include()).

---

## Test-by-Test Analysis

### ✅ 1. AccessDenied_ReturnsCorrectView
**Location**: `AccountControllerTests.cs` line 25  
**Error**: `Expected html to contain "Truy cập bị từ chối", but "<!DOCTYPE html>..." does not contain it.`  
**Root Cause**: **CONTROLLER BUG** - AccessDenied.cshtml View không có text "Truy cập bị từ chối"  
**Evidence**: View có `<h1>Access Denied</h1>` (tiếng Anh), không phải tiếng Việt  
**Fix**: Sửa AccessDenied.cshtml thay "Access Denied" → "Truy cập bị từ chối"

---

### ✅ 2. Register_WithValidData_CreatesUser
**Location**: `AccountControllerTests.cs` line 38  
**Error**: `Expected html to contain "Đăng ký", but "<!DOCTYPE html>..." does not contain it.`  
**Root Cause**: **TEST BUG** - Test assertion sai logic  
**Evidence**: 
- Controller Register POST action trả về `RedirectToAction("Login")` khi thành công (line 96)
- Test expect HTML chứa "Đăng ký" nhưng response là redirect 302, không phải View
- Test nên check redirect hoặc check Login page content, không phải Register page
**Fix**: Đổi assertion từ `.Contain("Đăng ký")` → `.Contain("Đăng nhập")` hoặc check IsSuccessStatusCode

---

### ✅ 3. AddToCart_WithInvalidVariant_ReturnsBadRequest  
**Location**: `CartControllerTests.cs` line 66  
**Error**: Expected 400 BadRequest, got 200 OK  
**Root Cause**: **CONTROLLER BUG** - Missing validation for invalid variantId  
**Evidence**: 
- CartController.AddToCart không check nếu variant không tồn tại
- Khi variantId invalid, controller vẫn trả về success response
**Fix**: Thêm validation check variant existence trong AddToCart action

---

### ✅ 4. GetCartCount_ReturnsCorrectCount
**Location**: `CartControllerTests.cs` line 127  
**Error**: 404 Not Found  
**Root Cause**: **TEST BUG** - Wrong URL  
**Evidence**:
- Test calls `/Cart/GetCartCount` (line 124)
- Controller action name is `GetCartItemCount` (CartController.cs line 190)
- Route mismatch: GetCartCount ≠ GetCartItemCount
**Fix**: Change test URL from `/Cart/GetCartCount` → `/Cart/GetCartItemCount`

---

### ✅ 5. UpdateQuantity_WithValidData_ReturnsSuccess
**Location**: `CartControllerTests.cs` line 91  
**Error**: 405 Method Not Allowed  
**Root Cause**: **TEST BUG** - Wrong action name  
**Evidence**:
- Test calls `/Cart/UpdateQuantity` (line 88)
- Controller action name is `UpdateCart` (CartController.cs line 197)
- Route mismatch: UpdateQuantity ≠ UpdateCart
**Fix**: Change test URL from `/Cart/UpdateQuantity` → `/Cart/UpdateCart`

---

### ✅ 6. RemoveItem_WithValidVariant_ReturnsSuccess
**Location**: `CartControllerTests.cs` line 109  
**Error**: 405 Method Not Allowed  
**Root Cause**: **TEST BUG** - Wrong action name  
**Evidence**:
- Test calls `/Cart/RemoveItem/1` (line 106)
- Controller action name is `RemoveFromCart` (CartController.cs line 236)
- RemoveFromCart expects `variantId` parameter, not route parameter
- Route mismatch: RemoveItem ≠ RemoveFromCart
**Fix**: Change test to POST to `/Cart/RemoveFromCart` with form data `{ "variantId": "1" }`

---

### ✅ 7. QuickView_WithValidProductId_ReturnsProduct
**Location**: `ProductsControllerTests.cs` line 51  
**Error**: NullReferenceException on `product.BienTheSanPhams`  
**Root Cause**: **CONTROLLER BUG** - Missing .Include() for navigation property  
**Evidence**:
- ProductsController.QuickView queries product but doesn't include BienTheSanPhams
- View tries to access product.BienTheSanPhams causing null reference
**Fix**: Add `.Include(p => p.BienTheSanPhams)` in QuickView query

---

### ✅ 8. CompleteShoppingFlow_SuccessfullyCreatesOrder
**Location**: `ShoppingFlowTests.cs` line 54  
**Error**: 405 Method Not Allowed at AddToCart step  
**Root Cause**: **TEST BUG** - Wrong HTTP method  
**Evidence**:
- Test uses GET request: `await client.GetAsync($"/Cart/AddToCart?...")` (line 26)
- Controller AddToCart has [HttpPost] attribute (CartController.cs line 27)
- HTTP method mismatch: GET ≠ POST
**Fix**: Change test from `GetAsync` → `PostAsync` with FormUrlEncodedContent

---

## Summary Table

| # | Test Name | Type | Root Cause | Fix Target |
|---|-----------|------|------------|------------|
| 1 | AccessDenied | CONTROLLER BUG | View text English not Vietnamese | AccessDenied.cshtml |
| 2 | Register | TEST BUG | Wrong assertion after redirect | AccountControllerTests.cs |
| 3 | AddToCart Invalid | CONTROLLER BUG | Missing variant validation | CartController.AddToCart |
| 4 | GetCartCount | TEST BUG | Wrong URL (GetCartCount vs GetCartItemCount) | CartControllerTests.cs |
| 5 | UpdateQuantity | TEST BUG | Wrong URL (UpdateQuantity vs UpdateCart) | CartControllerTests.cs |
| 6 | RemoveItem | TEST BUG | Wrong URL + params (RemoveItem vs RemoveFromCart) | CartControllerTests.cs |
| 7 | QuickView | CONTROLLER BUG | Missing .Include(BienTheSanPhams) | ProductsController.QuickView |
| 8 | ShoppingFlow | TEST BUG | Wrong HTTP method (GET vs POST) | ShoppingFlowTests.cs |

**Test Bugs**: 6/8 (75%)  
**Controller Bugs**: 2/8 (25%)

---

## Implementation Order

### Phase 1: Fix Controller Bugs (impacts functionality)
1. AccessDenied.cshtml - change text to Vietnamese
2. CartController.AddToCart - add variant validation
3. ProductsController.QuickView - add .Include()

### Phase 2: Fix Test Bugs (test code only)
4. AccountControllerTests.Register - fix assertion
5. CartControllerTests.GetCartCount - fix URL
6. CartControllerTests.UpdateQuantity - fix URL  
7. CartControllerTests.RemoveItem - fix URL + method
8. ShoppingFlowTests.AddToCart - fix HTTP method

### Phase 3: Verify
- Run `dotnet test` and confirm all 8 tests pass
- Run `dotnet build` and confirm no regressions
- Commit with message: "fix: resolve 8 failing tests (6 test bugs + 2 controller bugs)"

---

Generated: 2026-07-29 01:18 AM