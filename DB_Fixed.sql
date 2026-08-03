SET NOCOUNT ON;
GO

IF DB_ID(N'QL_SHOPQUANAO_PRO') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE QL_SHOPQUANAO_PRO');
END
GO

USE QL_SHOPQUANAO_PRO;
GO

SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

BEGIN TRANSACTION;
GO

-- =================================================================
-- PHẦN 1: DỌN DẸP BẢNG CŨ (DROP TABLES THEO THỨ TỰ KHÓA NGOẠI)
-- =================================================================

-- Drop các bảng liên kết/phụ thuộc trước
IF OBJECT_ID('TinNhanChat', 'U') IS NOT NULL DROP TABLE TinNhanChat;
IF OBJECT_ID('CuocTroChuyen', 'U') IS NOT NULL DROP TABLE CuocTroChuyen;
IF OBJECT_ID('AdminActivityLog', 'U') IS NOT NULL DROP TABLE AdminActivityLog;
IF OBJECT_ID('DatLaiMatKhauToken', 'U') IS NOT NULL DROP TABLE DatLaiMatKhauToken;
IF OBJECT_ID('LichSuDonHang', 'U') IS NOT NULL DROP TABLE LichSuDonHang;
IF OBJECT_ID('LichSuTonKho', 'U') IS NOT NULL DROP TABLE LichSuTonKho;
IF OBJECT_ID('DanhGia', 'U') IS NOT NULL DROP TABLE DanhGia;
IF OBJECT_ID('YeuThich', 'U') IS NOT NULL DROP TABLE YeuThich;
IF OBJECT_ID('ChiTietDonHang', 'U') IS NOT NULL DROP TABLE ChiTietDonHang;
IF OBJECT_ID('GiaoDichThanhToan', 'U') IS NOT NULL DROP TABLE GiaoDichThanhToan;
IF OBJECT_ID('DonHang', 'U') IS NOT NULL DROP TABLE DonHang;
IF OBJECT_ID('GioHang', 'U') IS NOT NULL DROP TABLE GioHang;
IF OBJECT_ID('HinhAnh_BienThe', 'U') IS NOT NULL DROP TABLE HinhAnh_BienThe;
IF OBJECT_ID('HinhAnh', 'U') IS NOT NULL DROP TABLE HinhAnh;
IF OBJECT_ID('BienTheSanPham', 'U') IS NOT NULL DROP TABLE BienTheSanPham;
IF OBJECT_ID('SanPham', 'U') IS NOT NULL DROP TABLE SanPham;
IF OBJECT_ID('DiaChi', 'U') IS NOT NULL DROP TABLE DiaChi;
IF OBJECT_ID('NguoiDung', 'U') IS NOT NULL DROP TABLE NguoiDung;

-- Drop các bảng danh mục/tra cứu sau
IF OBJECT_ID('VaiTro', 'U') IS NOT NULL DROP TABLE VaiTro;
IF OBJECT_ID('TrangThaiDonHang', 'U') IS NOT NULL DROP TABLE TrangThaiDonHang;
IF OBJECT_ID('PhuongThucThanhToan', 'U') IS NOT NULL DROP TABLE PhuongThucThanhToan;
IF OBJECT_ID('DanhMuc', 'U') IS NOT NULL DROP TABLE DanhMuc;
IF OBJECT_ID('ThuongHieu', 'U') IS NOT NULL DROP TABLE ThuongHieu;
IF OBJECT_ID('MauSac', 'U') IS NOT NULL DROP TABLE MauSac;
IF OBJECT_ID('KichThuoc', 'U') IS NOT NULL DROP TABLE KichThuoc;
IF OBJECT_ID('MaGiamGia', 'U') IS NOT NULL DROP TABLE MaGiamGia;
GO

-- =================================================================
-- PHẦN 2: TẠO CẤU TRÚC BẢNG (SCHEMA) MỚI CHUẨN HÓA
-- =================================================================

-- 2.1. Các bảng tra cứu & danh mục
CREATE TABLE VaiTro (
    IDVaiTro INT NOT NULL PRIMARY KEY,
    TenVaiTro VARCHAR(50) NOT NULL,
    CONSTRAINT UQ_VaiTro_TenVaiTro UNIQUE (TenVaiTro)
);

CREATE TABLE TrangThaiDonHang (
    IDTrangThai INT NOT NULL PRIMARY KEY,
    TenTrangThai NVARCHAR(100) NOT NULL,
    CONSTRAINT UQ_TrangThaiDonHang_TenTrangThai UNIQUE (TenTrangThai)
);

CREATE TABLE PhuongThucThanhToan (
    IDPhuongThucThanhToan INT PRIMARY KEY IDENTITY(1,1),
    MaPhuongThuc VARCHAR(30) NOT NULL,
    TenPhuongThuc NVARCHAR(100) NOT NULL,
    TrangThai BIT NOT NULL CONSTRAINT DF_PhuongThucThanhToan_TrangThai DEFAULT 1,
    CONSTRAINT UQ_PhuongThucThanhToan_MaPhuongThuc UNIQUE (MaPhuongThuc),
    CONSTRAINT UQ_PhuongThucThanhToan_TenPhuongThuc UNIQUE (TenPhuongThuc)
);

CREATE TABLE DanhMuc (
    IDDanhMuc INT PRIMARY KEY IDENTITY(1,1),
    TenDanhMuc NVARCHAR(100) NOT NULL,
    Slug NVARCHAR(100) NULL,
    IDDanhMucCha INT NULL,
    ThuTuHienThi INT NOT NULL CONSTRAINT DF_DanhMuc_ThuTuHienThi DEFAULT 0,
    TrangThai BIT NOT NULL CONSTRAINT DF_DanhMuc_TrangThai DEFAULT 1,
    DeletedAt DATETIME2(0) NULL,
    CONSTRAINT FK_DanhMuc_DanhMucCha FOREIGN KEY (IDDanhMucCha)
        REFERENCES DanhMuc(IDDanhMuc)
);

CREATE TABLE ThuongHieu (
    IDThuongHieu INT PRIMARY KEY IDENTITY(1,1),
    TenThuongHieu NVARCHAR(100) NOT NULL,
    TrangThai BIT NOT NULL CONSTRAINT DF_ThuongHieu_TrangThai DEFAULT 1,
    DeletedAt DATETIME2(0) NULL,
    CONSTRAINT UQ_ThuongHieu_TenThuongHieu UNIQUE (TenThuongHieu)
);

CREATE TABLE MauSac (
    IDMauSac INT PRIMARY KEY IDENTITY(1,1),
    TenMau NVARCHAR(50) NOT NULL,
    MaMauHex VARCHAR(7) NULL,
    CONSTRAINT CK_MauSac_MaMauHex CHECK (
        MaMauHex IS NULL
        OR (
            LEN(MaMauHex) = 7
            AND LEFT(MaMauHex, 1) = '#'
            AND SUBSTRING(MaMauHex, 2, 6) NOT LIKE '%[^0-9A-Fa-f]%'
        )
    )
);

CREATE TABLE KichThuoc (
    IDKichThuoc INT PRIMARY KEY IDENTITY(1,1),
    TenKichThuoc VARCHAR(50) NOT NULL,
    CONSTRAINT UQ_KichThuoc_TenKichThuoc UNIQUE (TenKichThuoc)
);

-- 2.2. Người dùng & Địa chỉ
CREATE TABLE NguoiDung (
    IDNguoiDung INT PRIMARY KEY IDENTITY(1,1),
    HoTen NVARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    SoDienThoai VARCHAR(15) NULL,
    MatKhauHash VARCHAR(255) NOT NULL,
    SecurityStamp UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_NguoiDung_SecurityStamp DEFAULT NEWID(),
    IDVaiTro INT NOT NULL,
    NgayTao DATETIME2(0) NOT NULL CONSTRAINT DF_NguoiDung_NgayTao DEFAULT SYSDATETIME(),
    NgayCapNhat DATETIME2(0) NULL,
    TrangThai BIT NOT NULL CONSTRAINT DF_NguoiDung_TrangThai DEFAULT 1,
    DeletedAt DATETIME2(0) NULL,
    CONSTRAINT FK_NguoiDung_VaiTro FOREIGN KEY (IDVaiTro) REFERENCES VaiTro(IDVaiTro)
);

CREATE TABLE DatLaiMatKhauToken (
    IDToken BIGINT PRIMARY KEY IDENTITY(1,1),
    IDNguoiDung INT NOT NULL,
    TokenHash CHAR(64) NOT NULL,
    NgayHetHanUtc DATETIME2(0) NOT NULL,
    NgayTaoUtc DATETIME2(0) NOT NULL CONSTRAINT DF_DatLaiMatKhauToken_NgayTaoUtc DEFAULT SYSUTCDATETIME(),
    NgaySuDungUtc DATETIME2(0) NULL,
    DiaChiIP VARCHAR(45) NULL,
    CONSTRAINT FK_DatLaiMatKhauToken_NguoiDung FOREIGN KEY (IDNguoiDung)
        REFERENCES NguoiDung(IDNguoiDung) ON DELETE CASCADE
);

CREATE TABLE DiaChi (
    IDDiaChi INT PRIMARY KEY IDENTITY(1,1),
    IDNguoiDung INT NOT NULL,
    TenNguoiNhan NVARCHAR(100) NOT NULL,
    SoDienThoai VARCHAR(15) NOT NULL,
    ChiTiet NVARCHAR(255) NOT NULL, 
    PhuongXa NVARCHAR(100) NOT NULL,
    QuanHuyen NVARCHAR(100) NOT NULL,
    TinhThanh NVARCHAR(100) NOT NULL,
    LaMacDinh BIT NOT NULL CONSTRAINT DF_DiaChi_LaMacDinh DEFAULT 0,
    NgayTao DATETIME2(0) NOT NULL CONSTRAINT DF_DiaChi_NgayTao DEFAULT SYSDATETIME(),
    CONSTRAINT FK_DiaChi_NguoiDung FOREIGN KEY (IDNguoiDung)
        REFERENCES NguoiDung(IDNguoiDung) ON DELETE CASCADE
);

-- 2.3. Sản phẩm, Biến thể & Hình ảnh
CREATE TABLE SanPham (
    IDSanPham INT PRIMARY KEY IDENTITY(1,1),
    TenSanPham NVARCHAR(255) NOT NULL,
    Slug NVARCHAR(255) NULL,
    MoTa NVARCHAR(MAX) NULL,
    Gia DECIMAL(18, 0) NOT NULL CONSTRAINT DF_SanPham_Gia DEFAULT 0,
    GiaKhuyenMai DECIMAL(18, 0) NULL,
    NgayBatDauKM DATETIME2(0) NULL,
    NgayKetThucKM DATETIME2(0) NULL,
    IDDanhMuc INT NULL,
    IDThuongHieu INT NULL,
    TrangThai BIT NOT NULL CONSTRAINT DF_SanPham_TrangThai DEFAULT 1,
    NgayTao DATETIME2(0) NOT NULL CONSTRAINT DF_SanPham_NgayTao DEFAULT SYSDATETIME(),
    NgayCapNhat DATETIME2(0) NULL,
    DeletedAt DATETIME2(0) NULL,
    CONSTRAINT FK_SanPham_DanhMuc FOREIGN KEY (IDDanhMuc) REFERENCES DanhMuc(IDDanhMuc),
    CONSTRAINT FK_SanPham_ThuongHieu FOREIGN KEY (IDThuongHieu) REFERENCES ThuongHieu(IDThuongHieu),
    CONSTRAINT CK_SanPham_Gia CHECK (Gia >= 0),
    CONSTRAINT CK_SanPham_GiaKhuyenMai CHECK (
        GiaKhuyenMai IS NULL OR (GiaKhuyenMai >= 0 AND GiaKhuyenMai <= Gia)
    ),
    CONSTRAINT CK_SanPham_ThoiGianKhuyenMai CHECK (
        NgayBatDauKM IS NULL OR NgayKetThucKM IS NULL OR NgayBatDauKM <= NgayKetThucKM
    )
);

CREATE TABLE BienTheSanPham (
    IDBienThe INT PRIMARY KEY IDENTITY(1,1),
    IDSanPham INT NOT NULL,
    IDMauSac INT,
    IDKichThuoc INT,
    SKU VARCHAR(100) NOT NULL,
    Gia DECIMAL(18, 0) NOT NULL CONSTRAINT DF_BienTheSanPham_Gia DEFAULT 0,
    SoLuongTon INT NOT NULL CONSTRAINT DF_BienTheSanPham_SoLuongTon DEFAULT 0,
    SoLuongCanhBao INT NOT NULL CONSTRAINT DF_BienTheSanPham_SoLuongCanhBao DEFAULT 10,
    TongDaBan INT NOT NULL CONSTRAINT DF_BienTheSanPham_TongDaBan DEFAULT 0,
    TrangThai BIT NOT NULL CONSTRAINT DF_BienTheSanPham_TrangThai DEFAULT 1,
    NgayTao DATETIME2(0) NOT NULL CONSTRAINT DF_BienTheSanPham_NgayTao DEFAULT SYSDATETIME(),
    NgayCapNhat DATETIME2(0) NULL,
    DeletedAt DATETIME2(0) NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT UQ_BienTheSanPham_SKU UNIQUE (SKU),
    CONSTRAINT FK_BienTheSanPham_SanPham FOREIGN KEY (IDSanPham)
        REFERENCES SanPham(IDSanPham) ON DELETE CASCADE,
    CONSTRAINT FK_BienTheSanPham_MauSac FOREIGN KEY (IDMauSac) REFERENCES MauSac(IDMauSac),
    CONSTRAINT FK_BienTheSanPham_KichThuoc FOREIGN KEY (IDKichThuoc) REFERENCES KichThuoc(IDKichThuoc),
    CONSTRAINT CK_BienTheSanPham_Gia CHECK (Gia >= 0),
    CONSTRAINT CK_BienTheSanPham_SoLuongTon CHECK (SoLuongTon >= 0),
    CONSTRAINT CK_BienTheSanPham_SoLuongCanhBao CHECK (SoLuongCanhBao >= 0),
    CONSTRAINT CK_BienTheSanPham_TongDaBan CHECK (TongDaBan >= 0)
);

CREATE TABLE HinhAnh (
    IDHinhAnh INT PRIMARY KEY IDENTITY(1,1),
    DuongDan VARCHAR(500) NOT NULL,
    MoTa NVARCHAR(255) NULL,
    NgayTao DATETIME2(0) NOT NULL CONSTRAINT DF_HinhAnh_NgayTao DEFAULT SYSDATETIME()
);

CREATE TABLE HinhAnh_BienThe (
    IDHinhAnh INT NOT NULL,
    IDBienThe INT NOT NULL,
    LaAnhChinh BIT NOT NULL CONSTRAINT DF_HinhAnhBienThe_LaAnhChinh DEFAULT 0,
    ThuTuHienThi INT NOT NULL CONSTRAINT DF_HinhAnhBienThe_ThuTuHienThi DEFAULT 0,
    PRIMARY KEY (IDHinhAnh, IDBienThe),
    CONSTRAINT FK_HinhAnhBienThe_HinhAnh FOREIGN KEY (IDHinhAnh)
        REFERENCES HinhAnh(IDHinhAnh) ON DELETE CASCADE,
    CONSTRAINT FK_HinhAnhBienThe_BienTheSanPham FOREIGN KEY (IDBienThe)
        REFERENCES BienTheSanPham(IDBienThe) ON DELETE CASCADE
);

-- 2.4. Giỏ hàng & Khuyến mãi
CREATE TABLE GioHang (
    IDNguoiDung INT NOT NULL,
    IDBienThe INT NOT NULL,
    SoLuong INT NOT NULL CONSTRAINT DF_GioHang_SoLuong DEFAULT 1,
    NgayThem DATETIME2(0) NOT NULL CONSTRAINT DF_GioHang_NgayThem DEFAULT SYSDATETIME(),
    NgayCapNhat DATETIME2(0) NOT NULL CONSTRAINT DF_GioHang_NgayCapNhat DEFAULT SYSDATETIME(),
    PRIMARY KEY (IDNguoiDung, IDBienThe),
    CONSTRAINT FK_GioHang_NguoiDung FOREIGN KEY (IDNguoiDung)
        REFERENCES NguoiDung(IDNguoiDung) ON DELETE CASCADE,
    CONSTRAINT FK_GioHang_BienTheSanPham FOREIGN KEY (IDBienThe)
        REFERENCES BienTheSanPham(IDBienThe) ON DELETE CASCADE,
    CONSTRAINT CK_GioHang_SoLuong CHECK (SoLuong > 0)
);

CREATE TABLE MaGiamGia (
    IDMaGiamGia INT PRIMARY KEY IDENTITY(1,1),
    MaCode VARCHAR(50) NOT NULL UNIQUE,
    TenChuongTrinh NVARCHAR(255),
    LoaiGiamGia INT NOT NULL, -- 1: Tiền cố định, 2: Phần trăm
    GiaTri DECIMAL(18, 0) NOT NULL,
    DonHangToiThieu DECIMAL(18, 0) NOT NULL CONSTRAINT DF_MaGiamGia_DonHangToiThieu DEFAULT 0,
    GiamToiDa DECIMAL(18, 0) NULL,
    SoLuong INT NOT NULL,
    DaSuDung INT NOT NULL CONSTRAINT DF_MaGiamGia_DaSuDung DEFAULT 0,
    NgayBatDau DATETIME2(0) NOT NULL,
    NgayKetThuc DATETIME2(0) NOT NULL,
    TrangThai BIT NOT NULL CONSTRAINT DF_MaGiamGia_TrangThai DEFAULT 1,
    NgayTao DATETIME2(0) NOT NULL CONSTRAINT DF_MaGiamGia_NgayTao DEFAULT SYSDATETIME(),
    DeletedAt DATETIME2(0) NULL,
    CONSTRAINT CK_MaGiamGia_Loai CHECK (LoaiGiamGia IN (1, 2)),
    CONSTRAINT CK_MaGiamGia_GiaTri CHECK (
        GiaTri > 0 AND (LoaiGiamGia = 1 OR GiaTri <= 100)
    ),
    CONSTRAINT CK_MaGiamGia_SoLuong CHECK (
        SoLuong >= 0 AND DaSuDung >= 0 AND DaSuDung <= SoLuong
    ),
    CONSTRAINT CK_MaGiamGia_ThoiGian CHECK (NgayBatDau <= NgayKetThuc)
);

-- 2.5. Đơn hàng & Chi tiết đơn hàng
CREATE TABLE DonHang (
    IDDonHang INT PRIMARY KEY IDENTITY(1,1),
    IDNguoiDung INT NULL,
    IDMaGiamGia INT NULL,
    TenNguoiNhan NVARCHAR(100) NOT NULL,
    DiaChiGiao NVARCHAR(500) NOT NULL,
    SoDienThoai VARCHAR(15) NOT NULL,
    TongTienHang DECIMAL(18, 0) NOT NULL,
    PhiVanChuyen DECIMAL(18, 0) NOT NULL CONSTRAINT DF_DonHang_PhiVanChuyen DEFAULT 0,
    TienGiamGia DECIMAL(18, 0) NOT NULL CONSTRAINT DF_DonHang_TienGiamGia DEFAULT 0,
    TongThanhToan DECIMAL(18, 0) NOT NULL,
    IDPhuongThucThanhToan INT NULL,
    TrangThaiThanhToan TINYINT NOT NULL
        CONSTRAINT DF_DonHang_TrangThaiThanhToan DEFAULT 0,
    NgayThanhToan DATETIME2(0) NULL,
    IDTrangThai INT NOT NULL,
    GhiChu NVARCHAR(500) NULL,
    NgayTao DATETIME2(0) NOT NULL CONSTRAINT DF_DonHang_NgayTao DEFAULT SYSDATETIME(),
    NgayCapNhat DATETIME2(0) NULL,
    CONSTRAINT FK_DonHang_NguoiDung FOREIGN KEY (IDNguoiDung)
        REFERENCES NguoiDung(IDNguoiDung) ON DELETE SET NULL,
    CONSTRAINT FK_DonHang_MaGiamGia FOREIGN KEY (IDMaGiamGia) REFERENCES MaGiamGia(IDMaGiamGia),
    CONSTRAINT FK_DonHang_PhuongThucThanhToan FOREIGN KEY (IDPhuongThucThanhToan)
        REFERENCES PhuongThucThanhToan(IDPhuongThucThanhToan) ON DELETE SET NULL,
    CONSTRAINT FK_DonHang_TrangThaiDonHang FOREIGN KEY (IDTrangThai)
        REFERENCES TrangThaiDonHang(IDTrangThai),
    CONSTRAINT CK_DonHang_SoTien CHECK (
        TongTienHang >= 0 AND PhiVanChuyen >= 0 AND TienGiamGia >= 0
        AND TongThanhToan >= 0
        AND TongThanhToan = TongTienHang + PhiVanChuyen - TienGiamGia
    ),
    CONSTRAINT CK_DonHang_TrangThaiThanhToan CHECK (TrangThaiThanhToan BETWEEN 0 AND 4)
);

CREATE TABLE GiaoDichThanhToan (
    IDGiaoDich BIGINT NOT NULL IDENTITY(1,1)
        CONSTRAINT PK_GiaoDichThanhToan PRIMARY KEY,
    IDDonHang INT NOT NULL,
    MaThamChieu VARCHAR(100) NOT NULL,
    CongThanhToan VARCHAR(30) NOT NULL,
    SoTien DECIMAL(18, 0) NOT NULL,
    TrangThai TINYINT NOT NULL
        CONSTRAINT DF_GiaoDichThanhToan_TrangThai DEFAULT 1,
    MaGiaoDichCong VARCHAR(50) NULL,
    MaPhanHoi VARCHAR(10) NULL,
    MaNganHang VARCHAR(20) NULL,
    NoiDung NVARCHAR(255) NULL,
    NgayTao DATETIME2(0) NOT NULL
        CONSTRAINT DF_GiaoDichThanhToan_NgayTao DEFAULT SYSUTCDATETIME(),
    NgayCapNhat DATETIME2(0) NULL,
    NgayThanhToan DATETIME2(0) NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT UQ_GiaoDichThanhToan_MaThamChieu UNIQUE (MaThamChieu),
    CONSTRAINT FK_GiaoDichThanhToan_DonHang FOREIGN KEY (IDDonHang)
        REFERENCES DonHang(IDDonHang) ON DELETE CASCADE,
    CONSTRAINT CK_GiaoDichThanhToan_SoTien CHECK (SoTien > 0),
    CONSTRAINT CK_GiaoDichThanhToan_TrangThai CHECK (TrangThai BETWEEN 1 AND 4)
);

CREATE TABLE ChiTietDonHang (
    IDChiTietDonHang INT PRIMARY KEY IDENTITY(1,1),
    IDDonHang INT NOT NULL,
    IDBienThe INT NULL,
    SoLuong INT NOT NULL,
    DonGia DECIMAL(18, 0) NOT NULL,
    TenSanPham NVARCHAR(255) NOT NULL,
    TenMau NVARCHAR(50) NULL,
    TenKichThuoc VARCHAR(50) NULL,
    CONSTRAINT FK_ChiTietDonHang_DonHang FOREIGN KEY (IDDonHang)
        REFERENCES DonHang(IDDonHang) ON DELETE CASCADE,
    CONSTRAINT FK_ChiTietDonHang_BienTheSanPham FOREIGN KEY (IDBienThe)
        REFERENCES BienTheSanPham(IDBienThe) ON DELETE SET NULL,
    CONSTRAINT UQ_ChiTietDonHang_DonHang_BienThe UNIQUE (IDDonHang, IDBienThe),
    CONSTRAINT CK_ChiTietDonHang_SoLuong CHECK (SoLuong > 0),
    CONSTRAINT CK_ChiTietDonHang_DonGia CHECK (DonGia >= 0)
);

-- 2.6. Wishlist, đánh giá và lịch sử nghiệp vụ
CREATE TABLE YeuThich (
    IDNguoiDung INT NOT NULL,
    IDSanPham INT NOT NULL,
    NgayThem DATETIME2(0) NOT NULL CONSTRAINT DF_YeuThich_NgayThem DEFAULT SYSDATETIME(),
    CONSTRAINT PK_YeuThich PRIMARY KEY (IDNguoiDung, IDSanPham),
    CONSTRAINT FK_YeuThich_NguoiDung FOREIGN KEY (IDNguoiDung)
        REFERENCES NguoiDung(IDNguoiDung) ON DELETE CASCADE,
    CONSTRAINT FK_YeuThich_SanPham FOREIGN KEY (IDSanPham)
        REFERENCES SanPham(IDSanPham) ON DELETE CASCADE
);

CREATE TABLE DanhGia (
    IDDanhGia INT PRIMARY KEY IDENTITY(1,1),
    IDNguoiDung INT NOT NULL,
    IDSanPham INT NOT NULL,
    IDChiTietDonHang INT NULL,
    DiemSo TINYINT NOT NULL,
    NoiDung NVARCHAR(2000) NULL,
    TrangThai BIT NOT NULL CONSTRAINT DF_DanhGia_TrangThai DEFAULT 1,
    NgayTao DATETIME2(0) NOT NULL CONSTRAINT DF_DanhGia_NgayTao DEFAULT SYSDATETIME(),
    NgayCapNhat DATETIME2(0) NULL,
    DeletedAt DATETIME2(0) NULL,
    CONSTRAINT FK_DanhGia_NguoiDung FOREIGN KEY (IDNguoiDung) REFERENCES NguoiDung(IDNguoiDung),
    CONSTRAINT FK_DanhGia_SanPham FOREIGN KEY (IDSanPham) REFERENCES SanPham(IDSanPham),
    CONSTRAINT FK_DanhGia_ChiTietDonHang FOREIGN KEY (IDChiTietDonHang)
        REFERENCES ChiTietDonHang(IDChiTietDonHang) ON DELETE SET NULL,
    CONSTRAINT UQ_DanhGia_NguoiDung_SanPham UNIQUE (IDNguoiDung, IDSanPham),
    CONSTRAINT CK_DanhGia_DiemSo CHECK (DiemSo BETWEEN 1 AND 5)
);

CREATE TABLE LichSuTonKho (
    IDLichSu INT PRIMARY KEY IDENTITY(1,1),
    IDBienThe INT NOT NULL,
    IDNguoiThucHien INT NULL,
    IDDonHang INT NULL,
    LoaiThayDoi NVARCHAR(50) NOT NULL,
    SoLuongThayDoi INT NOT NULL,
    TonTruoc INT NOT NULL,
    TonSau INT NOT NULL,
    GhiChu NVARCHAR(500) NULL,
    NgayTao DATETIME2(0) NOT NULL CONSTRAINT DF_LichSuTonKho_NgayTao DEFAULT SYSDATETIME(),
    CONSTRAINT FK_LichSuTonKho_BienTheSanPham FOREIGN KEY (IDBienThe)
        REFERENCES BienTheSanPham(IDBienThe),
    CONSTRAINT FK_LichSuTonKho_NguoiDung FOREIGN KEY (IDNguoiThucHien)
        REFERENCES NguoiDung(IDNguoiDung) ON DELETE SET NULL,
    CONSTRAINT FK_LichSuTonKho_DonHang FOREIGN KEY (IDDonHang)
        REFERENCES DonHang(IDDonHang) ON DELETE SET NULL,
    CONSTRAINT CK_LichSuTonKho_Ton CHECK (TonTruoc >= 0 AND TonSau >= 0)
);

CREATE TABLE LichSuDonHang (
    IDLichSu INT PRIMARY KEY IDENTITY(1,1),
    IDDonHang INT NOT NULL,
    IDTrangThaiCu INT NULL,
    IDTrangThaiMoi INT NOT NULL,
    IDNguoiThucHien INT NULL,
    GhiChu NVARCHAR(500) NULL,
    NgayTao DATETIME2(0) NOT NULL CONSTRAINT DF_LichSuDonHang_NgayTao DEFAULT SYSDATETIME(),
    CONSTRAINT FK_LichSuDonHang_DonHang FOREIGN KEY (IDDonHang)
        REFERENCES DonHang(IDDonHang) ON DELETE CASCADE,
    CONSTRAINT FK_LichSuDonHang_TrangThaiCu FOREIGN KEY (IDTrangThaiCu)
        REFERENCES TrangThaiDonHang(IDTrangThai),
    CONSTRAINT FK_LichSuDonHang_TrangThaiMoi FOREIGN KEY (IDTrangThaiMoi)
        REFERENCES TrangThaiDonHang(IDTrangThai),
    CONSTRAINT FK_LichSuDonHang_NguoiDung FOREIGN KEY (IDNguoiThucHien)
        REFERENCES NguoiDung(IDNguoiDung) ON DELETE SET NULL
);

CREATE TABLE AdminActivityLog (
    IDLog BIGINT PRIMARY KEY IDENTITY(1,1),
    IDAdmin INT NULL,
    HanhDong NVARCHAR(100) NOT NULL,
    TenBang NVARCHAR(100) NULL,
    IDBanGhi NVARCHAR(100) NULL,
    DuLieuCu NVARCHAR(MAX) NULL,
    DuLieuMoi NVARCHAR(MAX) NULL,
    DiaChiIP VARCHAR(45) NULL,
    NgayTao DATETIME2(0) NOT NULL CONSTRAINT DF_AdminActivityLog_NgayTao DEFAULT SYSDATETIME(),
    CONSTRAINT FK_AdminActivityLog_NguoiDung FOREIGN KEY (IDAdmin)
        REFERENCES NguoiDung(IDNguoiDung) ON DELETE SET NULL,
    CONSTRAINT CK_AdminActivityLog_DuLieuCuJson CHECK (DuLieuCu IS NULL OR ISJSON(DuLieuCu) = 1),
    CONSTRAINT CK_AdminActivityLog_DuLieuMoiJson CHECK (DuLieuMoi IS NULL OR ISJSON(DuLieuMoi) = 1)
);

CREATE TABLE CuocTroChuyen (
    IDCuocTroChuyen UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT PK_CuocTroChuyen PRIMARY KEY,
    IDNguoiDung INT NOT NULL,
    NgayTao DATETIME2(0) NOT NULL
        CONSTRAINT DF_CuocTroChuyen_NgayTao DEFAULT SYSUTCDATETIME(),
    NgayCapNhat DATETIME2(0) NOT NULL
        CONSTRAINT DF_CuocTroChuyen_NgayCapNhat DEFAULT SYSUTCDATETIME(),
    NgayKetThuc DATETIME2(0) NULL,
    CONSTRAINT FK_CuocTroChuyen_NguoiDung FOREIGN KEY (IDNguoiDung)
        REFERENCES NguoiDung(IDNguoiDung) ON DELETE CASCADE
);

CREATE TABLE TinNhanChat (
    IDTinNhan BIGINT NOT NULL IDENTITY(1,1)
        CONSTRAINT PK_TinNhanChat PRIMARY KEY,
    IDCuocTroChuyen UNIQUEIDENTIFIER NOT NULL,
    VaiTro VARCHAR(20) NOT NULL,
    NoiDung NVARCHAR(2000) NOT NULL,
    DuLieuJson NVARCHAR(MAX) NULL,
    NgayTao DATETIME2(0) NOT NULL
        CONSTRAINT DF_TinNhanChat_NgayTao DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_TinNhanChat_CuocTroChuyen FOREIGN KEY (IDCuocTroChuyen)
        REFERENCES CuocTroChuyen(IDCuocTroChuyen) ON DELETE CASCADE,
    CONSTRAINT CK_TinNhanChat_VaiTro CHECK (VaiTro IN ('user', 'assistant')),
    CONSTRAINT CK_TinNhanChat_DuLieuJson CHECK (
        DuLieuJson IS NULL OR ISJSON(DuLieuJson) = 1
    )
);
GO

-- =================================================================
-- PHẦN 3: TẠO INDEX VÀ RÀNG BUỘC BỔ SUNG
-- =================================================================

CREATE UNIQUE NONCLUSTERED INDEX IX_NguoiDung_SoDienThoai 
ON NguoiDung(SoDienThoai) 
WHERE SoDienThoai IS NOT NULL;

CREATE UNIQUE INDEX UX_DatLaiMatKhauToken_TokenHash
ON DatLaiMatKhauToken(TokenHash);

CREATE INDEX IX_DatLaiMatKhauToken_NguoiDung_HetHan
ON DatLaiMatKhauToken(IDNguoiDung, NgayHetHanUtc DESC)
INCLUDE (NgaySuDungUtc);

CREATE UNIQUE NONCLUSTERED INDEX UX_DanhMuc_Slug
ON DanhMuc(Slug)
WHERE Slug IS NOT NULL AND DeletedAt IS NULL;

CREATE UNIQUE NONCLUSTERED INDEX UX_SanPham_Slug
ON SanPham(Slug)
WHERE Slug IS NOT NULL AND DeletedAt IS NULL;

CREATE UNIQUE NONCLUSTERED INDEX UX_DiaChi_MacDinh
ON DiaChi(IDNguoiDung)
WHERE LaMacDinh = 1;

CREATE UNIQUE NONCLUSTERED INDEX UX_HinhAnhBienThe_AnhChinh
ON HinhAnh_BienThe(IDBienThe)
WHERE LaAnhChinh = 1;

CREATE INDEX IX_SanPham_DanhMuc_TrangThai
ON SanPham(IDDanhMuc, TrangThai, DeletedAt);

CREATE INDEX IX_SanPham_ThuongHieu
ON SanPham(IDThuongHieu, DeletedAt);

CREATE INDEX IX_BienTheSanPham_SanPham_TrangThai
ON BienTheSanPham(IDSanPham, TrangThai, DeletedAt);

CREATE INDEX IX_GioHang_NgayCapNhat
ON GioHang(IDNguoiDung, NgayCapNhat DESC);

CREATE INDEX IX_DonHang_NguoiDung_NgayTao
ON DonHang(IDNguoiDung, NgayTao DESC);

CREATE INDEX IX_DonHang_TrangThai_NgayTao
ON DonHang(IDTrangThai, NgayTao DESC);

CREATE INDEX IX_GiaoDichThanhToan_DonHang_NgayTao
ON GiaoDichThanhToan(IDDonHang, NgayTao DESC);

CREATE INDEX IX_GiaoDichThanhToan_TrangThai_NgayTao
ON GiaoDichThanhToan(TrangThai, NgayTao DESC);

CREATE INDEX IX_ChiTietDonHang_BienThe
ON ChiTietDonHang(IDBienThe);

CREATE INDEX IX_DanhGia_SanPham_TrangThai
ON DanhGia(IDSanPham, TrangThai, DeletedAt);

CREATE INDEX IX_LichSuTonKho_BienThe_NgayTao
ON LichSuTonKho(IDBienThe, NgayTao DESC);

CREATE INDEX IX_LichSuDonHang_DonHang_NgayTao
ON LichSuDonHang(IDDonHang, NgayTao DESC);

CREATE INDEX IX_AdminActivityLog_Admin_NgayTao
ON AdminActivityLog(IDAdmin, NgayTao DESC);

CREATE UNIQUE NONCLUSTERED INDEX UX_CuocTroChuyen_DangHoatDong
ON CuocTroChuyen(IDNguoiDung)
WHERE NgayKetThuc IS NULL;

CREATE INDEX IX_CuocTroChuyen_NguoiDung_NgayCapNhat
ON CuocTroChuyen(IDNguoiDung, NgayCapNhat DESC);

CREATE INDEX IX_TinNhanChat_CuocTroChuyen_NgayTao
ON TinNhanChat(IDCuocTroChuyen, NgayTao, IDTinNhan);
GO

-- =================================================================
-- PHẦN 4: NẠP DỮ LIỆU MẪU (SEED DATA) CHUẨN
-- =================================================================

-- 4.1. Vai trò & Trạng thái & Phương thức thanh toán
INSERT INTO VaiTro (IDVaiTro, TenVaiTro) VALUES
(1, 'Admin'),
(2, 'Customer');

INSERT INTO TrangThaiDonHang (IDTrangThai, TenTrangThai) VALUES 
(0, N'Chờ xác nhận'), 
(1, N'Đã xác nhận'), 
(2, N'Đang giao'), 
(3, N'Hoàn thành'), 
(4, N'Đã hủy');

INSERT INTO PhuongThucThanhToan (MaPhuongThuc, TenPhuongThuc, TrangThai) VALUES
('COD', N'Thanh toán khi nhận hàng (COD)', 1),
('VNPAY', N'Thanh toán ngân hàng qua VNPAY', 1),
('MOMO', N'Ví điện tử Momo', 0);

-- 4.2. Danh mục & Thương hiệu & Màu sắc & Kích thước
INSERT INTO DanhMuc (TenDanhMuc, Slug, IDDanhMucCha, ThuTuHienThi) VALUES
(N'Thời Trang Nam', N'thoi-trang-nam', NULL, 1),
(N'Thời Trang Nữ', N'thoi-trang-nu', NULL, 2),
(N'Phụ Kiện', N'phu-kien', NULL, 3);

INSERT INTO DanhMuc (TenDanhMuc, Slug, IDDanhMucCha, ThuTuHienThi) VALUES
(N'Áo Nam', N'ao-nam', 1, 1),
(N'Quần Nam', N'quan-nam', 1, 2),
(N'Đồ Mặc Ngoài Nam', N'do-mac-ngoai-nam', 1, 3),
(N'Váy & Đầm', N'vay-dam', 2, 1),
(N'Áo Nữ', N'ao-nu', 2, 2),
(N'Quần Nữ', N'quan-nu', 2, 3),
(N'Túi Xách', N'tui-xach', 3, 1),
(N'Mũ & Nón', N'mu-non', 3, 2);

INSERT INTO ThuongHieu (TenThuongHieu) VALUES 
(N'Uniqlo'), (N'Zara'), (N'H&M'), (N'Levi''s'), (N'Gucci'), (N'Nike');

INSERT INTO MauSac (TenMau, MaMauHex) VALUES 
(N'Trắng', '#FFFFFF'), 
(N'Đen', '#000000'), 
(N'Xanh Navy', '#000080'), 
(N'Beige', '#F5F5DC'), 
(N'Xám', '#808080'), 
(N'Đỏ', '#FF0000'), 
(N'Xanh Olive', '#808000'),
(N'Tím', '#800080'),
(N'Nâu', '#8B4513'),
(N'Hồng', '#FFC0CB'),
(N'Xanh Đen', '#000033');

INSERT INTO KichThuoc (TenKichThuoc) VALUES ('S'), ('M'), ('L'), ('XL'), ('XXL');

-- 4.3. Người dùng & Địa chỉ mẫu
-- Tất cả tài khoản demo dùng BCrypt. Không lưu mật khẩu dạng rõ trong database.
DECLARE @DemoPasswordHash VARCHAR(255) = '$2a$12$AZGJYVN0XcX7Fb7oMA1/xebhH6AorfdkpNoPd/WgMVKtkzhWVGjpK';

INSERT INTO NguoiDung (HoTen, Email, SoDienThoai, MatKhauHash, IDVaiTro) VALUES
(N'Admin FashionHub', 'admin@fashionhub.local', '0911111111', @DemoPasswordHash, 1),
(N'Nguyễn Thị Lan', 'lan.nguyen@fashionhub.local', '0987654321', @DemoPasswordHash, 2),
(N'Trần Văn Bình', 'binh.tran@fashionhub.local', '0912345678', @DemoPasswordHash, 2);

INSERT INTO DiaChi (IDNguoiDung, TenNguoiNhan, SoDienThoai, ChiTiet, PhuongXa, QuanHuyen, TinhThanh, LaMacDinh) VALUES
(2, N'Nguyễn Thị Lan', '0987654321', N'Số 123, Đường Lê Lợi', N'Bến Nghé', N'Quận 1', N'TP. Hồ Chí Minh', 1),
(3, N'Trần Văn Bình', '0912345678', N'Phòng 502, Tòa nhà ABC', N'Dịch Vọng Hậu', N'Cầu Giấy', N'Hà Nội', 1);

-- 4.4. Mã giảm giá
INSERT INTO MaGiamGia (MaCode, TenChuongTrinh, LoaiGiamGia, GiaTri, DonHangToiThieu, GiamToiDa, SoLuong, NgayBatDau, NgayKetThuc, TrangThai) VALUES 
('GIAM10', N'Giảm 10%', 2, 10.00, 500000, 70000, 100, GETDATE()-1, GETDATE()+30, 1),
('FREESHIP', N'Freeship 30k', 1, 30000, 200000, NULL, 200, GETDATE(), GETDATE()+15, 1);

-- =================================================================
-- PHẦN 5: KHỞI TẠO SẢN PHẨM & BIẾN THỂ (KÈM HÌNH ẢNH)
-- =================================================================

-- Lấy ID Màu sắc dùng cho chèn sản phẩm
DECLARE @MauTrang INT = (SELECT IDMauSac FROM MauSac WHERE TenMau = N'Trắng');
DECLARE @MauDen INT = (SELECT IDMauSac FROM MauSac WHERE TenMau = N'Đen');
DECLARE @MauXam INT = (SELECT IDMauSac FROM MauSac WHERE TenMau = N'Xám');
DECLARE @MauBe INT = (SELECT IDMauSac FROM MauSac WHERE TenMau = N'Beige');
DECLARE @MauXanhDen INT = (SELECT IDMauSac FROM MauSac WHERE TenMau = N'Xanh Đen');
DECLARE @MauTim INT = (SELECT IDMauSac FROM MauSac WHERE TenMau = N'Tím');
DECLARE @MauNau INT = (SELECT IDMauSac FROM MauSac WHERE TenMau = N'Nâu');
DECLARE @MauHong INT = (SELECT IDMauSac FROM MauSac WHERE TenMau = N'Hồng');

-- Lấy ID Kích thước
DECLARE @SizeS INT = (SELECT IDKichThuoc from KichThuoc WHERE TenKichThuoc = 'S');
DECLARE @SizeM INT = (SELECT IDKichThuoc from KichThuoc WHERE TenKichThuoc = 'M');
DECLARE @SizeL INT = (SELECT IDKichThuoc from KichThuoc WHERE TenKichThuoc = 'L');

-- -----------------------------------------------------------------
-- SẢN PHẨM 1: ÁO THUN BOXY FIT (IDDanhMuc = 4: Áo Nam)
-- -----------------------------------------------------------------
INSERT INTO SanPham (TenSanPham, Slug, MoTa, Gia, IDDanhMuc, IDThuongHieu, TrangThai)
VALUES (N'Áo Thun Boxy Fit Basic', N'ao-thun-boxy-fit-basic', N'Form Boxy rộng rãi, thoải mái. Chất liệu cotton dày dặn.', 320000, 4, 1, 1);
DECLARE @SP_Boxy INT = SCOPE_IDENTITY();

-- Biến thể & Giá biến thể tương ứng
INSERT INTO BienTheSanPham (IDSanPham, IDMauSac, IDKichThuoc, SKU, Gia, SoLuongTon) VALUES 
(@SP_Boxy, @MauBe, @SizeM, 'TS-BOXY-BE-M', 320000, 50),
(@SP_Boxy, @MauDen, @SizeM, 'TS-BOXY-BLK-M', 320000, 50),
(@SP_Boxy, @MauXam, @SizeM, 'TS-BOXY-GRY-M', 320000, 50);

DECLARE @BT_Boxy_Be INT = (SELECT IDBienThe FROM BienTheSanPham WHERE SKU = 'TS-BOXY-BE-M');
DECLARE @BT_Boxy_Den INT = (SELECT IDBienThe FROM BienTheSanPham WHERE SKU = 'TS-BOXY-BLK-M');
DECLARE @BT_Boxy_Xam INT = (SELECT IDBienThe FROM BienTheSanPham WHERE SKU = 'TS-BOXY-GRY-M');

-- Chèn ảnh biến thể
INSERT INTO HinhAnh (DuongDan) VALUES ('/images/products/aothun1_be_boxy.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Boxy_Be, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/images/products/aothun1_den_boxy.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Boxy_Den, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/images/products/aothun1_xam_boxy.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Boxy_Xam, 1);

-- -----------------------------------------------------------------
-- SẢN PHẨM 2: ÁO THUN REGULAR (IDDanhMuc = 4: Áo Nam)
-- -----------------------------------------------------------------
INSERT INTO SanPham (TenSanPham, Slug, MoTa, Gia, IDDanhMuc, IDThuongHieu, TrangThai)
VALUES (N'Áo Thun Regular Essential', N'ao-thun-regular-essential', N'Dáng Regular vừa vặn, phù hợp mặc hàng ngày.', 250000, 4, 2, 1);
DECLARE @SP_Regular INT = SCOPE_IDENTITY();

INSERT INTO BienTheSanPham (IDSanPham, IDMauSac, IDKichThuoc, SKU, Gia, SoLuongTon) VALUES 
(@SP_Regular, @MauDen, @SizeL, 'TS-REG-BLK-L', 250000, 100),
(@SP_Regular, @MauTrang, @SizeL, 'TS-REG-WHT-L', 250000, 100),
(@SP_Regular, @MauXam, @SizeL, 'TS-REG-GRY-L', 250000, 100);

DECLARE @BT_Reg_Den INT = (SELECT IDBienThe FROM BienTheSanPham WHERE SKU = 'TS-REG-BLK-L');
DECLARE @BT_Reg_Trang INT = (SELECT IDBienThe FROM BienTheSanPham WHERE SKU = 'TS-REG-WHT-L');
DECLARE @BT_Reg_Xam INT = (SELECT IDBienThe FROM BienTheSanPham WHERE SKU = 'TS-REG-GRY-L');

INSERT INTO HinhAnh (DuongDan) VALUES ('/images/products/aothun2_den_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Reg_Den, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/images/products/aothun2_trang_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Reg_Trang, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/images/products/aothun2_xam_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Reg_Xam, 1);

-- -----------------------------------------------------------------
-- SẢN PHẨM 3: QUẦN JEAN STRAIGHT (IDDanhMuc = 5: Quần Nam)
-- -----------------------------------------------------------------
INSERT INTO SanPham (TenSanPham, Slug, MoTa, Gia, IDDanhMuc, IDThuongHieu, TrangThai)
VALUES (N'Quần Jeans Straight Xanh Đen', N'quan-jeans-straight-xanh-den', N'Quần bò ống đứng màu xanh đen nam tính. Vải denim không co giãn.', 550000, 5, 4, 1);
DECLARE @SP_Jean INT = SCOPE_IDENTITY();

INSERT INTO BienTheSanPham (IDSanPham, IDMauSac, IDKichThuoc, SKU, Gia, SoLuongTon) VALUES 
(@SP_Jean, @MauXanhDen, @SizeM, 'JN-STR-DBL-M', 550000, 40);
DECLARE @BT_Jean INT = SCOPE_IDENTITY();

-- Nhiều hình ảnh cho 1 biến thể
INSERT INTO HinhAnh (DuongDan) VALUES ('/images/products/jean1_xanhden_straigh_truoc.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Jean, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/images/products/jean1_xanhden_straigh_sau.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Jean, 0);

INSERT INTO HinhAnh (DuongDan) VALUES ('/images/products/jean1_xanhden_straight_mau1.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Jean, 0);

-- -----------------------------------------------------------------
-- SẢN PHẨM 4: ÁO POLO PIQUE (IDDanhMuc = 4: Áo Nam)
-- -----------------------------------------------------------------
INSERT INTO SanPham (TenSanPham, Slug, MoTa, Gia, IDDanhMuc, IDThuongHieu, TrangThai)
VALUES (N'Áo Polo Pique Cotton', N'ao-polo-pique-cotton', N'Áo thun có cổ vải cá sấu. Nhiều màu sắc trẻ trung.', 350000, 4, 5, 1);
DECLARE @SP_Polo INT = SCOPE_IDENTITY();

INSERT INTO BienTheSanPham (IDSanPham, IDMauSac, IDKichThuoc, SKU, Gia, SoLuongTon) VALUES 
(@SP_Polo, @MauTim, @SizeM, 'POLO-TIM-M', 350000, 60),
(@SP_Polo, @MauNau, @SizeM, 'POLO-NAU-M', 350000, 60),
(@SP_Polo, @MauTrang, @SizeM, 'POLO-WHT-M', 350000, 60),
(@SP_Polo, @MauDen, @SizeM, 'POLO-BLK-M', 350000, 60);

DECLARE @BT_Polo_Tim INT = (SELECT IDBienThe FROM BienTheSanPham WHERE SKU = 'POLO-TIM-M');
DECLARE @BT_Polo_Nau INT = (SELECT IDBienThe FROM BienTheSanPham WHERE SKU = 'POLO-NAU-M');
DECLARE @BT_Polo_Trang INT = (SELECT IDBienThe FROM BienTheSanPham WHERE SKU = 'POLO-WHT-M');
DECLARE @BT_Polo_Den INT = (SELECT IDBienThe FROM BienTheSanPham WHERE SKU = 'POLO-BLK-M');

INSERT INTO HinhAnh (DuongDan) VALUES ('/images/products/polo3_tim_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Polo_Tim, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/images/products/polo4_nau_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Polo_Nau, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/images/products/polo3_trang_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Polo_Trang, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/images/products/polo4_den_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Polo_Den, 1);

-- -----------------------------------------------------------------
-- SẢN PHẨM 5: ÁO SƠ MI OXFORD (IDDanhMuc = 4: Áo Nam)
-- -----------------------------------------------------------------
INSERT INTO SanPham (TenSanPham, Slug, MoTa, Gia, IDDanhMuc, IDThuongHieu, TrangThai)
VALUES (N'Áo Sơ Mi Oxford Regular', N'ao-so-mi-oxford-regular', N'Sơ mi vải Oxford đứng form, ít nhăn. Phù hợp công sở.', 420000, 4, 2, 1);
DECLARE @SP_Somi INT = SCOPE_IDENTITY();

INSERT INTO BienTheSanPham (IDSanPham, IDMauSac, IDKichThuoc, SKU, Gia, SoLuongTon) VALUES 
(@SP_Somi, @MauHong, @SizeL, 'SM-HONG-L', 420000, 30),
(@SP_Somi, @MauTrang, @SizeL, 'SM-TRANG-L', 420000, 50),
(@SP_Somi, @MauXanhDen, @SizeL, 'SM-XD-L', 420000, 40);

DECLARE @BT_Somi_Hong INT = (SELECT IDBienThe FROM BienTheSanPham WHERE SKU = 'SM-HONG-L');
DECLARE @BT_Somi_Trang INT = (SELECT IDBienThe FROM BienTheSanPham WHERE SKU = 'SM-TRANG-L');
DECLARE @BT_Somi_XD INT = (SELECT IDBienThe FROM BienTheSanPham WHERE SKU = 'SM-XD-L');

INSERT INTO HinhAnh (DuongDan) VALUES ('/images/products/somi4_hong_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Somi_Hong, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/images/products/somi4_hong_regular_nguoi.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Somi_Hong, 0);

INSERT INTO HinhAnh (DuongDan) VALUES ('/images/products/somi1_trang_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Somi_Trang, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/images/products/somi1_xanhden_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Somi_XD, 1);

COMMIT TRANSACTION;
GO

PRINT 'Database schema and seed data completed successfully.';
GO
