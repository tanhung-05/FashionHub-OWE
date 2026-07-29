-- Fix Image Paths for .NET Core Migration
-- This script updates image paths from ~/Content/images/ or /Content/images/ to /images/
-- Run this against your FashionHub database

USE [FashionHub];
GO

PRINT 'Fixing image paths in HinhAnhSanPham table...';
GO

-- Update paths that start with ~/Content/images/ to /images/
UPDATE HinhAnhSanPham
SET DuongDan = REPLACE(DuongDan, '~/Content/images/', '/images/')
WHERE DuongDan LIKE '~/Content/images/%';

PRINT '✓ Updated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows with ~/Content/images/ prefix';
GO

-- Update paths that start with /Content/images/ to /images/
UPDATE HinhAnhSanPham
SET DuongDan = REPLACE(DuongDan, '/Content/images/', '/images/')
WHERE DuongDan LIKE '/Content/images/%';

PRINT '✓ Updated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows with /Content/images/ prefix';
GO

-- Update paths that start with Content/images/ (no leading slash/tilde) to /images/
UPDATE HinhAnhSanPham
SET DuongDan = REPLACE(DuongDan, 'Content/images/', '/images/')
WHERE DuongDan LIKE 'Content/images/%' 
  AND DuongDan NOT LIKE '/%'
  AND DuongDan NOT LIKE '~/%';

PRINT '✓ Updated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows with Content/images/ prefix (no leading char)';
GO

PRINT '';
PRINT 'Verifying updated paths...';
SELECT 
    COUNT(*) AS TotalImages,
    SUM(CASE WHEN DuongDan LIKE '/images/%' THEN 1 ELSE 0 END) AS CorrectPaths,
    SUM(CASE WHEN DuongDan LIKE '%Content%' THEN 1 ELSE 0 END) AS StillHasContentPath
FROM HinhAnhSanPham;
GO

PRINT '';
PRINT 'Sample of updated paths:';
SELECT TOP 10 
    IDHinhAnh,
    IdsanPham,
    DuongDan,
    LaChinh
FROM HinhAnhSanPham
ORDER BY IDHinhAnh;
GO

PRINT '';
PRINT '✅ Image path migration complete!';
PRINT 'All paths should now start with /images/';
GO