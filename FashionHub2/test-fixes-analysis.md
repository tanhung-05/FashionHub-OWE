# Analysis of 8 Failing Tests - Quick Fix Plan

## Test Failures Summary (27/35 passing, 8 failing)

### 1. AccountControllerTests.Register_Get_ReturnsRegisterPage
**Issue**: Test expects "Đăng ký" but page contains "Tạo tài khoản" (button text)
**Actual**: ViewData["Title"] = "Đăng ký" (line 4 of Register.cshtml) - so "Đăng ký" IS in the page
**Fix**: Test assertion is correct, page should contain it. Need to verify why test fails.

### 2. AccountControllerTests.AccessDenied_ReturnsAccessDeniedPage
**Issue**: Test expects "Truy cập bị từ chối" 
**Actual**: Page contains "Không có quyền truy cập" (line 16 of AccessDenied.cshtml)
**Fix**: Update test to expect "Không có quyền truy cập"

### 3-6. CartControllerTests failures (4 tests)
- RemoveItem_WithValidVariant_ReturnsSuccess
- UpdateQuantity_WithValidData_ReturnsSuccess  
- GetCartCount_ReturnsCorrectCount
- AddToCart_WithInvalidVariant_ReturnsBadRequest

**Issue**: Cart API methods likely not handling session-based cart properly in test environment OR expecting JSON responses
**Root cause**: Tests POST form data but CartController methods might return JSON for AJAX calls
**Fix**: Need to check CartController's AddToCart, UpdateQuantity, RemoveItem, GetCartCount methods

### 7. ProductsControllerTests.QuickView_WithValidId_ReturnsSuccess
**Issue**: 404 Not Found
**Root cause**: QuickView action likely doesn't include necessary .Include() for related data in InMemory DB
**Fix**: Check ProductsController.QuickView action

### 8. ShoppingFlowTests.CartManagement_AddUpdateRemove
**Issue**: 405 Method Not Allowed
**Root cause**: Integration test calling wrong HTTP verb or route
**Fix**: Check test expectations vs actual Cart controller methods

## Quick Diagnosis Plan
1. Run single failing test with detailed output to see exact error
2. For Account tests: just fix assertion text
3. For Cart tests: check if methods exist and return correct types
4. For Products QuickView: check if action exists and has proper includes
5. For Shopping flow: check HTTP verbs match

## Priority
Fix Account tests first (easiest - just text), then investigate Cart/Products controller issues.