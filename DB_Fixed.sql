create database QL_SHOPQUANAO_PRO;

USE QL_SHOPQUANAO_PRO;
GO

-- =================================================================
-- PHẦN 1: DỌN DẸP BẢNG CŨ (DROP TABLES THEO THỨ TỰ KHÓA NGOẠI)
-- =================================================================

-- Drop các bảng liên kết/phụ thuộc trước
IF OBJECT_ID('ChiTietDonHang', 'U') IS NOT NULL DROP TABLE ChiTietDonHang;
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
    IDVaiTro INT PRIMARY KEY IDENTITY(1,1),
    TenVaiTro VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE TrangThaiDonHang (
    IDTrangThai INT PRIMARY KEY,
    TenTrangThai NVARCHAR(100) NOT NULL
);

CREATE TABLE PhuongThucThanhToan (
    IDPhuongThucThanhToan INT PRIMARY KEY IDENTITY(1,1),
    TenPhuongThuc NVARCHAR(100) NOT NULL
);

CREATE TABLE DanhMuc (
    IDDanhMuc INT PRIMARY KEY IDENTITY(1,1),
    TenDanhMuc NVARCHAR(100) NOT NULL,
    IDDanhMucCha INT NULL,
    FOREIGN KEY (IDDanhMucCha) REFERENCES DanhMuc(IDDanhMuc)
);

CREATE TABLE ThuongHieu (
    IDThuongHieu INT PRIMARY KEY IDENTITY(1,1),
    TenThuongHieu NVARCHAR(100) NOT NULL
);

CREATE TABLE MauSac (
    IDMauSac INT PRIMARY KEY IDENTITY(1,1),
    TenMau NVARCHAR(50) NOT NULL,
    MaMauHex VARCHAR(7)
);

CREATE TABLE KichThuoc (
    IDKichThuoc INT PRIMARY KEY IDENTITY(1,1),
    TenKichThuoc VARCHAR(50) NOT NULL
);

-- 2.2. Người dùng & Địa chỉ
CREATE TABLE NguoiDung (
    IDNguoiDung INT PRIMARY KEY IDENTITY(1,1),
    HoTen NVARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    SoDienThoai VARCHAR(15) NULL,
    MatKhauHash VARCHAR(255) NOT NULL,
    IDVaiTro INT NOT NULL,
    NgayTao DATETIME DEFAULT GETDATE(),
    TrangThai BIT DEFAULT 1,
    FOREIGN KEY (IDVaiTro) REFERENCES VaiTro(IDVaiTro)
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
    LaMacDinh BIT DEFAULT 0,
    FOREIGN KEY (IDNguoiDung) REFERENCES NguoiDung(IDNguoiDung) ON DELETE CASCADE
);

-- 2.3. Sản phẩm, Biến thể & Hình ảnh
CREATE TABLE SanPham (
    IDSanPham INT PRIMARY KEY IDENTITY(1,1),
    TenSanPham NVARCHAR(255) NOT NULL,
    MoTa NVARCHAR(MAX) NULL, -- Đã chuyển từ NTEXT sang NVARCHAR(MAX)
    Gia DECIMAL(18, 2) NOT NULL DEFAULT 0,
    GiaKhuyenMai DECIMAL(18, 2) NULL,
    NgayBatDauKM DATETIME NULL,
    NgayKetThucKM DATETIME NULL,
    IDDanhMuc INT,
    IDThuongHieu INT,
    TrangThai BIT DEFAULT 1,
    FOREIGN KEY (IDDanhMuc) REFERENCES DanhMuc(IDDanhMuc),
    FOREIGN KEY (IDThuongHieu) REFERENCES ThuongHieu(IDThuongHieu)
);

CREATE TABLE BienTheSanPham (
    IDBienThe INT PRIMARY KEY IDENTITY(1,1),
    IDSanPham INT NOT NULL,
    IDMauSac INT,
    IDKichThuoc INT,
    SKU VARCHAR(100) UNIQUE,
    Gia DECIMAL(18, 2) NOT NULL DEFAULT 0, -- Thêm cột Gia để lưu trữ giá biến thể
    SoLuongTon INT NOT NULL DEFAULT 0,
    FOREIGN KEY (IDSanPham) REFERENCES SanPham(IDSanPham) ON DELETE CASCADE,
    FOREIGN KEY (IDMauSac) REFERENCES MauSac(IDMauSac),
    FOREIGN KEY (IDKichThuoc) REFERENCES KichThuoc(IDKichThuoc)
);

CREATE TABLE HinhAnh (
    IDHinhAnh INT PRIMARY KEY IDENTITY(1,1),
    DuongDan VARCHAR(500) NOT NULL,
    MoTa NVARCHAR(255) NULL
);

CREATE TABLE HinhAnh_BienThe (
    IDHinhAnh INT,
    IDBienThe INT,
    LaAnhChinh BIT DEFAULT 0,
    PRIMARY KEY (IDHinhAnh, IDBienThe),
    FOREIGN KEY (IDHinhAnh) REFERENCES HinhAnh(IDHinhAnh) ON DELETE CASCADE,
    FOREIGN KEY (IDBienThe) REFERENCES BienTheSanPham(IDBienThe) ON DELETE CASCADE
);

-- 2.4. Giỏ hàng & Khuyến mãi
CREATE TABLE GioHang (
    IDNguoiDung INT,
    IDBienThe INT,
    SoLuong INT NOT NULL DEFAULT 1,
    PRIMARY KEY (IDNguoiDung, IDBienThe),
    FOREIGN KEY (IDNguoiDung) REFERENCES NguoiDung(IDNguoiDung) ON DELETE CASCADE,
    FOREIGN KEY (IDBienThe) REFERENCES BienTheSanPham(IDBienThe) ON DELETE CASCADE
);

CREATE TABLE MaGiamGia (
    IDMaGiamGia INT PRIMARY KEY IDENTITY(1,1),
    MaCode VARCHAR(50) NOT NULL UNIQUE,
    TenChuongTrinh NVARCHAR(255),
    LoaiGiamGia INT NOT NULL, -- 1: Tiền cố định, 2: Phần trăm
    GiaTri DECIMAL(18, 2) NOT NULL,
    DonHangToiThieu DECIMAL(18, 2) DEFAULT 0,
    GiamToiDa DECIMAL(18, 2) NULL,
    SoLuong INT NOT NULL,
    DaSuDung INT NOT NULL DEFAULT 0,
    NgayBatDau DATETIME,
    NgayKetThuc DATETIME,
    TrangThai BIT NOT NULL DEFAULT 1
);

-- 2.5. Đơn hàng & Chi tiết đơn hàng
CREATE TABLE DonHang (
    IDDonHang INT PRIMARY KEY IDENTITY(1,1),
    IDNguoiDung INT NULL,
    IDMaGiamGia INT NULL,
    TenNguoiNhan NVARCHAR(100) NOT NULL,
    DiaChiGiao NVARCHAR(500) NOT NULL,
    SoDienThoai VARCHAR(15) NOT NULL,
    TongTienHang DECIMAL(18, 2) NOT NULL,
    PhiVanChuyen DECIMAL(18, 2) DEFAULT 0,
    TienGiamGia DECIMAL(18, 2) DEFAULT 0,
    TongThanhToan DECIMAL(18, 2) NOT NULL,
    IDPhuongThucThanhToan INT NULL,
    IDTrangThai INT NOT NULL,
    NgayTao DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (IDNguoiDung) REFERENCES NguoiDung(IDNguoiDung) ON DELETE SET NULL,
    FOREIGN KEY (IDMaGiamGia) REFERENCES MaGiamGia(IDMaGiamGia),
    FOREIGN KEY (IDPhuongThucThanhToan) REFERENCES PhuongThucThanhToan(IDPhuongThucThanhToan) ON DELETE SET NULL,
    FOREIGN KEY (IDTrangThai) REFERENCES TrangThaiDonHang(IDTrangThai)
);

CREATE TABLE ChiTietDonHang (
    IDChiTietDonHang INT PRIMARY KEY IDENTITY(1,1),
    IDDonHang INT NOT NULL,
    IDBienThe INT NULL,
    SoLuong INT NOT NULL,
    DonGia DECIMAL(18, 2) NOT NULL,
    TenSanPham NVARCHAR(255) NOT NULL,
    TenMau NVARCHAR(50) NULL,
    TenKichThuoc VARCHAR(50) NULL,
    FOREIGN KEY (IDDonHang) REFERENCES DonHang(IDDonHang) ON DELETE CASCADE,
    FOREIGN KEY (IDBienThe) REFERENCES BienTheSanPham(IDBienThe) ON DELETE SET NULL,
    CONSTRAINT UQ_DonHang_BienThe UNIQUE (IDDonHang, IDBienThe)
);
GO

-- =================================================================
-- PHẦN 3: TẠO INDEX VÀ RÀNG BUỘC BỔ SUNG
-- =================================================================

CREATE UNIQUE NONCLUSTERED INDEX IX_NguoiDung_SoDienThoai 
ON NguoiDung(SoDienThoai) 
WHERE SoDienThoai IS NOT NULL;
GO

-- =================================================================
-- PHẦN 4: NẠP DỮ LIỆU MẪU (SEED DATA) CHUẨN
-- =================================================================

-- 4.1. Vai trò & Trạng thái & Phương thức thanh toán
INSERT INTO VaiTro (TenVaiTro) VALUES ('Admin'), ('Customer');

INSERT INTO TrangThaiDonHang (IDTrangThai, TenTrangThai) VALUES 
(0, N'Chờ xác nhận'), 
(1, N'Đã xác nhận'), 
(2, N'Đang giao'), 
(3, N'Hoàn thành'), 
(4, N'Đã hủy');

INSERT INTO PhuongThucThanhToan (TenPhuongThuc) VALUES 
(N'Thanh toán khi nhận hàng (COD)'), 
(N'Chuyển khoản ngân hàng'), 
(N'Ví điện tử Momo');

-- 4.2. Danh mục & Thương hiệu & Màu sắc & Kích thước
INSERT INTO DanhMuc (TenDanhMuc, IDDanhMucCha) VALUES 
(N'Thời Trang Nam', NULL), 
(N'Thời Trang Nữ', NULL), 
(N'Phụ Kiện', NULL);

INSERT INTO DanhMuc (TenDanhMuc, IDDanhMucCha) VALUES 
(N'Áo Nam', 1), (N'Quần Nam', 1), (N'Đồ Mặc Ngoài (Nam)', 1),
(N'Váy & Đầm', 2), (N'Áo Nữ', 2), (N'Quần Nữ', 2),
(N'Túi Xách', 3), (N'Mũ & Nón', 3);

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
-- Giữ nguyên cả tài khoản dùng BCrypt hash và tài khoản test đơn giản của bạn để tránh lỗi đăng nhập trên web
INSERT INTO NguoiDung (HoTen, Email, SoDienThoai, MatKhauHash, IDVaiTro) VALUES
(N'Admin Account', 'admin@fashionhub.com', '0911111111', '$2a$11$7S.z/5.z/5.z/5.z/5.z/5.z/5.z/5.z/5.z/5.z/5.z/5.z/5', 1),
(N'Nguyễn Thị Lan', 'lan.nguyen@example.com', '0987654321', '$2a$11$7S.z/5.z/5.z/5.z/5.z/5.z/5.z/5.z/5.z/5.z/5.z/5.z/5', 2),
(N'Trần Văn Bình', 'binh.tran@example.com', '0912345678', '$2a$11$7S.z/5.z/5.z/5.z/5.z/5.z/5.z/5.z/5.z/5.z/5.z/5.z/5', 2),
(N'Admin Backup', 'admin@gmail.com', '0911111211', '123123', 1),
(N'K Long', 'Klong@gmail.com', '0934842323', '123123', 1);

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
INSERT INTO SanPham (TenSanPham, MoTa, Gia, IDDanhMuc, IDThuongHieu, TrangThai)
VALUES (N'Áo Thun Boxy Fit Basic', N'Form Boxy rộng rãi, thoải mái. Chất liệu cotton dày dặn.', 320000, 4, 1, 1);
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
INSERT INTO HinhAnh (DuongDan) VALUES ('/Content/images/products/aothun1_be_boxy.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Boxy_Be, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/Content/images/products/aothun1_den_boxy.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Boxy_Den, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/Content/images/products/aothun1_xam_boxy.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Boxy_Xam, 1);

-- -----------------------------------------------------------------
-- SẢN PHẨM 2: ÁO THUN REGULAR (IDDanhMuc = 4: Áo Nam)
-- -----------------------------------------------------------------
INSERT INTO SanPham (TenSanPham, MoTa, Gia, IDDanhMuc, IDThuongHieu, TrangThai)
VALUES (N'Áo Thun Regular Essential', N'Dáng Regular vừa vặn, phù hợp mặc hàng ngày.', 250000, 4, 2, 1);
DECLARE @SP_Regular INT = SCOPE_IDENTITY();

INSERT INTO BienTheSanPham (IDSanPham, IDMauSac, IDKichThuoc, SKU, Gia, SoLuongTon) VALUES 
(@SP_Regular, @MauDen, @SizeL, 'TS-REG-BLK-L', 250000, 100),
(@SP_Regular, @MauTrang, @SizeL, 'TS-REG-WHT-L', 250000, 100),
(@SP_Regular, @MauXam, @SizeL, 'TS-REG-GRY-L', 250000, 100);

DECLARE @BT_Reg_Den INT = (SELECT IDBienThe FROM BienTheSanPham WHERE SKU = 'TS-REG-BLK-L');
DECLARE @BT_Reg_Trang INT = (SELECT IDBienThe FROM BienTheSanPham WHERE SKU = 'TS-REG-WHT-L');
DECLARE @BT_Reg_Xam INT = (SELECT IDBienThe FROM BienTheSanPham WHERE SKU = 'TS-REG-GRY-L');

INSERT INTO HinhAnh (DuongDan) VALUES ('/Content/images/products/aothun2_den_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Reg_Den, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/Content/images/products/aothun2_trang_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Reg_Trang, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/Content/images/products/aothun2_xam_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Reg_Xam, 1);

-- -----------------------------------------------------------------
-- SẢN PHẨM 3: QUẦN JEAN STRAIGHT (IDDanhMuc = 5: Quần Nam)
-- -----------------------------------------------------------------
INSERT INTO SanPham (TenSanPham, MoTa, Gia, IDDanhMuc, IDThuongHieu, TrangThai)
VALUES (N'Quần Jeans Straight Xanh Đen', N'Quần bò ống đứng màu xanh đen nam tính. Vải denim không co giãn.', 550000, 5, 4, 1);
DECLARE @SP_Jean INT = SCOPE_IDENTITY();

INSERT INTO BienTheSanPham (IDSanPham, IDMauSac, IDKichThuoc, SKU, Gia, SoLuongTon) VALUES 
(@SP_Jean, @MauXanhDen, @SizeM, 'JN-STR-DBL-M', 550000, 40);
DECLARE @BT_Jean INT = SCOPE_IDENTITY();

-- Nhiều hình ảnh cho 1 biến thể
INSERT INTO HinhAnh (DuongDan) VALUES ('/Content/images/products/jean1_xanhden_straigh_truoc.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Jean, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/Content/images/products/jean1_xanhden_straigh_sau.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Jean, 0);

INSERT INTO HinhAnh (DuongDan) VALUES ('/Content/images/products/jean1_xanhden_straight_mau1.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Jean, 0);

-- -----------------------------------------------------------------
-- SẢN PHẨM 4: ÁO POLO PIQUE (IDDanhMuc = 4: Áo Nam)
-- -----------------------------------------------------------------
INSERT INTO SanPham (TenSanPham, MoTa, Gia, IDDanhMuc, IDThuongHieu, TrangThai)
VALUES (N'Áo Polo Pique Cotton', N'Áo thun có cổ vải cá sấu. Nhiều màu sắc trẻ trung.', 350000, 4, 5, 1);
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

INSERT INTO HinhAnh (DuongDan) VALUES ('/Content/images/products/polo3_tim_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Polo_Tim, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/Content/images/products/polo4_nau_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Polo_Nau, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/Content/images/products/polo3_trang_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Polo_Trang, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/Content/images/products/polo4_den_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Polo_Den, 1);

-- -----------------------------------------------------------------
-- SẢN PHẨM 5: ÁO SƠ MI OXFORD (IDDanhMuc = 4: Áo Nam)
-- -----------------------------------------------------------------
INSERT INTO SanPham (TenSanPham, MoTa, Gia, IDDanhMuc, IDThuongHieu, TrangThai)
VALUES (N'Áo Sơ Mi Oxford Regular', N'Sơ mi vải Oxford đứng form, ít nhăn. Phù hợp công sở.', 420000, 4, 2, 1);
DECLARE @SP_Somi INT = SCOPE_IDENTITY();

INSERT INTO BienTheSanPham (IDSanPham, IDMauSac, IDKichThuoc, SKU, Gia, SoLuongTon) VALUES 
(@SP_Somi, @MauHong, @SizeL, 'SM-HONG-L', 420000, 30),
(@SP_Somi, @MauTrang, @SizeL, 'SM-TRANG-L', 420000, 50),
(@SP_Somi, @MauXanhDen, @SizeL, 'SM-XD-L', 420000, 40);

DECLARE @BT_Somi_Hong INT = (SELECT IDBienThe FROM BienTheSanPham WHERE SKU = 'SM-HONG-L');
DECLARE @BT_Somi_Trang INT = (SELECT IDBienThe FROM BienTheSanPham WHERE SKU = 'SM-TRANG-L');
DECLARE @BT_Somi_XD INT = (SELECT IDBienThe FROM BienTheSanPham WHERE SKU = 'SM-XD-L');

INSERT INTO HinhAnh (DuongDan) VALUES ('/Content/images/products/somi4_hong_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Somi_Hong, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/Content/images/products/somi4_hong_regular_nguoi.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Somi_Hong, 0);

INSERT INTO HinhAnh (DuongDan) VALUES ('/Content/images/products/somi1_trang_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Somi_Trang, 1);

INSERT INTO HinhAnh (DuongDan) VALUES ('/Content/images/products/somi1_xanhden_regular.jpg');
INSERT INTO HinhAnh_BienThe (IDHinhAnh, IDBienThe, LaAnhChinh) VALUES (SCOPE_IDENTITY(), @BT_Somi_XD, 1);

USE QL_SHOPQUANAO_PRO;
GO

-- 1. Tự động kiểm tra và thêm cột VectorDacTrung vào bảng HinhAnh nếu chưa có
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'HinhAnh') AND name = 'VectorDacTrung')
BEGIN
    ALTER TABLE HinhAnh ADD VectorDacTrung NVARCHAR(MAX) NULL;
    PRINT 'Da bo sung cot VectorDacTrung vao bang HinhAnh';
END
GO

-- 2. Tự động kiểm tra và thêm cột VectorDacTrung vào bảng SanPham nếu chưa có
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SanPham') AND name = 'VectorDacTrung')
BEGIN
    ALTER TABLE SanPham ADD VectorDacTrung NVARCHAR(MAX) NULL;
    PRINT 'Da bo sung cot VectorDacTrung vao bang SanPham';
END
GO

PRINT 'Database setup and seeding completed successfully!';
GO