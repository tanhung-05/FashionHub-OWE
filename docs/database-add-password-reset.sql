USE QL_SHOPQUANAO_PRO;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.NguoiDung', 'U') IS NULL
BEGIN
    THROW 50001, 'Bang dbo.NguoiDung chua ton tai. Hay chay DB_Fixed.sql truoc.', 1;
END;

IF COL_LENGTH('dbo.NguoiDung', 'SecurityStamp') IS NULL
BEGIN
    ALTER TABLE dbo.NguoiDung
    ADD SecurityStamp UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_NguoiDung_SecurityStamp DEFAULT NEWID();
END;

IF OBJECT_ID('dbo.DatLaiMatKhauToken', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DatLaiMatKhauToken (
        IDToken BIGINT PRIMARY KEY IDENTITY(1,1),
        IDNguoiDung INT NOT NULL,
        TokenHash CHAR(64) NOT NULL,
        NgayHetHanUtc DATETIME2(0) NOT NULL,
        NgayTaoUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_DatLaiMatKhauToken_NgayTaoUtc DEFAULT SYSUTCDATETIME(),
        NgaySuDungUtc DATETIME2(0) NULL,
        DiaChiIP VARCHAR(45) NULL,
        CONSTRAINT FK_DatLaiMatKhauToken_NguoiDung
            FOREIGN KEY (IDNguoiDung)
            REFERENCES dbo.NguoiDung(IDNguoiDung)
            ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_DatLaiMatKhauToken_TokenHash'
      AND object_id = OBJECT_ID('dbo.DatLaiMatKhauToken')
)
BEGIN
    CREATE UNIQUE INDEX UX_DatLaiMatKhauToken_TokenHash
    ON dbo.DatLaiMatKhauToken(TokenHash);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_DatLaiMatKhauToken_NguoiDung_HetHan'
      AND object_id = OBJECT_ID('dbo.DatLaiMatKhauToken')
)
BEGIN
    CREATE INDEX IX_DatLaiMatKhauToken_NguoiDung_HetHan
    ON dbo.DatLaiMatKhauToken(IDNguoiDung, NgayHetHanUtc DESC)
    INCLUDE (NgaySuDungUtc);
END;

COMMIT TRANSACTION;
GO

PRINT 'Password reset schema is ready.';
GO
