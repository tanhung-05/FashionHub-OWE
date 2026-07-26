-- FashionHub Production Database Indexes
-- Execute this script before production deployment for optimal performance
-- Review Date: 2026-07-26

USE [FashionHub];
GO

-- Check if indexes exist before creating
PRINT 'Creating performance indexes for FashionHub...';
GO

-- Products table - Filter by active status
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SanPham_TrangThai' AND object_id = OBJECT_ID('SanPham'))
BEGIN
    CREATE INDEX IX_SanPham_TrangThai ON SanPham(TrangThai)
    INCLUDE (IdsanPham, TenSanPham, Gia, Slug);
    PRINT '✓ Created index: IX_SanPham_TrangThai';
END
ELSE
    PRINT '- Index IX_SanPham_TrangThai already exists';
GO

-- Orders table - Filter by creation date (for reports and order history)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DonHang_NgayTao' AND object_id = OBJECT_ID('DonHang'))
BEGIN
    CREATE INDEX IX_DonHang_NgayTao ON DonHang(NgayTao DESC)
    INCLUDE (IddonHang, IdnguoiDung, TongThanhToan, IdtrangThai);
    PRINT '✓ Created index: IX_DonHang_NgayTao';
END
ELSE
    PRINT '- Index IX_DonHang_NgayTao already exists';
GO

-- Orders table - Filter by status
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DonHang_IDTrangThai' AND object_id = OBJECT_ID('DonHang'))
BEGIN
    CREATE INDEX IX_DonHang_IDTrangThai ON DonHang(IdtrangThai)
    INCLUDE (IddonHang, NgayTao, TongThanhToan);
    PRINT '✓ Created index: IX_DonHang_IDTrangThai';
END
ELSE
    PRINT '- Index IX_DonHang_IDTrangThai already exists';
GO

-- Orders table - User's orders lookup
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DonHang_IDNguoiDung_NgayTao' AND object_id = OBJECT_ID('DonHang'))
BEGIN
    CREATE INDEX IX_DonHang_IDNguoiDung_NgayTao ON DonHang(IdnguoiDung, NgayTao DESC)
    INCLUDE (IddonHang, TongThanhToan, IdtrangThai);
    PRINT '✓ Created index: IX_DonHang_IDNguoiDung_NgayTao';
END
ELSE
    PRINT '- Index IX_DonHang_IDNguoiDung_NgayTao already exists';
GO

-- Order details - Join with orders
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChiTietDonHang_IDDonHang' AND object_id = OBJECT_ID('ChiTietDonHang'))
BEGIN
    CREATE INDEX IX_ChiTietDonHang_IDDonHang ON ChiTietDonHang(IddonHang)
    INCLUDE (IdbienThe, SoLuong, Gia);
    PRINT '✓ Created index: IX_ChiTietDonHang_IDDonHang';
END
ELSE
    PRINT '- Index IX_ChiTietDonHang_IDDonHang already exists';
GO

-- Product variants - Join with products
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BienThe_IDSanPham' AND object_id = OBJECT_ID('BienThe'))
BEGIN
    CREATE INDEX IX_BienThe_IDSanPham ON BienThe(IdsanPham)
    INCLUDE (IdmauSac, IdkichThuoc, SoLuongTon, GiaBan);
    PRINT '✓ Created index: IX_BienThe_IDSanPham';
END
ELSE
    PRINT '- Index IX_BienThe_IDSanPham already exists';
GO

-- Product images - Join with products
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HinhAnhSanPham_IDSanPham' AND object_id = OBJECT_ID('HinhAnhSanPham'))
BEGIN
    CREATE INDEX IX_HinhAnhSanPham_IDSanPham ON HinhAnhSanPham(IdsanPham)
    INCLUDE (DuongDan, LaChinh);
    PRINT '✓ Created index: IX_HinhAnhSanPham_IDSanPham';
END
ELSE
    PRINT '- Index IX_HinhAnhSanPham_IDSanPham already exists';
GO

-- Products - Category filter
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SanPham_IDDanhMuc_TrangThai' AND object_id = OBJECT_ID('SanPham'))
BEGIN
    CREATE INDEX IX_SanPham_IDDanhMuc_TrangThai ON SanPham(IddanhMuc, TrangThai)
    INCLUDE (IdsanPham, TenSanPham, Gia, Slug);
    PRINT '✓ Created index: IX_SanPham_IDDanhMuc_TrangThai';
END
ELSE
    PRINT '- Index IX_SanPham_IDDanhMuc_TrangThai already exists';
GO

-- Products - Brand filter
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SanPham_IDThuongHieu_TrangThai' AND object_id = OBJECT_ID('SanPham'))
BEGIN
    CREATE INDEX IX_SanPham_IDThuongHieu_TrangThai ON SanPham(IdthuongHieu, TrangThai)
    INCLUDE (IdsanPham, TenSanPham, Gia, Slug);
    PRINT '✓ Created index: IX_SanPham_IDThuongHieu_TrangThai';
END
ELSE
    PRINT '- Index IX_SanPham_IDThuongHieu_TrangThai already exists';
GO

-- User addresses - Lookup by user
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DiaChi_IDNguoiDung' AND object_id = OBJECT_ID('DiaChi'))
BEGIN
    CREATE INDEX IX_DiaChi_IDNguoiDung ON DiaChi(IdnguoiDung)
    INCLUDE (DiaChi1, ThanhPho, QuanHuyen, PhuongXa, MacDinh);
    PRINT '✓ Created index: IX_DiaChi_IDNguoiDung';
END
ELSE
    PRINT '- Index IX_DiaChi_IDNguoiDung already exists';
GO

-- Cart items - Lookup by user
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChiTietGioHang_IDGioHang' AND object_id = OBJECT_ID('ChiTietGioHang'))
BEGIN
    CREATE INDEX IX_ChiTietGioHang_IDGioHang ON ChiTietGioHang(IdgioHang)
    INCLUDE (IdbienThe, SoLuong);
    PRINT '✓ Created index: IX_ChiTietGioHang_IDGioHang';
END
ELSE
    PRINT '- Index IX_ChiTietGioHang_IDGioHang already exists';
GO

-- Coupons - Filter by active status and date
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MaGiamGia_TrangThai_NgayBatDau_NgayKetThuc' AND object_id = OBJECT_ID('MaGiamGia'))
BEGIN
    CREATE INDEX IX_MaGiamGia_TrangThai_NgayBatDau_NgayKetThuc ON MaGiamGia(TrangThai, NgayBatDau, NgayKetThuc)
    INCLUDE (IdmaGiamGia, Code, PhanTramGiam, GiaTriGiamToiDa);
    PRINT '✓ Created index: IX_MaGiamGia_TrangThai_NgayBatDau_NgayKetThuc';
END
ELSE
    PRINT '- Index IX_MaGiamGia_TrangThai_NgayBatDau_NgayKetThuc already exists';
GO

PRINT '';
PRINT 'Index creation complete!';
PRINT 'Run the following to verify indexes:';
PRINT 'SELECT t.name AS TableName, i.name AS IndexName, i.type_desc';
PRINT 'FROM sys.indexes i';
PRINT 'INNER JOIN sys.tables t ON i.object_id = t.object_id';
PRINT 'WHERE t.name IN (''SanPham'', ''DonHang'', ''ChiTietDonHang'', ''BienThe'', ''HinhAnhSanPham'', ''DiaChi'', ''ChiTietGioHang'', ''MaGiamGia'')';
PRINT 'ORDER BY t.name, i.name;';
GO