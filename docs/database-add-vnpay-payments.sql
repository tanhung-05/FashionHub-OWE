/*
    FashionHub - idempotent VNPAY payment upgrade
    Back up the target database before applying this script.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'dbo.PhuongThucThanhToan', N'MaPhuongThuc') IS NULL
    BEGIN
        ALTER TABLE dbo.PhuongThucThanhToan
            ADD MaPhuongThuc VARCHAR(30) NULL;

        EXEC sys.sp_executesql N'
            UPDATE dbo.PhuongThucThanhToan
            SET MaPhuongThuc = CASE IDPhuongThucThanhToan
                WHEN 1 THEN ''COD''
                WHEN 2 THEN ''VNPAY''
                WHEN 3 THEN ''MOMO''
                ELSE CONCAT(''METHOD_'', IDPhuongThucThanhToan)
            END;

            ALTER TABLE dbo.PhuongThucThanhToan
                ALTER COLUMN MaPhuongThuc VARCHAR(30) NOT NULL;';
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'UQ_PhuongThucThanhToan_MaPhuongThuc'
          AND object_id = OBJECT_ID(N'dbo.PhuongThucThanhToan')
    )
    BEGIN
        EXEC sys.sp_executesql N'
            CREATE UNIQUE INDEX UQ_PhuongThucThanhToan_MaPhuongThuc
                ON dbo.PhuongThucThanhToan(MaPhuongThuc);';
    END;

    EXEC sys.sp_executesql N'
        UPDATE dbo.PhuongThucThanhToan
        SET MaPhuongThuc = ''COD'',
            TenPhuongThuc = N''Thanh toán khi nhận hàng (COD)'',
            TrangThai = 1
        WHERE IDPhuongThucThanhToan = 1;

        UPDATE dbo.PhuongThucThanhToan
        SET MaPhuongThuc = ''VNPAY'',
            TenPhuongThuc = N''Thanh toán ngân hàng qua VNPAY'',
            TrangThai = 1
        WHERE IDPhuongThucThanhToan = 2;

        UPDATE dbo.PhuongThucThanhToan
        SET MaPhuongThuc = ''MOMO'', TrangThai = 0
        WHERE IDPhuongThucThanhToan = 3;';

    IF COL_LENGTH(N'dbo.DonHang', N'TrangThaiThanhToan') IS NULL
    BEGIN
        EXEC sys.sp_executesql N'
            ALTER TABLE dbo.DonHang ADD TrangThaiThanhToan TINYINT NOT NULL
                CONSTRAINT DF_DonHang_TrangThaiThanhToan DEFAULT 0 WITH VALUES;';
        EXEC sys.sp_executesql N'
            ALTER TABLE dbo.DonHang ADD CONSTRAINT CK_DonHang_TrangThaiThanhToan
                CHECK (TrangThaiThanhToan BETWEEN 0 AND 4);';
    END;

    IF COL_LENGTH(N'dbo.DonHang', N'NgayThanhToan') IS NULL
    BEGIN
        ALTER TABLE dbo.DonHang ADD NgayThanhToan DATETIME2(0) NULL;
    END;

    IF OBJECT_ID(N'dbo.GiaoDichThanhToan', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.GiaoDichThanhToan (
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
                REFERENCES dbo.DonHang(IDDonHang) ON DELETE CASCADE,
            CONSTRAINT CK_GiaoDichThanhToan_SoTien CHECK (SoTien > 0),
            CONSTRAINT CK_GiaoDichThanhToan_TrangThai CHECK (TrangThai BETWEEN 1 AND 4)
        );
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_GiaoDichThanhToan_DonHang_NgayTao'
          AND object_id = OBJECT_ID(N'dbo.GiaoDichThanhToan')
    )
    BEGIN
        EXEC sys.sp_executesql N'
            CREATE INDEX IX_GiaoDichThanhToan_DonHang_NgayTao
                ON dbo.GiaoDichThanhToan(IDDonHang, NgayTao DESC);';
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_GiaoDichThanhToan_TrangThai_NgayTao'
          AND object_id = OBJECT_ID(N'dbo.GiaoDichThanhToan')
    )
    BEGIN
        EXEC sys.sp_executesql N'
            CREATE INDEX IX_GiaoDichThanhToan_TrangThai_NgayTao
                ON dbo.GiaoDichThanhToan(TrangThai, NgayTao DESC);';
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
