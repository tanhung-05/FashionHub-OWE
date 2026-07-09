# Hướng dẫn: Fix Image Paths cho ASP.NET Core

## Vấn đề
Ảnh sản phẩm không hiển thị vì database lưu đường dẫn theo format ASP.NET MVC cũ (`/Content/Images/...`) không tương thích với ASP.NET Core (`/images/...`).

## Giải pháp
Chạy SQL script `docs/fix-image-paths.sql` để update tất cả đường dẫn ảnh trong database.

---

## Các bước thực hiện

### Bước 1: Mở SQL Server Management Studio (SSMS)

1. Mở **SQL Server Management Studio**
2. Connect tới server: `DESKTOP-EFO8BQK` (hoặc server name của bạn)
3. Database: `QL_SHOPQUANAO_PRO`

### Bước 2: Mở SQL Script

1. Trong SSMS, click **File > Open > File...**
2. Browse tới project folder và mở:
   ```
   e:\NĂM 3\CNPM\Fasssshionnnnnn\docs\fix-image-paths.sql
   ```
3. Hoặc copy toàn bộ nội dung file SQL và paste vào SSMS Query Window

### Bước 3: Chạy Script

1. Đảm bảo đang kết nối đúng database `QL_SHOPQUANAO_PRO`
2. Click **Execute** (hoặc nhấn F5)
3. Script sẽ tự động:
   - ✅ Backup data vào table `HinhAnh_Backup_20260709`
   - ✅ Update tất cả đường dẫn ảnh
   - ✅ Verify kết quả
   - ✅ Hiển thị Before/After comparison

### Bước 4: Kiểm tra kết quả

Sau khi script chạy xong, kiểm tra messages tab trong SSMS:

```
=== STEP 1: Inspecting current image paths ===
[Hiển thị format path hiện tại]

=== STEP 2: Creating backup table ===
Backup created: HinhAnh_Backup_20260709
[Số lượng records đã backup]

=== STEP 3: Updating image paths ===
Updated X records... (cho mỗi pattern)

=== STEP 4: Verification ===
[Hiển thị format mới và comparison]

Migration Complete!
```

### Bước 5: Refresh Browser

1. Quay lại browser
2. Nhấn **Ctrl + Shift + R** (Windows) hoặc **Cmd + Shift + R** (Mac) để force refresh
3. Ảnh sản phẩm sẽ hiển thị

---

## Troubleshooting

### Nếu ảnh vẫn không hiển thị:

#### 1. Kiểm tra đường dẫn đã update chưa

Chạy query trong SSMS:

```sql
USE QL_SHOPQUANAO_PRO;

-- Xem 10 đường dẫn mới
SELECT TOP 10 DuongDan FROM HinhAnh;

-- Kiểm tra còn path cũ không
SELECT COUNT(*) 
FROM HinhAnh 
WHERE DuongDan LIKE '%Content/Images%';
-- Kết quả phải là 0
```

#### 2. Kiểm tra file ảnh có tồn tại không

Đường dẫn ảnh trong database (ví dụ `/images/products/ao-thun-1.jpg`) cần có file thực tế tại:

```
FashionHub2/FashionHub.Web/wwwroot/images/products/ao-thun-1.jpg
```

Nếu chưa có folder images, copy từ project cũ:

**Windows Command Prompt:**
```cmd
xcopy FashionHub\Content\Images FashionHub2\FashionHub.Web\wwwroot\images /E /I /Y
```

**PowerShell:**
```powershell
Copy-Item -Path "FashionHub\Content\Images\*" -Destination "FashionHub2\FashionHub.Web\wwwroot\images" -Recurse -Force
```

#### 3. Kiểm tra browser console

1. Mở browser DevTools (F12)
2. Tab **Console**
3. Refresh trang
4. Xem có lỗi 404 cho ảnh không

Nếu thấy:
```
GET http://localhost:5197/images/products/ao-thun-1.jpg 404 (Not Found)
```

→ File ảnh chưa tồn tại trong `wwwroot/images/`

---

## Rollback (nếu cần)

Nếu muốn quay lại đường dẫn cũ:

```sql
USE QL_SHOPQUANAO_PRO;

UPDATE HinhAnh 
SET DuongDan = b.DuongDan
FROM HinhAnh h 
INNER JOIN HinhAnh_Backup_20260709 b ON h.IDHinhAnh = b.IDHinhAnh;
```

---

## Summary

**Trước update:**
```
/Content/Images/products/ao-thun-1.jpg
~/Content/Images/products/ao-polo-2.jpg
\Content\Images\products\quan-jean-3.jpg
```

**Sau update:**
```
/images/products/ao-thun-1.jpg
/images/products/ao-polo-2.jpg
/images/products/quan-jean-3.jpg
```

**File location:**
```
FashionHub2/FashionHub.Web/wwwroot/images/products/ao-thun-1.jpg
FashionHub2/FashionHub.Web/wwwroot/images/products/ao-polo-2.jpg
FashionHub2/FashionHub.Web/wwwroot/images/products/quan-jean-3.jpg
```

---

## Notes

- ✅ Script tự động backup data trước khi update
- ✅ Script handle nhiều format path khác nhau
- ✅ Có thể rollback bất cứ lúc nào
- ✅ Safe to run multiple times (idempotent)
- ✅ Server vẫn chạy trong khi update database (hot update)

Nếu có vấn đề, check file log hoặc hỏi lại.