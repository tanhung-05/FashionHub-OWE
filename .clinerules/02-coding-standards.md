# Coding Standards

## C# naming
- Public class, method, property, enum: dùng `PascalCase`.
- Local variable, parameter, private field đơn giản: dùng `camelCase`.
- Tên nên rõ nghĩa, ưu tiên tiếng Anh cho code và giữ thuật ngữ domain nhất quán.
- Async method nếu có nên kết thúc bằng `Async`.

## Controllers và Services
- Action method ngắn gọn, dễ đọc.
- Không lặp logic xử lý nghiệp vụ giữa nhiều controller.
- Logic dùng lại phải tách vào `Services/`.
- Validate dữ liệu đầu vào trước khi xử lý.

## Razor Views
- Không dùng inline style trong `.cshtml`.
- Không viết thẻ `<style>` trong partial view.
- Không viết business logic phức tạp trong Razor.
- Không query database hoặc gọi EF trực tiếp trong View.
- Ưu tiên dùng partial view cho component UI tái sử dụng.

## CSS
- Dùng CSS custom properties đã có trong `FashionHub/Content/site.css`.
- Ưu tiên token hiện có:
  - `--owe-black`
  - `--owe-ink`
  - `--owe-muted`
  - `--owe-soft`
  - `--owe-surface`
  - `--owe-border`
  - `--owe-sale`
- Không hard-code màu/kích thước nếu đã có token phù hợp.
- Giữ style tập trung trong `site.css`, tránh phân tán trong View.

## JavaScript
- JavaScript dùng chung đặt trong `FashionHub/Scripts/site.js`.
- Tránh lặp code AJAX/toast giữa nhiều View.
- Không dùng `alert()`/`confirm()` cho luồng UI chính nếu đã có toast/modal.