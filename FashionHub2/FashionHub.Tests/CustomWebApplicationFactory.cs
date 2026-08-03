using FashionHub.Web.Data;
using FashionHub.Web.Application.Authentication;
using FashionHub.Web.Application.Email;
using FashionHub.Web.Models.Generated;
using FashionHub.Web.Services;
using FashionHub.Tests.Fakes;
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
            
            // Add InMemory database for testing — unique per factory instance
            // to avoid duplicate key conflicts when xUnit runs test classes in parallel
            var dbName = "TestDb_" + Guid.NewGuid().ToString("N");
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });

            var emailSenderDescriptor = services.SingleOrDefault(
                descriptor => descriptor.ServiceType == typeof(IEmailSender));
            if (emailSenderDescriptor != null)
            {
                services.Remove(emailSenderDescriptor);
            }

            services.AddSingleton<TestEmailSender>();
            services.AddSingleton<IEmailSender>(
                provider => provider.GetRequiredService<TestEmailSender>());

            var chatAiDescriptor = services.SingleOrDefault(
                descriptor => descriptor.ServiceType == typeof(IChatAiService));
            if (chatAiDescriptor != null)
            {
                services.Remove(chatAiDescriptor);
            }

            services.AddSingleton<IChatAiService, TestChatAiService>();
            
            // Seed data after app is built
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            SeedTestData(db, passwordHasher);
        });
        
        builder.UseEnvironment("Test");
    }
    
    private void SeedTestData(ApplicationDbContext db, IPasswordHasher passwordHasher)
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
                Gia = 100000,
                SoLuongTon = 100,
                SoLuongCanhBao = 10,
                TongDaBan = 0,
                TrangThai = true,
                RowVersion = BitConverter.GetBytes(1L)
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

            db.SanPhams.Add(new SanPham
            {
                IdsanPham = 2,
                TenSanPham = "Premium Product",
                MoTa = "Higher priced product for filtering tests",
                IddanhMuc = 2,
                IdthuongHieu = 1,
                Gia = 300000,
                TrangThai = true
            });
            db.BienTheSanPhams.Add(new BienTheSanPham
            {
                IdbienThe = 2,
                IdsanPham = 2,
                IdmauSac = 1,
                IdkichThuoc = 1,
                Sku = "TEST-002",
                Gia = 300000,
                SoLuongTon = 20,
                SoLuongCanhBao = 5,
                TongDaBan = 0,
                TrangThai = true,
                RowVersion = BitConverter.GetBytes(2L)
            });
        }
        
        // Add test roles
        if (!db.VaiTros.Any())
        {
            db.VaiTros.AddRange(
                new VaiTro { IdvaiTro = 1, TenVaiTro = "Admin" },
                new VaiTro { IdvaiTro = 2, TenVaiTro = "Customer" }
            );
            db.SaveChanges();
        }

        if (!db.TrangThaiDonHangs.Any())
        {
            db.TrangThaiDonHangs.AddRange(
                new TrangThaiDonHang { IdtrangThai = 0, TenTrangThai = "Chờ xác nhận" },
                new TrangThaiDonHang { IdtrangThai = 1, TenTrangThai = "Đã xác nhận" },
                new TrangThaiDonHang { IdtrangThai = 2, TenTrangThai = "Đang giao" },
                new TrangThaiDonHang { IdtrangThai = 3, TenTrangThai = "Hoàn thành" },
                new TrangThaiDonHang { IdtrangThai = 4, TenTrangThai = "Đã hủy" });
        }

        if (!db.PhuongThucThanhToans.Any())
        {
            db.PhuongThucThanhToans.Add(new PhuongThucThanhToan
            {
                IdphuongThucThanhToan = 1,
                MaPhuongThuc = "COD",
                TenPhuongThuc = "Thanh toán khi nhận hàng (COD)",
                TrangThai = true
            });
        }

        db.SaveChanges();
        
        // Add test users
        if (!db.NguoiDungs.Any())
        {
            db.NguoiDungs.AddRange(
                new NguoiDung
                {
                    IdnguoiDung = 1,
                    Email = "test@example.com",
                    MatKhauHash = passwordHasher.Hash("Test123!"),
                    HoTen = "Test User",
                    SoDienThoai = "0123456789",
                    IdvaiTro = 2,
                    TrangThai = true
                },
                new NguoiDung
                {
                    IdnguoiDung = 2,
                    Email = "admin@example.com",
                    MatKhauHash = passwordHasher.Hash("Test123!"),
                    HoTen = "Admin User",
                    SoDienThoai = "0987654321",
                    IdvaiTro = 1,
                    TrangThai = true
                }
            );
        }

        db.SaveChanges();

        if (!db.DiaChis.Any())
        {
            db.DiaChis.Add(new DiaChi
            {
                IddiaChi = 1,
                IdnguoiDung = 1,
                TenNguoiNhan = "Test User",
                SoDienThoai = "0123456789",
                ChiTiet = "123 Test Street",
                PhuongXa = "Test Ward",
                QuanHuyen = "Test District",
                TinhThanh = "Test City",
                LaMacDinh = true,
                NgayTao = DateTime.Now
            });
        }
        
        // Add test coupon
        if (!db.MaGiamGia.Any())
        {
            db.MaGiamGia.Add(new MaGiamGium
            {
                IdmaGiamGia = 1,
                MaCode = "TEST10",
                TenChuongTrinh = "Test Coupon",
                LoaiGiamGia = 1,
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
