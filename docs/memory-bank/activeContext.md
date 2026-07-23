# Active Context — FashionHub Migration

**Cập nhật:** 2026-07-23

## Trạng thái hiện tại

**Project:** FashionHub2/FashionHub.Web  
**Framework:** ASP.NET Core MVC on .NET 10  
**Database:** SQL Server (EF Core)  
**Build:** ✅ Build succeeded (20 warnings, 0 errors)
**Tiến độ:** 58% (11.5/20 prompts)

✅ **Tiến triển mới:** 5 commits từ 20/07 đến 23/07

## Công việc gần đây (20/07-23/07/2026)

### Đã hoàn thành
1. ✅ Copy complete CSS/JS from original project (commit `c8573c4`)
2. ✅ Add Bootstrap CSS to _Layout (commit `3ab059b`)
3. ✅ Redirect homepage to Products (commit `c2cdf59`)
4. ✅ Fix image paths với SQL script (commit `8ad5590`)
5. ✅ Admin Products Views: Index, Create, Edit (commit `da8a680`)
6. ✅ Admin Products ViewModels với variants support (commit `da8a680`)

### Verified
- ✅ Build: 0 errors, 0 warnings
- ✅ Runtime: App khởi động clean trên http://localhost:5167
- ✅ Security: API key không hardcode, SearchByImage vẫn disabled
- ✅ No regression issues

## Những gì đã migrate (Customer-facing)

### Controllers ✅
- HomeController
- AccountController (Login, Register, Logout, AccessDenied)
- ProductsController (Index, Details)
- CartController (full AJAX API)
- OrderController (Checkout, OrderSuccess, ApplyCoupon, AddAddress)
- ChatController (SendMessage)

### Views ✅
- Home/Index
- Products: Index (grid + filter), Details
- Cart/Index
- Order: Checkout, OrderSuccess
- Account: Login, Register, AccessDenied, _AuthLayout
- Shared: _Layout, _Header, _Menu, _Footer, _GlobalFeedback, _CartOffcanvas, _ChatWidget
- Partials: _ProductCard, _QuickViewModal, _AddAddressModal

### Services ✅
- ChatAiService (Gemini API)
- ImageFeatureService (stub, disabled)

## Những gì đã migrate (Admin)

### Partial (40%)
- ✅ Admin/OrdersController: full CRUD đơn hàng
- ✅ Admin/Orders views: Index, Details, Invoice, BulkPrint
- ✅ Admin/_Layout, Admin/_ViewStart
- 🔶 Admin/ProductsController: có Index stub, cần implement 8 actions còn lại
- 🔶 Admin/Products views: Index, Create, Edit (UI ready, chờ backend)
- 🔶 Admin/Products ViewModels: ProductAdminViewModel, ProductVariantAdminViewModel, etc.
- ❌ Admin/DashboardController: chưa có
- ❌ Admin/CategoriesController: chưa có
- ❌ Admin/UsersController: chưa có

## Những gì chưa migrate / đang dở

### Admin (Đang thực hiện)
1. **Admin Products CRUD** (Prompt 13) — 🔶 ĐANG LÀM DỞ
   - ✅ Views: Index, Create, Edit (UI complete với AJAX)
   - ✅ ViewModels: ProductAdminViewModel, ProductVariantAdminViewModel, ProductListAdminViewModel, ProductItemAdminViewModel, VariantDetailViewModel, VariantImageViewModel
   - ✅ Controller: Index action (basic stub)
   - ❌ **CẦN LÀM TIẾP:** Implement 8 actions trong ProductsController:
     1. Create GET (load dropdowns)
     2. Create POST (save product)
     3. Edit GET (load product + variants)
     4. Edit POST (update product info)
     5. AddVariant (AJAX - thêm biến thể màu/size)
     6. DeleteVariant (AJAX - xóa biến thể)
     7. ImportStock (AJAX - nhập kho)
     8. GetVariantImages (AJAX - lấy danh sách ảnh)
     9. UploadImages (AJAX - upload ảnh cho variant)
     10. DeleteImage (AJAX - xóa ảnh)
   - ❌ Image upload file handling
   - ❌ ViewBag data cho dropdowns (DanhMucs, ThuongHieux, Colors, Sizes)

2. **Admin Dashboard & Categories** (Prompt 14)
   - DashboardController với stats (doanh thu, đơn hàng, sản phẩm)
   - CategoriesController CRUD

3. **Admin Users & Promotions** (Prompt 15)
   - UsersController: Index, Details, ToggleLock
   - Promotions/Coupons CRUD

### Customer Features
4. **User Profile & Order History** (Prompt 17)
   - AccountController: Profile, OrderHistory
   - Views tương ứng

### Quality Assurance
5. **CSS/JS Review & Polish** (Prompt 16) — chỉ mới fix image paths
6. **Integration Testing** (Prompt 18) — chưa bắt đầu
7. **Dockerize** (Prompt 19) — chưa bắt đầu
8. **Final Review** (Prompt 20) — chưa bắt đầu

## Known Issues & Decisions

### Resolved ✅
- ✅ API key security: moved to User Secrets
- ✅ SearchByImage: disabled by design, documented
- ✅ Image paths: fixed to ~/images/ prefix
- ✅ Build issues: không còn

### Open Items ⚠️
- ⚠️ CSS/JS chưa review toàn diện
- ⚠️ UI/UX chưa polish responsive
- ⚠️ Chưa có tests
- ⚠️ Chưa có Docker config

## Next Actions (Priority Order)

1. **[URGENT]** Hoàn thiện Prompt 13: Admin Products Controller
   - Implement 10 actions còn thiếu trong ProductsController
   - Test từng action với Views đã có
   - Handle file upload cho images
   - Commit khi hoàn tất
2. **[HIGH]** Prompt 14: Admin Dashboard & Categories
3. **[HIGH]** Prompt 15: Admin Users & Promotions
4. **[MEDIUM]** Prompt 16: CSS/JS review & UI polish
5. **[MEDIUM]** Prompt 17: User Profile & Order History
6. **[LOW]** Prompt 18: Integration Testing
7. **[LOW]** Prompt 19: Dockerize
8. **[LOW]** Prompt 20: Final Review & Cleanup

## Technical Notes

### Architecture Decisions
- ViewComponents thay partial views có logic (Menu, CartIcon)
- Service layer cho business logic (ChatAiService)
- ViewModels cho data transfer to views
- Cookie Authentication (no Identity yet)
- Session-based cart (không dùng database)

### Database Schema
- 28 entities scaffolded từ SQL Server
- Không đổi schema khi migrate
- Entity trong Models/Generated/
- ApplicationDbContext trong Data/

### Static Files
- CSS: wwwroot/css/site.css (copied từ FashionHub/Content/)
- JS: wwwroot/js/site.js (copied từ FashionHub/Scripts/)
- Images: wwwroot/images/ (prefix ~/images/ trong views)

### Authentication Flow
- Cookie-based với `CookieAuthenticationDefaults.AuthenticationScheme`
- User claims: Id, Email, Role
- [Authorize] cho customer pages
- [Authorize(Roles = "Admin")] cho admin area

## Files to Reference

### Documentation
- `docs/migration-progress-report-v2.md` — comprehensive status report (2026-07-20)
- `docs/migration-comparison-report.md` — old comparison (2026-07-05)
- `docs/chat-ai-implementation-clarification.md` — Chat AI decisions
- `docs/searchbyimage-status.md` — SearchByImage disabled status
- `docs/ui-testing-checklist.md` — UI testing notes
- `FashionHub-AI-Agent-Roadmap.md` — full 20-prompt roadmap

### Key Code
- `FashionHub2/FashionHub.Web/Program.cs` — startup config
- `FashionHub2/FashionHub.Web/Data/ApplicationDbContext.cs` — EF Core context
- `FashionHub2/FashionHub.Web/Controllers/*.cs` — customer controllers
- `FashionHub2/FashionHub.Web/Areas/Admin/Controllers/*.cs` — admin controllers
- `FashionHub2/FashionHub.Web/Services/*.cs` — business logic
- `FashionHub2/FashionHub.Web/ViewModels/**/*.cs` — data transfer objects

### Old Project (Reference Only)
- `FashionHub/` — ASP.NET MVC 5 cũ, KHÔNG sửa
- Dùng để tham khảo logic khi migrate