# Hướng Dẫn Fix Đường Dẫn Ảnh

## Vấn Đề

Sau khi migrate từ .NET Framework sang .NET Core, đường dẫn ảnh trong database vẫn giữ định dạng cũ:
- `~/Content/images/aothun1.jpg` 
- `/Content/images/aothun1.jpg`

Nhưng .NET Core project sử dụng `wwwroot/` làm thư mục static files, nên đường dẫn đúng phải là:
- `/images/aothun1.jpg`

## Cách Fix

### 1. Mở SQL Server Management Studio (SSMS)

### 2. Kết nối đến database FashionHub

### 3. Mở file `docs/fix-image-paths.sql`

### 4. Chạy script (Execute hoặc F5)

Script sẽ:
- Update tất cả đường dẫn `~/Content/images/` → `/images/`
- Update tất cả đường dẫn `/Content/images/` → `/images/`
- Update tất cả đường dẫn `Content/images/` → `/images/`
- Hiển thị số lượng rows đã update
- Verify kết quả

### 5. Kiểm tra kết quả

Sau khi chạy script, bạn sẽ thấy output như:
```
✓ Updated X rows with ~/Content/images/ prefix
✓ Updated X rows with /Content/images/ prefix
✓ Updated X rows with Content/images/ prefix (no leading char)

Verifying updated paths...
TotalImages | CorrectPaths | StillHasContentPath
------------|--------------|--------------------
50          | 50           | 0

✅ Image path migration complete!
```

### 6. Test lại website

Refresh trang Products - ảnh sản phẩm sẽ hiển thị bình thường.

## Lưu Ý

- Ảnh vật lý phải nằm trong `FashionHub2/FashionHub.Web/wwwroot/images/`
- Script này chỉ update đường dẫn trong database, không copy/move files
- Nếu vẫn không load ảnh, kiểm tra:
  1. File ảnh có tồn tại trong `wwwroot/images/` không
  2. Tên file trong database có khớp với tên file thực tế không (case-sensitive trên Linux)
  3. Console browser có báo lỗi 404 không

## Rollback (nếu cần)

Nếu muốn rollback về đường dẫn cũ:
```sql
UPDATE HinhAnhSanPham
SET DuongDan = REPLACE(DuongDan, '/images/', '~/Content/images/')
WHERE DuongDan LIKE '/images/%';