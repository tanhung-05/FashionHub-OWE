# Deployment cho người mới

Tài liệu này giải thích deployment theo đúng kiến trúc hiện tại của FashionHub.

## 1. Deployment là gì?

Khi chạy bằng Visual Studio, web server và SQL Server đang nằm trên máy cá nhân.
Chỉ máy của bạn truy cập ổn định được và ứng dụng dừng khi bạn tắt máy.

Deployment là đưa các thành phần đó lên hạ tầng luôn hoạt động và có địa chỉ công
khai để người dùng truy cập:

```text
Trình duyệt người dùng
        |
        | HTTPS: https://your-domain.example
        v
ASP.NET Core trên server
        |
        | kết nối nội bộ, không public
        v
SQL Server + nơi lưu ảnh sản phẩm
```

Người dùng không kết nối trực tiếp vào SQL Server. Chỉ ASP.NET Core biết chuỗi
kết nối và truy cập database.

## 2. SQL có phải để trên máy cá nhân không?

Không. Khi chạy thật, bạn thường chọn một trong hai cách:

- Managed database: Azure SQL hoặc dịch vụ tương đương quản lý backup, cập nhật
  và tính sẵn sàng giúp bạn. Cách này dễ vận hành hơn nhưng thường có chi phí.
- SQL Server container trên VPS: tiết kiệm và phù hợp để học Docker, nhưng bạn tự
  chịu trách nhiệm backup, cập nhật, bảo mật và khôi phục khi có sự cố.

Máy cá nhân chỉ giữ database phát triển. Production có database riêng và dữ liệu
thật không được tải tùy tiện về máy cá nhân.

## 3. Mô hình phù hợp với FashionHub

Lộ trình học nên đi theo ba bước:

1. Chạy Docker Compose trên máy cá nhân để hiểu web, SQL, network và volume.
2. Đưa cùng bộ container lên một VPS thử nghiệm có domain và HTTPS.
3. Khi đã quen, chuyển SQL sang managed database và thiết lập CI/CD.

Compose hiện tại tạo ba service:

- `sqlserver`: chạy SQL Server và lưu dữ liệu trong volume `sqlserver_data`.
- `db-init`: chạy `DB_Fixed.sql` duy nhất khi database chưa tồn tại.
- `web`: chạy FashionHub, kết nối SQL qua Docker network và lưu ảnh upload trong
  volume `product_images`.

`DB_Fixed.sql` là script dựng database mới và có thao tác xóa bảng. Không chạy
script này để cập nhật production đang có dữ liệu. Mọi thay đổi schema sau này
phải có script nâng cấp riêng, idempotent và không xóa dữ liệu.

## 4. Chạy Docker trên máy cá nhân

### Chuẩn bị

1. Cài Docker Desktop và bật Linux containers.
2. Mở PowerShell tại thư mục repository.
3. Tạo file cấu hình riêng:

```powershell
Copy-Item FashionHub2/.env.example FashionHub2/.env
notepad FashionHub2/.env
```

Điền mật khẩu SQL mạnh, Gemini key, SMTP và `PUBLIC_BASE_URL`. File `.env` đã bị
Git ignore và không được commit.

### Khởi động

```powershell
cd FashionHub2
docker compose up -d --build
docker compose ps
docker compose logs -f web
```

Mở `http://localhost:5167`. Khi cần dừng:

```powershell
docker compose down
```

Lệnh trên xóa container nhưng giữ volume. Không chạy `docker compose down -v`
trừ khi bạn chủ động muốn xóa toàn bộ database và ảnh upload.

## 5. Đưa lên Internet

Một lần triển khai VPS cơ bản gồm:

1. Thuê VPS Linux có IP công khai và cài Docker Engine.
2. Clone repository hoặc để pipeline chuyển image lên server.
3. Tạo `.env` trực tiếp trên server; không gửi file secret lên GitHub.
4. Chạy Compose và chỉ cho web/reverse proxy truy cập SQL.
5. Trỏ domain về IP của VPS.
6. Đặt Caddy, nginx hoặc dịch vụ proxy phía trước để cấp HTTPS.
7. Chạy smoke test: trang chủ, đăng nhập, giỏ hàng, đặt hàng và `/health`.

Máy cá nhân có thể tắt sau đó. VPS tiếp tục phục vụ người dùng.

## 6. Quản lý phần mềm sau deployment

Deployment không kết thúc ở lần đưa code đầu tiên. Người vận hành cần:

- Logs: xem lỗi bằng `docker compose logs` và không ghi password/token vào log.
- Health: giám sát `/health` và cảnh báo khi web hoặc database lỗi.
- Backup: backup SQL tự động, lưu ở nơi khác server và thử restore định kỳ.
- Images: backup volume ảnh hoặc chuyển ảnh sang object storage khi mở rộng.
- Updates: test trên môi trường staging trước, backup rồi mới cập nhật production.
- Rollback: giữ image/version cũ để quay lại khi bản mới có lỗi.
- Security: HTTPS, firewall, cập nhật image, xoay secret và không public cổng SQL.

## 7. CI khác deployment như thế nào?

Workflow `.github/workflows/ci.yml` hiện tự restore, build và test khi push hoặc
tạo pull request vào `main`. Đó là CI: kiểm tra chất lượng code.

CD là bước tiếp theo: sau khi CI qua, build Docker image, đẩy image lên registry
và cập nhật server. Chưa nên tự động deploy production cho đến khi có staging,
backup và rollback đã được kiểm chứng.

## 8. Checklist trước lần deploy thật

- Release build và toàn bộ test đều qua.
- Docker Compose đã chạy thử thành công trên máy có Docker.
- Secret production được đặt ngoài Git.
- Domain và HTTPS hoạt động.
- SQL và ảnh có backup; đã thử restore.
- Database update script không làm mất dữ liệu.
- SMTP, Gemini, login, checkout và admin đã smoke test.
- Có cách xem log, health và rollback.

Tài liệu chính thức tham khảo:

- [ASP.NET Core Docker](https://learn.microsoft.com/aspnet/core/host-and-deploy/docker/)
- [SQL Server containers](https://learn.microsoft.com/sql/linux/containers/deploy)
- [Persist SQL Server container data](https://learn.microsoft.com/sql/linux/containers/configure)
