# Project Brief — FashionHub / OWE

## Tổng quan
FashionHub (OWE) là website bán thời trang/quần áo, UI chính bằng tiếng Việt, hướng đến phong cách tối giản, sạch và phù hợp cho demo e-commerce thời trang.

Project hiện tại đang ở giai đoạn chuyển hướng kỹ thuật: thay vì tiếp tục tối ưu UI/UX trực tiếp trên ASP.NET MVC 5 (.NET Framework 4.8), ưu tiên mới là migrate sang ASP.NET Core MVC trên .NET 10 bằng project mới `FashionHub2/FashionHub.Web`. Project cũ `FashionHub/` được giữ lại để tham chiếu cho tới khi migration hoàn tất.

## Mục tiêu dự án
Hoàn thiện project thành một sản phẩm demo chỉn chu để đưa vào CV xin thực tập, thể hiện được khả năng:
- Phân tích và migrate hệ thống legacy từ ASP.NET MVC 5 sang ASP.NET Core MVC.
- Giữ nguyên domain nghiệp vụ e-commerce thời trang khi đổi nền tảng kỹ thuật.
- Tổ chức code rõ ràng theo Controllers, Services, Models/ViewModels, Views.
- Kết nối database SQL Server bằng EF Core sau migration.
- Dựng Authentication bằng Cookie Authentication, giữ tương thích BCrypt.Net-Next nếu dữ liệu mật khẩu cũ đang dùng.
- Tối ưu UI/UX sau khi nền tảng .NET 10 ổn định.
- Viết test, Dockerize và chuẩn bị deploy online.

## Stack hiện tại
- ASP.NET MVC 5.3.0
- .NET Framework 4.8
- Entity Framework 6.5.1
- Razor Views
- Bootstrap 5.3.x
- jQuery 3.7.x
- SQL Server

## Stack đích
- ASP.NET Core MVC trên .NET 10
- EF Core SQL Server
- Cookie Authentication
- BCrypt.Net-Next
- X.PagedList.Mvc.Core
- xUnit cho testing
- Docker / docker-compose
- Static files trong `wwwroot/`

## Phạm vi chức năng chính cần giữ khi migrate
- Hiển thị danh sách sản phẩm thời trang.
- Xem chi tiết sản phẩm.
- Phân loại theo danh mục, thương hiệu, giá.
- Biến thể sản phẩm theo màu sắc/kích thước/tồn kho.
- Giỏ hàng.
- Thanh toán/đặt hàng.
- Quản lý tài khoản, địa chỉ.
- Quản lý đơn hàng.
- Giao diện admin hoặc khu vực quản trị.
- Chat/hỗ trợ khách hàng nếu tiếp tục hoàn thiện.

## Ưu tiên phát triển hiện tại
1. Cập nhật tài liệu, `.clinerules` và Memory Bank theo hướng migrate .NET 10.
2. Dựng project mới `FashionHub2/FashionHub.Web` và `FashionHub2/FashionHub.Tests`.
3. Scaffold EF Core model từ database SQL Server hiện có.
4. Chuyển Authentication, Services, Controllers, Views theo từng nhóm chức năng.
5. Sau khi migration ổn định mới áp dụng lại roadmap UI/UX trên code mới.
6. Viết unit test, Dockerize và deploy.

## Định hướng chất lượng
- Không xoá project cũ cho tới khi migration hoàn tất.
- Không đổi schema/domain nghiệp vụ nếu chưa có yêu cầu rõ ràng.
- Mọi code mới cho project migrate phải theo convention ASP.NET Core MVC/.NET 10.
- Giữ UI tiếng Việt.
- Giữ phong cách OWE tối giản, sạch, hiện đại.
- Ưu tiên code dễ đọc, dễ bảo trì hơn tối ưu phức tạp.
- Mỗi thay đổi nên nhỏ, rõ mục tiêu và có thể commit riêng.