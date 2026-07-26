using FashionHub.Web.Data;
using FashionHub.Web.Models.Generated;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FashionHub.Tests;

public class CustomWebApplicationFactory<TProgram> 
    : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext configuration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Remove the DbContext itself
            var dbContextRegistration = services.SingleOrDefault(
                d => d.ServiceType == typeof(ApplicationDbContext));
            if (dbContextRegistration != null)
            {
                services.Remove(dbContextRegistration);
            }
            
            // Add InMemory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });
            
            // Seed data after app is built
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedTestData(db);
        });
        
        builder.UseEnvironment("Test");
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureDeleted();
        }
        base.Dispose(disposing);
    }
    
    private void SeedTestData(ApplicationDbContext db)
    {
        // Ensure database is created
        db.Database.EnsureCreated();
        
        // Add test categories
        if (!db.DanhMucs.Any())
        {
            db.DanhMucs.AddRange(
                new DanhMuc { IddanhMuc = 1, TenDanhMuc = "Áo" },
                new DanhMuc { IddanhMuc = 2, TenDanhMuc = "Quần" }
            );
        }
        
        // Add test brands
        if (!db.ThuongHieus.Any())
        {
            db.ThuongHieus.Add(new ThuongHieu
            {
                IdthuongHieu = 1,
                TenThuongHieu = "Test Brand"
            });
        }
        
        // Add test colors and sizes
        if (!db.MauSacs.Any())
        {
            db.MauSacs.Add(new MauSac
            {
                IdmauSac = 1,
                TenMau = "Đen",
                MaMauHex = "#000000"
            });
        }
        
        if (!db.KichThuocs.Any())
        {
            db.KichThuocs.Add(new KichThuoc
            {
                IdkichThuoc = 1,
                TenKichThuoc = "M"
            });
        }
        
        db.SaveChanges();
        
        // Add test products
        if (!db.SanPhams.Any())
        {
            var product = new SanPham
            {
                IdsanPham = 1,
                TenSanPham = "Test Product",
                MoTa = "Test product description",
                IddanhMuc = 1,
                IdthuongHieu = 1,
                Gia = 100000,
                TrangThai = true
            };
            db.SanPhams.Add(product);
            db.SaveChanges();
            
            // Add test variant
            db.BienTheSanPhams.Add(new BienTheSanPham
            {
                IdbienThe = 1,
                IdsanPham = 1,
                IdmauSac = 1,
                IdkichThuoc = 1,
                Sku = "TEST-001",
                Gia = 100000
            });
            
            // Add test image
            var image = new HinhAnh
            {
                IdhinhAnh = 1,
                DuongDan = "/images/products/test.jpg",
                MoTa = "Test Image"
            };
            db.HinhAnhs.Add(image);
            db.SaveChanges();
            
            db.HinhAnhBienThes.Add(new HinhAnhBienThe
            {
                IdhinhAnh = 1,
                IdbienThe = 1,
                LaAnhChinh = true
            });
        }
        
        // Add test roles
        if (!db.VaiTros.Any())
        {
            db.VaiTros.AddRange(
                new VaiTro { IdvaiTro = 1, TenVaiTro = "Customer" },
                new VaiTro { IdvaiTro = 2, TenVaiTro = "Admin" }
            );
            db.SaveChanges();
        }
        
        // Add test users
        if (!db.NguoiDungs.Any())
        {
            db.NguoiDungs.AddRange(
                new NguoiDung
                {
                    IdnguoiDung = 1,
                    Email = "test@example.com",
                    MatKhauHash = "hashed_password",
                    HoTen = "Test User",
                    SoDienThoai = "0123456789",
                    IdvaiTro = 1,
                    TrangThai = true
                },
                new NguoiDung
                {
                    IdnguoiDung = 2,
                    Email = "admin@example.com",
                    MatKhauHash = "hashed_password",
                    HoTen = "Admin User",
                    SoDienThoai = "0987654321",
                    IdvaiTro = 2,
                    TrangThai = true
                }
            );
        }
        
        // Add test coupon
        if (!db.MaGiamGia.Any())
        {
            db.MaGiamGia.Add(new MaGiamGium
            {
                IdmaGiamGia = 1,
                MaCode = "TEST10",
                TenChuongTrinh = "Test Coupon",
                GiaTri = 10000,
                GiamToiDa = 100000,
                DonHangToiThieu = 0,
                NgayBatDau = DateTime.Now.AddDays(-1),
                NgayKetThuc = DateTime.Now.AddDays(30),
                SoLuong = 100,
                DaSuDung = 0,
                TrangThai = true
            });
        }
        
        db.SaveChanges();
    }
}