---
description: UI/UX rules for Razor views and CSS
globs:
  - "FashionHub/Views/**"
  - "FashionHub/Content/**"
---

# UI/UX Guidelines

## Phạm vi
- Áp dụng cho `FashionHub/Views/**` và `FashionHub/Content/**`.
- Ưu tiên giữ UI tiếng Việt, phù hợp website thời trang OWE/FashionHub.

## Design tokens
- Luôn dùng design token trong `:root` của `FashionHub/Content/site.css`.
- Token chính:
  - `--owe-black`
  - `--owe-ink`
  - `--owe-muted`
  - `--owe-soft`
  - `--owe-surface`
  - `--owe-border`
  - `--owe-sale`
  - `--owe-radius-sm`, `--owe-radius-md`, `--owe-radius-lg`
  - `--owe-shadow-sm`, `--owe-shadow-md`
- Không hard-code màu nếu đã có token tương ứng.
- Giữ palette tối giản: đen, trắng, nền soft, đỏ sale.

## Components
- Ưu tiên Bootstrap 5.3 utility/class có sẵn kết hợp custom class trong `site.css`.
- Card, button, badge, form, modal, offcanvas phải có style nhất quán.
- Không đặt `<style>` trong partial view.
- Không dùng inline style trong Razor view; chuyển sang class CSS.

## Feedback UI
- Mọi thông báo người dùng phải dùng hệ thống toast trong `_GlobalFeedbackPartial`.
- Không dùng `alert()` hoặc `confirm()` của trình duyệt.
- Với xác nhận xóa/hủy, dùng modal hoặc UI confirmation tùy chỉnh.

## Responsive
- Thiết kế mobile-first cho product grid, cart, checkout, filter, footer.
- Tránh table khó responsive cho luồng mua hàng trên mobile.
- Filter/sidebar nên có phương án offcanvas hoặc collapse trên màn hình nhỏ.

## Accessibility
- Button chỉ có icon phải có `aria-label`.
- Ảnh sản phẩm phải có `alt` mô tả.
- Form control cần label rõ ràng và thông báo lỗi dễ hiểu.
- Không chỉ dùng màu để truyền đạt trạng thái.