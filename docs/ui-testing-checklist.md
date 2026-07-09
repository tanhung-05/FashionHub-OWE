# UI Testing Checklist - FashionHub Migration

## Thời điểm: Sau khi fix Bootstrap CSS trong _Layout

### Commits đã thực hiện:
- `08426a5`: Migrate layout và shared partials sang ASP.NET Core
- `3ab059b`: Fix thiếu Bootstrap CSS trong _Layout

---

## Cách chạy app để test:

```bash
cd FashionHub2/FashionHub.Web
dotnet run
```

Sau khi server khởi động, mở browser: **http://localhost:5197**

---

## Checklist Testing UI

### 1. Trang Products (/Products)

**Layout chung:**
- [ ] Header hiển thị đầy đủ (logo, search bar, menu, cart icon)
- [ ] Header sticky khi scroll
- [ ] Menu dropdown hoạt động
- [ ] Cart icon hiển thị số lượng sản phẩm
- [ ] Footer hiển thị đầy đủ
- [ ] Chat widget hiển thị ở góc phải dưới

**Product listing:**
- [ ] Product cards hiển thị đúng layout (ảnh, tên, giá, badge sale)
- [ ] Ảnh sản phẩm load được
- [ ] Hover effect trên product card
- [ ] Badge "SALE" hiển thị cho sản phẩm giảm giá
- [ ] Button "Thêm vào giỏ" hiển thị
- [ ] Filter sidebar hiển thị (danh mục, màu sắc, kích thước, giá)
- [ ] Pagination hoạt động

**Responsive:**
- [ ] Mobile: filter collapse thành button
- [ ] Tablet: product grid 2-3 cột
- [ ] Desktop: product grid 4 cột

---

### 2. Trang Product Details (/Products/Details/{id})

**Layout:**
- [ ] Gallery ảnh sản phẩm (main image + thumbnails)
- [ ] Thông tin sản phẩm (tên, giá, mô tả)
- [ ] Chọn biến thể (màu sắc, kích thước)
- [ ] Số lượng tồn kho hiển thị
- [ ] Button "Thêm vào giỏ hàng"
- [ ] Tab mô tả chi tiết
- [ ] Sản phẩm liên quan

**Functionality:**
- [ ] Click thumbnail thay đổi main image
- [ ] Chọn màu/size cập nhật giá và tồn kho
- [ ] Quick view modal hoạt động

---

### 3. Trang Cart (/Cart)

**Layout:**
- [ ] Danh sách sản phẩm trong giỏ
- [ ] Ảnh, tên, biến thể, giá, số lượng hiển thị
- [ ] Tăng/giảm số lượng
- [ ] Xóa sản phẩm khỏi giỏ
- [ ] Tổng tiền tạm tính
- [ ] Button "Tiến hành thanh toán"

**Cart offcanvas:**
- [ ] Click cart icon mở offcanvas
- [ ] Hiển thị sản phẩm mini
- [ ] Link đến trang giỏ hàng
- [ ] Close button hoạt động

---

### 4. Trang Checkout (/Order/Checkout)

**Layout:**
- [ ] Form thông tin giao hàng
- [ ] Danh sách địa chỉ đã lưu
- [ ] Thêm địa chỉ mới
- [ ] Chọn phương thức thanh toán
- [ ] Nhập mã giảm giá
- [ ] Tóm tắt đơn hàng (sản phẩm, phí ship, giảm giá, tổng)
- [ ] Button "Đặt hàng"

**Validation:**
- [ ] Required fields có validation
- [ ] Error message hiển thị rõ ràng

---

### 5. Trang Login/Register (/Account/Login, /Account/Register)

**Layout:**
- [ ] Sử dụng _AuthLayout (không có header/footer thông thường)
- [ ] Form đăng nhập/đăng ký
- [ ] Remember me checkbox
- [ ] Link chuyển đổi Login ↔ Register
- [ ] Button submit

**Validation:**
- [ ] Email format validation
- [ ] Password strength validation (Register)
- [ ] Error message hiển thị

---

## Vấn đề UI cần chú ý

### Priority High:
- [ ] Bootstrap CSS load được (nếu không, toàn bộ layout vỡ)
- [ ] Custom CSS (site.css) load được
- [ ] Bootstrap Icons load được
- [ ] jQuery và Bootstrap JS load được

### Priority Medium:
- [ ] Responsive trên mobile/tablet
- [ ] Ảnh sản phẩm có placeholder khi chưa load
- [ ] Toast notification hoạt động
- [ ] Modal/Offcanvas hoạt động

### Priority Low:
- [ ] Animation smooth
- [ ] Hover effects
- [ ] Color palette nhất quán

---

## Known Issues (đã biết)

1. **Menu dropdown**: Cần verify ViewComponent MenuViewComponent hoạt động đúng
2. **Cart icon count**: Cần verify CartIconViewComponent lấy số lượng từ session
3. **Chat widget**: Gemini AI integration cần API key trong appsettings

---

## Ghi chú cho developer

- Nếu Bootstrap CSS không load → Check _Layout.cshtml có dòng Bootstrap CDN
- Nếu site.css không load → Check wwwroot/css/site.css tồn tại
- Nếu ViewComponent lỗi → Check dependency injection trong Program.cs
- Nếu Session lỗi → Check session middleware trong Program.cs

---

## Sau khi test xong

Báo cáo kết quả theo format:

**✅ Hoạt động tốt:**
- (liệt kê các trang/chức năng OK)

**⚠️ Cần fix:**
- (liệt kê vấn đề UI cụ thể với screenshot nếu có)

**❌ Lỗi nghiêm trọng:**
- (liệt kê lỗi blocking)