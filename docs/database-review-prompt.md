# Prompt để tiếp tục Database Review Task

## Context
Tôi đang làm Sprint chuẩn hóa Database cho project FashionHub (ASP.NET Core MVC, .NET 10, EF Core database-first).

## Task đã giao
Phân tích toàn bộ database hiện tại và tạo tài liệu **database-review.md** bao gồm:
1. Current ERD
2. Analysis (điểm mạnh, yếu, thiếu sót)
3. Suggested ERD (chuẩn hóa)
4. Migration Plan
5. Risks

**Quan trọng:** Chỉ phân tích, KHÔNG code, KHÔNG sửa, KHÔNG tạo migration.

## Tiến độ đã hoàn thành

### ✅ Phân tích xong:
- **17 Entity Models** đã đọc (SanPham, BienTheSanPham, DonHang, NguoiDung, GioHang, HinhAnh, etc.)
- **ApplicationDbContext** với relationships, constraints, indexes
- **DB_Fixed.sql** (schema gốc)
- **database-indexes-production.sql** (indexes recommendations)

### ✅ Quyết định thiết kế đã thu thập:

**Xóa bỏ:**
- VectorDacTrung field (SanPham, HinhAnh) - AI search đã disabled

**Thêm mới - Tables:**
1. YeuThich (Wishlist) - IDYeuThich, IDNguoiDung, IDSanPham, NgayThem
2. DanhGia (Reviews) - IDDanhGia, IDSanPham, IDNguoiDung, DiemDanhGia (1-5), BinhLuan, NgayTao
3. LichSuTonKho (Inventory History) - IDLichSu, IDBienThe, LoaiThayDoi, SoLuongThayDoi, SoLuongSau, LyDo, NguoiThucHien, NgayTao
4. LichSuDonHang (Order Status History) - IDLichSu, IDDonHang, TrangThaiCu, TrangThaiMoi, NguoiThucHien, GhiChu, NgayThayDoi
5. AdminActivityLog (Admin Audit) - IDLog, IDAdmin, HanhDong, BangTable, IDBanGhi, NoiDung, IPAddress, NgayTao

**Thêm mới - Columns:**
- SanPham: Slug (VARCHAR 255 UNIQUE), DeletedAt (DATETIME NULL)
- DanhMuc: Slug (VARCHAR 255 UNIQUE), DeletedAt (DATETIME NULL)
- BienTheSanPham: SoLuongCanhBao (INT DEFAULT 10), TongDaBan (INT DEFAULT 0), DeletedAt (DATETIME NULL)
- NguoiDung: DeletedAt (DATETIME NULL)
- MaGiamGia: DeletedAt (DATETIME NULL)

**Modify - Data Types:**
- TẤT CẢ money fields: DECIMAL(18,2) → DECIMAL(18,0) (VNĐ only, không decimal)

**Migration Strategy:**
- Zero Downtime Migration (4 phases: Add → Deploy dual support → Migrate data → Cleanup)

---

## PROMPT ĐỂ PASTE VÀO SESSION MỚI:

TASK: Database Standardization Sprint - Analysis & Documentation

Tôi đang chuẩn hóa database cho FashionHub (ASP.NET Core MVC, .NET 10, EF Core database-first).

Context đã có:
- Đã phân tích xong 17 Entity Models
- Đã thu thập đầy đủ quyết định thiết kế

Chi tiết decisions:
1. XÓA: VectorDacTrung field
2. THÊM 5 TABLES: YeuThich, DanhGia, LichSuTonKho, LichSuDonHang, AdminActivityLog
3. THÊM COLUMNS: Slug (SanPham, DanhMuc), DeletedAt (5 tables), SoLuongCanhBao, TongDaBan
4. SỬA: Money fields DECIMAL(18,2) → (18,0)
5. STRATEGY: Zero Downtime Migration (4 phases)

TASK:
Tạo file docs/database-review.md với:
1. Current State Analysis (ERD hiện tại, điểm mạnh/yếu, thiếu sót)
2. Suggested Schema (5 tables mới + columns mới + modify types với SQL DDL)
3. Migration Plan (Zero Downtime - 4 phases với SQL scripts chi tiết)
4. Risks & Mitigation (chi tiết từng risk với rollback plan)
5. Appendix (DDL scripts, validation queries)

KHÔNG code C#, KHÔNG sửa files, CHỈ viết tài liệu phân tích.

Đọc FashionHub2/FashionHub.Web/Data/ApplicationDbContext.cs để hiểu schema hiện tại.

Hãy bắt đầu viết tài liệu database-review.md với cấu trúc:
## 1. Current State Analysis
## 2. Suggested Database Schema  
## 3. Migration Plan (Zero Downtime)
## 4. Risks & Mitigation
## 5. Appendix
