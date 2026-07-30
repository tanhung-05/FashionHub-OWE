# Database Migration Skill

## Purpose
Handle Entity Framework Core migrations safely and systematically.

## When to Use
- Database schema changes needed
- Adding new tables or columns
- Modifying existing schema
- Scaffolding from existing database

## Database-First Approach (Current Project)

### Initial Scaffold from Existing Database
```powershell
cd FashionHub2/FashionHub.Web

# Scaffold entire database
dotnet ef dbcontext scaffold "Server=localhost;Database=FashionHub;User Id=sa;Password=YourPassword;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models/Generated -c ApplicationDbContext --context-dir Data --force

# Scaffold specific tables only
dotnet ef dbcontext scaffold "ConnectionString" Microsoft.EntityFrameworkCore.SqlServer -o Models/Generated -c ApplicationDbContext --context-dir Data --tables SanPham,DonHang,NguoiDung --force
```

### When Database Schema Changes

#### 1. Update Database First (SQL Script)
```sql
-- Example: Add new column
ALTER TABLE SanPham ADD MoTaNgan NVARCHAR(500) NULL;

-- Example: Add new table
CREATE TABLE YeuThich (
    IDYeuThich INT PRIMARY KEY IDENTITY(1,1),
    IDNguoiDung INT NOT NULL,
    IDSanPham INT NOT NULL,
    NgayThem DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (IDNguoiDung) REFERENCES NguoiDung(IDNguoiDung),
    FOREIGN KEY (IDSanPham) REFERENCES SanPham(IDSanPham)
);
```

#### 2. Re-scaffold Models
```powershell
cd FashionHub2/FashionHub.Web

# Re-scaffold (will overwrite generated models)
dotnet ef dbcontext scaffold "Server=localhost;Database=FashionHub;User Id=sa;Password=YourPassword;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models/Generated -c ApplicationDbContext --context-dir Data --force
```

#### 3. Review Generated Code
```powershell
# Check what changed
git diff FashionHub2/FashionHub.Web/Models/Generated/
git diff FashionHub2/FashionHub.Web/Data/ApplicationDbContext.cs
```

#### 4. Update Related Code
- ViewModels if needed
- Controllers using affected models
- Views displaying affected data

## Code-First Migrations (If Switching Approach)

### Create Migration
```powershell
cd FashionHub2/FashionHub.Web

# Create migration
dotnet ef migrations add AddWishlistTable

# Review generated migration
# Check: FashionHub.Web/Migrations/YYYYMMDDHHMMSS_AddWishlistTable.cs
```

### Apply Migration
```powershell
# Apply to database
dotnet ef database update

# Apply specific migration
dotnet ef database update AddWishlistTable

# Rollback to previous migration
dotnet ef database update PreviousMigrationName
```

### Remove Migration (Not Yet Applied)
```powershell
dotnet ef migrations remove
```

## Database Management

### Connection String Management

#### Development (User Secrets)
```powershell
cd FashionHub2/FashionHub.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=FashionHub;User Id=sa;Password=DevPassword;TrustServerCertificate=True;"
```

#### Production (Environment Variable)
```powershell
# Windows
$env:ConnectionStrings__DefaultConnection="Server=prod-server;Database=FashionHub;User Id=sa;Password=ProdPassword;TrustServerCertificate=True;"

# Docker
# Set in docker-compose.yml or .env file
```

### Database Operations

#### Check Current Database
```powershell
# List migrations
dotnet ef migrations list

# Get connection string (without password)
dotnet ef dbcontext info
```

#### Create Database Backup
```powershell
# Using SQL Server Management Studio
# Or via command line
sqlcmd -S localhost -U sa -P "Password" -Q "BACKUP DATABASE FashionHub TO DISK='C:\Backups\FashionHub_backup.bak'"
```

#### Restore Database
```powershell
sqlcmd -S localhost -U sa -P "Password" -Q "RESTORE DATABASE FashionHub FROM DISK='C:\Backups\FashionHub_backup.bak' WITH REPLACE"
```

### Database Indexes (Performance)

#### Review Missing Indexes
```sql
-- See: docs/database-indexes-production.sql
CREATE INDEX IX_SanPham_TrangThai ON SanPham(TrangThai);
CREATE INDEX IX_DonHang_NgayTao ON DonHang(NgayTao);
CREATE INDEX IX_DonHang_IDTrangThai ON DonHang(IDTrangThai);
CREATE INDEX IX_ChiTietDonHang_IDDonHang ON ChiTietDonHang(IDDonHang);
CREATE INDEX IX_BienThe_IDSanPham ON BienTheSanPham(IDSanPham);
CREATE INDEX IX_HinhAnhSanPham_IDSanPham ON HinhAnhSanPham(IDSanPham);
```

#### Apply Indexes
```powershell
# Run SQL script
sqlcmd -S localhost -U sa -P "Password" -d FashionHub -i docs/database-indexes-production.sql

# Or execute in SSMS
```

## Schema Changes Workflow

### Adding New Table

#### 1. Design Schema
```sql
-- Design new table
CREATE TABLE YeuThich (
    IDYeuThich INT PRIMARY KEY IDENTITY(1,1),
    IDNguoiDung INT NOT NULL,
    IDSanPham INT NOT NULL,
    NgayThem DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_YeuThich_NguoiDung FOREIGN KEY (IDNguoiDung) 
        REFERENCES NguoiDung(IDNguoiDung),
    CONSTRAINT FK_YeuThich_SanPham FOREIGN KEY (IDSanPham) 
        REFERENCES SanPham(IDSanPham)
);
```

#### 2. Create in Database
```powershell
# Execute SQL script
sqlcmd -S localhost -U sa -P "Password" -d FashionHub -i add-wishlist-table.sql
```

#### 3. Scaffold New Model
```powershell
cd FashionHub2/FashionHub.Web
dotnet ef dbcontext scaffold "ConnectionString" Microsoft.EntityFrameworkCore.SqlServer -o Models/Generated -c ApplicationDbContext --context-dir Data --force
```

#### 4. Create ViewModel
```csharp
// ViewModels/WishlistViewModel.cs
namespace FashionHub.Web.ViewModels
{
    public class WishlistViewModel
    {
        public int IDYeuThich { get; set; }
        public int IDSanPham { get; set; }
        public string TenSanPham { get; set; }
        public decimal GiaBan { get; set; }
        public string HinhAnh { get; set; }
        public DateTime NgayThem { get; set; }
    }
}
```

#### 5. Create Controller
```csharp
// Controllers/WishlistController.cs
public class WishlistController : Controller
{
    private readonly ApplicationDbContext _context;

    public WishlistController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var wishlist = await _context.YeuThiches
            .Include(y => y.IdSanPhamNavigation)
            .Where(y => y.IdNguoiDung == userId)
            .ToListAsync();
        
        return View(wishlist);
    }
}
```

#### 6. Create Views
```cshtml
<!-- Views/Wishlist/Index.cshtml -->
@model IEnumerable<YeuThich>

<h1>Danh sách yêu thích</h1>
<!-- View implementation -->
```

#### 7. Add Tests
```csharp
// FashionHub.Tests/Controllers/WishlistControllerTests.cs
public class WishlistControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    [Fact]
    public async Task Index_WithAuth_ReturnsWishlist()
    {
        // Test implementation
    }
}
```

### Modifying Existing Table

#### 1. Backup First
```powershell
sqlcmd -S localhost -U sa -P "Password" -Q "BACKUP DATABASE FashionHub TO DISK='C:\Backups\FashionHub_before_change.bak'"
```

#### 2. Create ALTER Script
```sql
-- alter-sanpham.sql
ALTER TABLE SanPham ADD MoTaNgan NVARCHAR(500) NULL;
ALTER TABLE SanPham ADD LuotXem INT NOT NULL DEFAULT 0;
```

#### 3. Test on Development Database
```powershell
# Apply to dev database
sqlcmd -S localhost -U sa -P "DevPassword" -d FashionHub -i alter-sanpham.sql
```

#### 4. Re-scaffold Models
```powershell
cd FashionHub2/FashionHub.Web
dotnet ef dbcontext scaffold "ConnectionString" Microsoft.EntityFrameworkCore.SqlServer -o Models/Generated -c ApplicationDbContext --context-dir Data --force
```

#### 5. Update Code
```csharp
// Update controllers, views, viewmodels to use new columns
public async Task<IActionResult> Details(int id)
{
    var product = await _context.SanPhams.FindAsync(id);
    
    // Increment view count (new column)
    product.LuotXem++;
    await _context.SaveChangesAsync();
    
    return View(product);
}
```

#### 6. Test Thoroughly
```powershell
dotnet build
dotnet test
```

#### 7. Document Changes
```markdown
# Database Schema Changes

## Date: 2026-07-29

### Added to SanPham table:
- `MoTaNgan` NVARCHAR(500) - Short product description
- `LuotXem` INT - View counter

### Impacted Areas:
- ProductsController.Details() - Added view count increment
- Product details view - Display short description
- Admin product create/edit - Added MoTaNgan field
```

## Troubleshooting

### Scaffold Fails
```powershell
# Error: Unable to connect to database
# Solution: Check connection string, SQL Server running

# Error: Tables not found
# Solution: Verify database name, table names

# Error: Permission denied
# Solution: Check SQL user permissions
```

### Migration Conflicts
```powershell
# Error: Migration already applied
# Solution: Check migrations list
dotnet ef migrations list

# Remove pending migration
dotnet ef migrations remove
```

### Data Loss Prevention
```powershell
# Always backup before schema changes
sqlcmd -S localhost -U sa -P "Password" -Q "BACKUP DATABASE FashionHub TO DISK='backup.bak'"

# Test on dev database first
# Never run untested migrations on production
```

## Docker Database

### Initialize Database in Docker
```yaml
# docker-compose.yml already includes SQL Server
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - SA_PASSWORD=YourStrong@Passw0rd
    volumes:
      - sqlserver_data:/var/opt/mssql
```

### Run Initialization Script
```bash
# Copy SQL script to container
docker cp DB_Fixed.sql fashionhub-sqlserver:/tmp/

# Execute script
docker exec -it fashionhub-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -i /tmp/DB_Fixed.sql
```

### Access Database from Container
```powershell
# Connect to SQL Server in container
docker exec -it fashionhub-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd'

# Run query
SELECT * FROM SanPham;
GO
```

## Production Deployment

### Pre-Deployment
1. ✅ Backup production database
2. ✅ Test migration on staging database
3. ✅ Review all schema changes
4. ✅ Plan rollback procedure
5. ✅ Schedule maintenance window

### Deployment
```powershell
# 1. Stop application
# 2. Backup database
sqlcmd -S prod-server -U sa -P "ProdPassword" -Q "BACKUP DATABASE FashionHub TO DISK='backup.bak'"

# 3. Apply schema changes
sqlcmd -S prod-server -U sa -P "ProdPassword" -d FashionHub -i schema-changes.sql

# 4. Start application
# 5. Verify health check
curl https://fashionhub.com/health
```

### Rollback Plan
```powershell
# If deployment fails:
# 1. Stop application
# 2. Restore database from backup
sqlcmd -S prod-server -U sa -P "ProdPassword" -Q "RESTORE DATABASE FashionHub FROM DISK='backup.bak' WITH REPLACE"

# 3. Deploy previous application version
# 4. Verify health check
```
