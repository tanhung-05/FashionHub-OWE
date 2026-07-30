# 🎯 HANDOFF PROMPT - Database Standardization Sprint

> **Copy everything below this line and paste into a new AI session.**

---

## TASK: Database Standardization Sprint - Analysis & Documentation

Tôi đang chuẩn hóa database cho **FashionHub** (ASP.NET Core MVC, .NET 10, EF Core database-first).

### Context đã có:
- ✅ Đã phân tích xong **17 Entity Models** (đọc từ FashionHub2/FashionHub.Web/Data/ApplicationDbContext.cs và các file trong FashionHub2/FashionHub.Web/Models/Generated/)
- ✅ Đã thu thập đầy đủ quyết định thiết kế

### Chi tiết decisions:

**1. XÓA:**
- VectorDacTrung field khỏi SanPham và HinhAnh (AI search đã disabled)

**2. THÊM 5 TABLES:**
1. YeuThich (Wishlist) - composite PK (IDNguoiDung, IDSanPham)
2. DanhGia (Reviews) - IDDanhGia identity PK, DiemSo 1-5, TrangThai BIT
3. LichSuTonKho (Inventory History) - IDLichSu identity PK, LoaiThayDoi NVARCHAR(50)
4. LichSuDonHang (Order Status History) - IDLichSu identity PK
5. AdminActivityLog (Admin Audit) - IDLog identity PK, DuLieuCu/Moi NVARCHAR(MAX) JSON

**3. THÊM COLUMNS:**
- SanPham: Slug NVARCHAR(255) UNIQUE, DeletedAt DATETIME2 NULL
- DanhMuc: Slug NVARCHAR(100) UNIQUE, DeletedAt DATETIME2 NULL
- BienTheSanPham: SoLuongCanhBao INT DEFAULT 10, TongDaBan INT DEFAULT 0, DeletedAt DATETIME2 NULL
- NguoiDung: DeletedAt DATETIME2 NULL
- ThuongHieu: DeletedAt DATETIME2 NULL
- MaGiamGia: DeletedAt DATETIME2 NULL

**4. SỬA:**
- TẤT CẢ money fields: DECIMAL(18,2) → DECIMAL(18,0) (VNĐ smallest unit, không decimal)

**5. STRATEGY:**
- Zero Downtime Migration (4 phases: EXPAND → BACKFILL → SWITCH → CONTRACT)

---

### TASK CHÍNH:

Tạo file **docs/database-review.md** với cấu trúc:

\\\markdown
# Database Standardization Review

## 1. Current State Analysis
- ERD hiện tại (mô tả text-based vì không có tool vẽ)
- Điểm mạnh
- Điểm yếu
- Thiếu sót (gaps)

## 2. Suggested Database Schema
- Remove columns (VectorDacTrung)
- Modify types (money fields DECIMAL changes)
- Add columns (Slug, DeletedAt, stock columns)
- 5 new tables với SQL DDL đầy đủ

## 3. Migration Plan (Zero Downtime - 4 phases)
- Phase 1: EXPAND (add nullable columns, create tables)
- Phase 2: BACKFILL (populate slugs, stock data)
- Phase 3: SWITCH (app code reads from new schema)
- Phase 4: CONTRACT (remove old columns, change DECIMAL)
- Mỗi phase có SQL script chi tiết + validation queries

## 4. Risks & Mitigation
- Chi tiết từng risk
- Probability, Impact
- Mitigation steps
- Rollback plan

## 5. Appendix
- Complete DDL scripts
- Validation queries
- Backfill scripts
\\\

### YÊU CẦU BẮT BUỘC:

1. **KHÔNG code C#**, KHÔNG sửa files, KHÔNG tạo migration
2. **CHỈ viết tài liệu phân tích** (markdown)
3. **Phải có SQL DDL scripts** cho tất cả changes
4. **Phải có validation queries** cho mỗi phase
5. **Phải có rollback plan** cho mỗi risk
6. Dựa trên **thực tế** từ ApplicationDbContext.cs đã đọc

### Files đã phân tích:

**ApplicationDbContext.cs** (FashionHub2/FashionHub.Web/Data/ApplicationDbContext.cs):
- 17 DbSet properties
- OnModelCreating với Fluent API configuration
- Foreign keys, indexes, unique constraints

**17 Entity Models** (FashionHub2/FashionHub.Web/Models/Generated/):
- SanPham.cs - Products (có VectorDacTrung cần xóa)
- BienTheSanPham.cs - Product Variants
- DanhMuc.cs - Categories (recursive self-reference)
- DonHang.cs - Orders
- ChiTietDonHang.cs - Order Items
- NguoiDung.cs - Users
- GioHang.cs - Cart (composite PK)
- HinhAnh.cs - Images (có VectorDacTrung cần xóa)
- HinhAnhBienThe.cs - Image-Variant mapping
- KichThuoc.cs - Sizes
- MauSac.cs - Colors
- MaGiamGium.cs - Coupons
- DiaChi.cs - Addresses
- PhuongThucThanhToan.cs - Payment Methods
- ThuongHieu.cs - Brands
- TrangThaiDonHang.cs - Order Status (seeded)
- VaiTro.cs - Roles

### Current schema summary (từ ApplicationDbContext.cs):

**Tables hiện tại (17):**
- VaiTro (IDVaiTro PK)
- NguoiDung (IDNguoiDung PK, UQ_Email, IX_SoDienThoai)
- SanPham (IDSanPham PK, FK_IDDanhMuc, FK_IDThuongHieu)
- BienTheSanPham (IDBienThe PK, UQ_SKU, FK_IDSanPham, FK_IDMauSac, FK_IDKichThuoc)
- DanhMuc (IDDanhMuc PK, self-ref FK_IDDanhMucCha)
- ThuongHieu (IDThuongHieu PK)
- MauSac (IDMauSac PK)
- KichThuoc (IDKichThuoc PK)
- HinhAnh (IDHinhAnh PK)
- HinhAnh_BienThe (composite PK: IDHinhAnh, IDBienThe)
- DonHang (IDDonHang PK, multiple FKs)
- ChiTietDonHang (IDChiTietDonHang PK, UQ_DonHang_BienThe)
- TrangThaiDonHang (IDTrangThai PK, ValueGeneratedNever)
- PhuongThucThanhToan (IDPhuongThucThanhToan PK)
- MaGiamGia (IDMaGiamGia PK, UQ_MaCode)
- DiaChi (IDDiaChi PK)
- GioHang (composite PK: IDNguoiDung, IDBienThe)

**Money fields (DECIMAL(18,2)):**
- BienTheSanPham.Gia
- ChiTietDonHang.DonGia
- DonHang.PhiVanChuyen, TienGiamGia, TongThanhToan, TongTienHang
- MaGiamGia.DonHangToiThieu, GiaTri, GiamToiDa
- SanPham.Gia, GiaKhuyenMai

### Output format:

File: \docs/database-review.md\
- Markdown với cấu trúc rõ ràng
- SQL scripts trong code blocks với syntax highlighting
- Validation queries trong mỗi phase
- Rollback plans chi tiết

### Bắt đầu ngay:

Đọc \FashionHub2/FashionHub.Web/Data/ApplicationDbContext.cs\ để hiểu schema hiện tại, sau đó tạo \docs/database-review.md\ theo cấu trúc đã nêu.
