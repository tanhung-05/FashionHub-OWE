# Admin Users & Coupons Testing Plan

## Test Environment Setup

### Prerequisites
1. Database connection configured in `appsettings.Development.json`
2. Admin account with access to `/Admin` area
3. Test data: At least 2-3 users and 2-3 coupons in database

### Test Data Required
- **Users**: 
  - Active users with orders
  - Inactive users
  - Users with different order counts
- **Coupons**:
  - Active percentage discount coupon
  - Active fixed amount discount coupon
  - Expired coupon
  - Coupon with usage limit reached

---

## Test Cases

### Admin Users Management

#### TC-U01: View Users List
**Endpoint**: `/Admin/Users/Index`

**Steps:**
1. Login as admin
2. Navigate to Admin → Khách hàng
3. Verify page loads successfully
4. Verify users table displays with columns:
   - ID
   - Tên
   - Email
   - Số điện thoại
   - Số đơn hàng
   - Tổng chi tiêu
   - Trạng thái
   - Thao tác

**Expected Result:**
- ✅ Page loads without errors
- ✅ All users displayed in table
- ✅ Data matches database records

---

#### TC-U02: Search Users
**Endpoint**: `/Admin/Users/Index?search={query}`

**Test Data:**
- Search by name
- Search by email
- Search by phone number

**Steps:**
1. Navigate to Users list page
2. Enter search term in search box
3. Click search or press Enter
4. Verify filtered results

**Expected Result:**
- ✅ Search filters users correctly
- ✅ Partial matches work
- ✅ Case-insensitive search
- ✅ Shows "Không tìm thấy..." if no results

---

#### TC-U03: View User Details
**Endpoint**: `/Admin/Users/Details/{id}`

**Steps:**
1. Navigate to Users list
2. Click "Xem chi tiết" button for a user
3. Verify user profile displayed
4. Verify order history section
5. Check statistics cards

**Expected Result:**
- ✅ User information displayed correctly
- ✅ Order history loads
- ✅ Statistics show correct counts
- ✅ Recent orders listed

---

#### TC-U04: Toggle User Status
**Endpoint**: `/Admin/Users/ToggleStatus` (POST)

**Steps:**
1. Navigate to Users list
2. Click toggle status button (power icon)
3. Confirm action in alert
4. Verify status changes
5. Check success message

**Expected Result:**
- ✅ Status toggles (Active ↔ Inactive)
- ✅ Success message displays
- ✅ Database updated correctly
- ✅ Page refreshes with new status

---

### Coupons Management

#### TC-C01: View Coupons List
**Endpoint**: `/Admin/Coupons/Index`

**Steps:**
1. Login as admin
2. Navigate to Admin → Mã giảm giá
3. Verify page loads successfully
4. Verify coupons table displays with columns:
   - ID
   - Mã code
   - Tên chương trình
   - Loại
   - Giá trị
   - Đơn tối thiểu
   - Số lượng
   - Đã dùng
   - Thời hạn
   - Trạng thái
   - Thao tác

**Expected Result:**
- ✅ Page loads without errors
- ✅ All coupons displayed
- ✅ Expired coupons highlighted
- ✅ Out of stock coupons highlighted

---

#### TC-C02: Create New Coupon - Percentage
**Endpoint**: `/Admin/Coupons/Create` (GET/POST)

**Test Data:**
```
MaCode: TEST20
TenChuongTrinh: Test 20% Off
LoaiGiamGia: 1 (Phần trăm)
GiaTri: 20
DonHangToiThieu: 100000
GiamToiDa: 50000
SoLuong: 100
NgayBatDau: 2026-07-23
NgayKetThuc: 2026-12-31
```

**Steps:**
1. Navigate to Coupons list
2. Click "Tạo mã mới"
3. Fill in form with test data
4. Click "Tạo mã"
5. Verify redirect to list
6. Check success message
7. Verify coupon appears in list

**Expected Result:**
- ✅ Form validation works
- ✅ Coupon created successfully
- ✅ Redirect to coupons list
- ✅ Success message displays
- ✅ New coupon visible in list

---

#### TC-C03: Create New Coupon - Fixed Amount
**Endpoint**: `/Admin/Coupons/Create` (GET/POST)

**Test Data:**
```
MaCode: SAVE50K
TenChuongTrinh: Giảm 50K
LoaiGiamGia: 2 (Số tiền cố định)
GiaTri: 50000
DonHangToiThieu: 200000
SoLuong: 50
NgayBatDau: 2026-07-23
NgayKetThuc: 2026-08-31
```

**Steps:**
1. Navigate to Create coupon page
2. Fill in form for fixed amount discount
3. Submit form
4. Verify creation

**Expected Result:**
- ✅ Fixed amount coupon created
- ✅ Displays with ₫ symbol
- ✅ All validations pass

---

#### TC-C04: Edit Existing Coupon
**Endpoint**: `/Admin/Coupons/Edit/{id}` (GET/POST)

**Steps:**
1. Navigate to Coupons list
2. Click edit button (pencil icon)
3. Modify fields:
   - Change TenChuongTrinh
   - Increase SoLuong
   - Extend NgayKetThuc
4. Click "Cập nhật"
5. Verify changes saved

**Expected Result:**
- ✅ Edit form pre-populated
- ✅ DaSuDung field readonly
- ✅ Changes saved successfully
- ✅ Updated coupon in list

---

#### TC-C05: Toggle Coupon Status
**Endpoint**: `/Admin/Coupons/ToggleStatus` (POST)

**Steps:**
1. Navigate to Coupons list
2. Click toggle button (power icon)
3. Confirm action
4. Verify status changes

**Expected Result:**
- ✅ Status toggles correctly
- ✅ Badge updates (Hoạt động ↔ Tắt)
- ✅ Database updated
- ✅ Success message shows

---

#### TC-C06: Delete Coupon
**Endpoint**: `/Admin/Coupons/Delete` (POST)

**Test Scenarios:**
- **Scenario A**: Delete unused coupon (should delete)
- **Scenario B**: Delete used coupon (should deactivate only)

**Steps:**
1. Navigate to Coupons list
2. Click delete button (trash icon)
3. Confirm deletion
4. Verify result based on scenario

**Expected Result:**
- ✅ Unused coupon: Removed from list
- ✅ Used coupon: Status set to Tắt
- ✅ Appropriate message displays
- ✅ No data integrity issues

---

#### TC-C07: Coupon Validation Edge Cases

**Test Cases:**
- Empty MaCode → Error
- Duplicate MaCode → Error
- GiaTri < 0 → Error
- GiaTri > 100 for percentage → Error
- NgayKetThuc < NgayBatDau → Error
- SoLuong < 0 → Error

**Expected Result:**
- ✅ All validation rules enforced
- ✅ Clear error messages
- ✅ Form doesn't submit with invalid data

---

## Integration Tests

### INT-01: Coupon Application in Checkout
**Purpose**: Verify coupons created in admin panel work in checkout

**Steps:**
1. Create active coupon in admin panel
2. Logout from admin
3. Login as regular user
4. Add products to cart
5. Go to checkout
6. Apply coupon code
7. Verify discount applied

**Expected Result:**
- ✅ Coupon code recognized
- ✅ Discount calculated correctly
- ✅ Total price updated
- ✅ Coupon usage count increments

---

### INT-02: User Order History Sync
**Purpose**: Verify user statistics update when orders placed

**Steps:**
1. Note user's current order count in admin panel
2. Place new order as that user
3. Refresh user details in admin
4. Verify order count incremented
5. Verify new order in order history

**Expected Result:**
- ✅ Order count accurate
- ✅ Total spending updated
- ✅ Order history shows new order
- ✅ Real-time data consistency

---

## Performance Tests

### PERF-01: Users List Load Time
- Load time < 2 seconds for 100 users
- Load time < 5 seconds for 1000 users

### PERF-02: Search Performance
- Search returns results < 1 second
- Handles partial matches efficiently

### PERF-03: Coupons List Load Time
- Load time < 2 seconds for any number of coupons

---

## Security Tests

### SEC-01: Authorization Check
**Test**: Access admin URLs without authentication

**Steps:**
1. Logout completely
2. Try to access `/Admin/Users/Index`
3. Try to access `/Admin/Coupons/Index`

**Expected Result:**
- ✅ Redirects to login page
- ✅ No data exposed
- ✅ Proper 401/403 status

---

### SEC-02: Role-Based Access
**Test**: Access admin URLs with non-admin user

**Steps:**
1. Login as regular customer
2. Try to access admin URLs
3. Verify access denied

**Expected Result:**
- ✅ Access denied page shown
- ✅ No admin functionality accessible

---

## Bug Tracking Template

### Bug Report Format
```
**Bug ID**: BUG-[YYMMDD]-[#]
**Component**: [Users/Coupons]
**Severity**: [Critical/High/Medium/Low]
**Status**: [Open/In Progress/Fixed/Closed]

**Description**:
[Detailed description of the bug]

**Steps to Reproduce**:
1. 
2. 
3. 

**Expected Behavior**:
[What should happen]

**Actual Behavior**:
[What actually happened]

**Screenshots/Logs**:
[If applicable]

**Environment**:
- Browser: 
- OS: 
- .NET Version: 10
```

---

## Test Execution Checklist

### Before Testing
- [ ] Database is running
- [ ] Application builds successfully
- [ ] Admin account credentials ready
- [ ] Test data prepared

### During Testing
- [ ] Follow test cases sequentially
- [ ] Document any issues found
- [ ] Take screenshots of bugs
- [ ] Note any performance issues

### After Testing
- [ ] Log all bugs found
- [ ] Prioritize bug fixes
- [ ] Update test results
- [ ] Report to team

---

## Test Results Summary Template

```
Test Date: [Date]
Tester: [Name]
Environment: [Development/Staging/Production]

Total Test Cases: [X]
Passed: [X]
Failed: [X]
Blocked: [X]
Not Executed: [X]

Pass Rate: [X]%

Critical Issues Found: [X]
High Priority Issues: [X]
Medium Priority Issues: [X]
Low Priority Issues: [X]

Notes:
[Any additional observations]
```

---

## Automated Test Suggestions

For future implementation, consider:
1. Unit tests for Controllers (xUnit)
2. Integration tests for database operations
3. UI automation tests (Selenium/Playwright)
4. API endpoint tests (if exposing APIs)