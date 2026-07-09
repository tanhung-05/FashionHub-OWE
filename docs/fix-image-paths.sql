-- ===================================================================
-- Script: Fix Image Paths for ASP.NET Core Migration
-- Database: QL_SHOPQUANAO_PRO
-- Purpose: Update HinhAnh.DuongDan from old ASP.NET MVC paths to new ASP.NET Core paths
-- Date: 2026-07-09
-- ===================================================================

USE QL_SHOPQUANAO_PRO;
GO

-- ===================================================================
-- STEP 1: BACKUP & INSPECTION
-- ===================================================================

PRINT '=== STEP 1: Inspecting current image paths ===';
PRINT '';

-- Check current path formats
SELECT 
    'Current Path Formats' AS [Analysis],
    COUNT(*) AS [Count],
    LEFT(DuongDan, 20) AS [Path_Sample]
FROM HinhAnh
GROUP BY LEFT(DuongDan, 20)
ORDER BY COUNT(*) DESC;

PRINT '';
PRINT 'Total images in database:';
SELECT COUNT(*) AS [Total_Images] FROM HinhAnh;

PRINT '';
PRINT '=== Sample of current paths (first 10 records) ===';
SELECT TOP 10 
    IDHinhAnh,
    DuongDan AS [Current_Path]
FROM HinhAnh
ORDER BY IDHinhAnh;

PRINT '';
PRINT 'Press ENTER to continue to STEP 2 (or Ctrl+C to cancel)...';
PRINT '';

-- ===================================================================
-- STEP 2: CREATE BACKUP TABLE
-- ===================================================================

PRINT '=== STEP 2: Creating backup table ===';

-- Drop backup table if exists
IF OBJECT_ID('HinhAnh_Backup_20260709', 'U') IS NOT NULL
BEGIN
    DROP TABLE HinhAnh_Backup_20260709;
    PRINT 'Dropped existing backup table.';
END

-- Create backup
SELECT * 
INTO HinhAnh_Backup_20260709
FROM HinhAnh;

PRINT 'Backup created: HinhAnh_Backup_20260709';
PRINT 'Backup record count:';
SELECT COUNT(*) AS [Backup_Count] FROM HinhAnh_Backup_20260709;

PRINT '';

-- ===================================================================
-- STEP 3: UPDATE IMAGE PATHS
-- ===================================================================

PRINT '=== STEP 3: Updating image paths ===';
PRINT '';

-- Update 1: Replace /Content/Images with /images
PRINT 'Updating paths: /Content/Images/* -> /images/*';
UPDATE HinhAnh
SET DuongDan = REPLACE(DuongDan, '/Content/Images', '/images')
WHERE DuongDan LIKE '/Content/Images%';

PRINT CONCAT('Updated ', @@ROWCOUNT, ' records.');
PRINT '';

-- Update 2: Replace ~/Content/Images with /images  
PRINT 'Updating paths: ~/Content/Images/* -> /images/*';
UPDATE HinhAnh
SET DuongDan = REPLACE(DuongDan, '~/Content/Images', '/images')
WHERE DuongDan LIKE '~/Content/Images%';

PRINT CONCAT('Updated ', @@ROWCOUNT, ' records.');
PRINT '';

-- Update 3: Replace \Content\Images with /images (Windows path)
PRINT 'Updating paths: \Content\Images\* -> /images/*';
UPDATE HinhAnh
SET DuongDan = REPLACE(REPLACE(DuongDan, '\Content\Images', '/images'), '\', '/')
WHERE DuongDan LIKE '%\Content\Images%';

PRINT CONCAT('Updated ', @@ROWCOUNT, ' records.');
PRINT '';

-- Update 4: Ensure leading slash
PRINT 'Ensuring all paths start with /';
UPDATE HinhAnh
SET DuongDan = '/' + DuongDan
WHERE DuongDan NOT LIKE '/%' 
  AND DuongDan NOT LIKE '~%'
  AND DuongDan IS NOT NULL
  AND LEN(DuongDan) > 0;

PRINT CONCAT('Updated ', @@ROWCOUNT, ' records.');
PRINT '';

-- ===================================================================
-- STEP 4: VERIFICATION
-- ===================================================================

PRINT '=== STEP 4: Verification ===';
PRINT '';

PRINT '=== Updated path formats ===';
SELECT 
    'Updated Path Formats' AS [Analysis],
    COUNT(*) AS [Count],
    LEFT(DuongDan, 20) AS [Path_Sample]
FROM HinhAnh
GROUP BY LEFT(DuongDan, 20)
ORDER BY COUNT(*) DESC;

PRINT '';
PRINT '=== Sample of updated paths (first 10 records) ===';
SELECT TOP 10 
    IDHinhAnh,
    DuongDan AS [New_Path]
FROM HinhAnh
ORDER BY IDHinhAnh;

PRINT '';
PRINT '=== Check for any remaining old paths ===';
SELECT COUNT(*) AS [Old_Path_Count]
FROM HinhAnh
WHERE DuongDan LIKE '%Content/Images%' 
   OR DuongDan LIKE '%Content\Images%';

PRINT '';
PRINT '=== Before/After Comparison (first 5 records) ===';
SELECT TOP 5
    b.IDHinhAnh,
    b.DuongDan AS [Old_Path],
    h.DuongDan AS [New_Path]
FROM HinhAnh_Backup_20260709 b
INNER JOIN HinhAnh h ON b.IDHinhAnh = h.IDHinhAnh
WHERE b.DuongDan <> h.DuongDan;

PRINT '';
PRINT '===================================================================';
PRINT 'Migration Complete!';
PRINT '===================================================================';
PRINT '';
PRINT 'Summary:';
PRINT '- Backup table: HinhAnh_Backup_20260709';
PRINT '- All paths updated to ASP.NET Core format (/images/...)';
PRINT '- Refresh your browser (Ctrl+Shift+R) to see images';
PRINT '';
PRINT 'If you need to rollback:';
PRINT 'UPDATE HinhAnh SET DuongDan = b.DuongDan';
PRINT 'FROM HinhAnh h INNER JOIN HinhAnh_Backup_20260709 b ON h.IDHinhAnh = b.IDHinhAnh;';
PRINT '';
PRINT '===================================================================';

GO