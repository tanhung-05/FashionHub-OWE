/*
    FashionHub - idempotent chat history upgrade
    Apply to the target FashionHub database after taking a backup.
    This script creates only missing chat objects and does not delete existing data.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.CuocTroChuyen', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.CuocTroChuyen (
            IDCuocTroChuyen UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_CuocTroChuyen PRIMARY KEY,
            IDNguoiDung INT NOT NULL,
            NgayTao DATETIME2(0) NOT NULL
                CONSTRAINT DF_CuocTroChuyen_NgayTao DEFAULT SYSUTCDATETIME(),
            NgayCapNhat DATETIME2(0) NOT NULL
                CONSTRAINT DF_CuocTroChuyen_NgayCapNhat DEFAULT SYSUTCDATETIME(),
            NgayKetThuc DATETIME2(0) NULL,
            CONSTRAINT FK_CuocTroChuyen_NguoiDung FOREIGN KEY (IDNguoiDung)
                REFERENCES dbo.NguoiDung(IDNguoiDung) ON DELETE CASCADE
        );
    END;

    IF OBJECT_ID(N'dbo.TinNhanChat', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.TinNhanChat (
            IDTinNhan BIGINT NOT NULL IDENTITY(1,1)
                CONSTRAINT PK_TinNhanChat PRIMARY KEY,
            IDCuocTroChuyen UNIQUEIDENTIFIER NOT NULL,
            VaiTro VARCHAR(20) NOT NULL,
            NoiDung NVARCHAR(2000) NOT NULL,
            DuLieuJson NVARCHAR(MAX) NULL,
            NgayTao DATETIME2(0) NOT NULL
                CONSTRAINT DF_TinNhanChat_NgayTao DEFAULT SYSUTCDATETIME(),
            CONSTRAINT FK_TinNhanChat_CuocTroChuyen
                FOREIGN KEY (IDCuocTroChuyen)
                REFERENCES dbo.CuocTroChuyen(IDCuocTroChuyen) ON DELETE CASCADE,
            CONSTRAINT CK_TinNhanChat_VaiTro
                CHECK (VaiTro IN ('user', 'assistant')),
            CONSTRAINT CK_TinNhanChat_DuLieuJson
                CHECK (DuLieuJson IS NULL OR ISJSON(DuLieuJson) = 1)
        );
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UX_CuocTroChuyen_DangHoatDong'
          AND object_id = OBJECT_ID(N'dbo.CuocTroChuyen')
    )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_CuocTroChuyen_DangHoatDong
        ON dbo.CuocTroChuyen(IDNguoiDung)
        WHERE NgayKetThuc IS NULL;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_CuocTroChuyen_NguoiDung_NgayCapNhat'
          AND object_id = OBJECT_ID(N'dbo.CuocTroChuyen')
    )
    BEGIN
        CREATE INDEX IX_CuocTroChuyen_NguoiDung_NgayCapNhat
        ON dbo.CuocTroChuyen(IDNguoiDung, NgayCapNhat DESC);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_TinNhanChat_CuocTroChuyen_NgayTao'
          AND object_id = OBJECT_ID(N'dbo.TinNhanChat')
    )
    BEGIN
        CREATE INDEX IX_TinNhanChat_CuocTroChuyen_NgayTao
        ON dbo.TinNhanChat(IDCuocTroChuyen, NgayTao, IDTinNhan);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
