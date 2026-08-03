using System;
using System.Collections.Generic;
using FashionHub.Web.Models.Generated;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdminActivityLog> AdminActivityLogs { get; set; }

    public virtual DbSet<BienTheSanPham> BienTheSanPhams { get; set; }

    public virtual DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }

    public virtual DbSet<CuocTroChuyen> CuocTroChuyens { get; set; }

    public virtual DbSet<DanhMuc> DanhMucs { get; set; }

    public virtual DbSet<DanhGia> DanhGia { get; set; }

    public virtual DbSet<DatLaiMatKhauToken> DatLaiMatKhauTokens { get; set; }

    public virtual DbSet<DiaChi> DiaChis { get; set; }

    public virtual DbSet<DonHang> DonHangs { get; set; }

    public virtual DbSet<GiaoDichThanhToan> GiaoDichThanhToans { get; set; }

    public virtual DbSet<GioHang> GioHangs { get; set; }

    public virtual DbSet<HinhAnh> HinhAnhs { get; set; }

    public virtual DbSet<HinhAnhBienThe> HinhAnhBienThes { get; set; }

    public virtual DbSet<KichThuoc> KichThuocs { get; set; }

    public virtual DbSet<LichSuDonHang> LichSuDonHangs { get; set; }

    public virtual DbSet<LichSuTonKho> LichSuTonKhos { get; set; }

    public virtual DbSet<MaGiamGium> MaGiamGia { get; set; }

    public virtual DbSet<MauSac> MauSacs { get; set; }

    public virtual DbSet<NguoiDung> NguoiDungs { get; set; }

    public virtual DbSet<PhuongThucThanhToan> PhuongThucThanhToans { get; set; }

    public virtual DbSet<SanPham> SanPhams { get; set; }

    public virtual DbSet<ThuongHieu> ThuongHieus { get; set; }

    public virtual DbSet<TinNhanChat> TinNhanChats { get; set; }

    public virtual DbSet<TrangThaiDonHang> TrangThaiDonHangs { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    public virtual DbSet<YeuThich> YeuThiches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminActivityLog>(entity =>
        {
            entity.HasKey(e => e.Idlog).HasName("PK_AdminActivityLog");

            entity.ToTable("AdminActivityLog");

            entity.HasIndex(e => new { e.Idadmin, e.NgayTao }, "IX_AdminActivityLog_Admin_NgayTao");

            entity.Property(e => e.Idlog).HasColumnName("IDLog");
            entity.Property(e => e.DiaChiIp)
                .HasMaxLength(45)
                .IsUnicode(false)
                .HasColumnName("DiaChiIP");
            entity.Property(e => e.DuLieuCu).HasColumnType("nvarchar(max)");
            entity.Property(e => e.DuLieuMoi).HasColumnType("nvarchar(max)");
            entity.Property(e => e.HanhDong).HasMaxLength(100);
            entity.Property(e => e.Idadmin).HasColumnName("IDAdmin");
            entity.Property(e => e.IdbanGhi)
                .HasMaxLength(100)
                .HasColumnName("IDBanGhi");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnType("datetime2(0)");
            entity.Property(e => e.TenBang).HasMaxLength(100);

            entity.HasOne(d => d.IdadminNavigation).WithMany(p => p.AdminActivityLogs)
                .HasForeignKey(d => d.Idadmin)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_AdminActivityLog_NguoiDung");
        });

        modelBuilder.Entity<BienTheSanPham>(entity =>
        {
            entity.HasKey(e => e.IdbienThe).HasName("PK_BienTheSanPham");

            entity.ToTable("BienTheSanPham");

            entity.HasIndex(e => new { e.IdsanPham, e.TrangThai, e.DeletedAt }, "IX_BienTheSanPham_SanPham_TrangThai");

            entity.HasIndex(e => e.Sku, "UQ_BienTheSanPham_SKU").IsUnique();

            entity.Property(e => e.IdbienThe).HasColumnName("IDBienThe");
            entity.Property(e => e.DeletedAt).HasColumnType("datetime2(0)");
            entity.Property(e => e.Gia)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 0)");
            entity.Property(e => e.IdkichThuoc).HasColumnName("IDKichThuoc");
            entity.Property(e => e.IdmauSac).HasColumnName("IDMauSac");
            entity.Property(e => e.IdsanPham).HasColumnName("IDSanPham");
            entity.Property(e => e.NgayCapNhat).HasColumnType("datetime2(0)");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnType("datetime2(0)");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.SoLuongCanhBao).HasDefaultValue(10);
            entity.Property(e => e.Sku)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("SKU");
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.IdkichThuocNavigation).WithMany(p => p.BienTheSanPhams)
                .HasForeignKey(d => d.IdkichThuoc)
                .HasConstraintName("FK_BienTheSanPham_KichThuoc");

            entity.HasOne(d => d.IdmauSacNavigation).WithMany(p => p.BienTheSanPhams)
                .HasForeignKey(d => d.IdmauSac)
                .HasConstraintName("FK_BienTheSanPham_MauSac");

            entity.HasOne(d => d.IdsanPhamNavigation).WithMany(p => p.BienTheSanPhams)
                .HasForeignKey(d => d.IdsanPham)
                .HasConstraintName("FK_BienTheSanPham_SanPham");
        });

        modelBuilder.Entity<DatLaiMatKhauToken>(entity =>
        {
            entity.HasKey(e => e.Idtoken).HasName("PK_DatLaiMatKhauToken");

            entity.ToTable("DatLaiMatKhauToken");

            entity.HasIndex(
                e => new { e.IdnguoiDung, e.NgayHetHanUtc },
                "IX_DatLaiMatKhauToken_NguoiDung_HetHan")
                .IsDescending(false, true);

            entity.HasIndex(e => e.TokenHash, "UX_DatLaiMatKhauToken_TokenHash")
                .IsUnique();

            entity.Property(e => e.Idtoken).HasColumnName("IDToken");
            entity.Property(e => e.IdnguoiDung).HasColumnName("IDNguoiDung");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(64)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.NgayHetHanUtc).HasColumnType("datetime2(0)");
            entity.Property(e => e.NgayTaoUtc)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnType("datetime2(0)");
            entity.Property(e => e.NgaySuDungUtc).HasColumnType("datetime2(0)");
            entity.Property(e => e.DiaChiIp)
                .HasMaxLength(45)
                .IsUnicode(false)
                .HasColumnName("DiaChiIP");

            entity.HasOne(d => d.IdnguoiDungNavigation)
                .WithMany(p => p.DatLaiMatKhauTokens)
                .HasForeignKey(d => d.IdnguoiDung)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_DatLaiMatKhauToken_NguoiDung");
        });

        modelBuilder.Entity<ChiTietDonHang>(entity =>
        {
            entity.HasKey(e => e.IdchiTietDonHang).HasName("PK_ChiTietDonHang");

            entity.ToTable("ChiTietDonHang");

            entity.HasIndex(e => e.IdbienThe, "IX_ChiTietDonHang_BienThe");

            entity.HasIndex(e => new { e.IddonHang, e.IdbienThe }, "UQ_ChiTietDonHang_DonHang_BienThe").IsUnique();

            entity.Property(e => e.IdchiTietDonHang).HasColumnName("IDChiTietDonHang");
            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.IdbienThe).HasColumnName("IDBienThe");
            entity.Property(e => e.IddonHang).HasColumnName("IDDonHang");
            entity.Property(e => e.TenKichThuoc)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TenMau).HasMaxLength(50);
            entity.Property(e => e.TenSanPham).HasMaxLength(255);

            entity.HasOne(d => d.IdbienTheNavigation).WithMany(p => p.ChiTietDonHangs)
                .HasForeignKey(d => d.IdbienThe)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ChiTietDonHang_BienTheSanPham");

            entity.HasOne(d => d.IddonHangNavigation).WithMany(p => p.ChiTietDonHangs)
                .HasForeignKey(d => d.IddonHang)
                .HasConstraintName("FK_ChiTietDonHang_DonHang");
        });

        modelBuilder.Entity<CuocTroChuyen>(entity =>
        {
            entity.HasKey(e => e.IdcuocTroChuyen).HasName("PK_CuocTroChuyen");

            entity.ToTable("CuocTroChuyen");

            entity.HasIndex(e => e.IdnguoiDung, "UX_CuocTroChuyen_DangHoatDong")
                .IsUnique()
                .HasFilter("([NgayKetThuc] IS NULL)");

            entity.HasIndex(
                e => new { e.IdnguoiDung, e.NgayCapNhat },
                "IX_CuocTroChuyen_NguoiDung_NgayCapNhat")
                .IsDescending(false, true);

            entity.Property(e => e.IdcuocTroChuyen)
                .ValueGeneratedNever()
                .HasColumnName("IDCuocTroChuyen");
            entity.Property(e => e.IdnguoiDung).HasColumnName("IDNguoiDung");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnType("datetime2(0)");
            entity.Property(e => e.NgayCapNhat)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnType("datetime2(0)");
            entity.Property(e => e.NgayKetThuc).HasColumnType("datetime2(0)");

            entity.HasOne(d => d.IdnguoiDungNavigation)
                .WithMany(p => p.CuocTroChuyens)
                .HasForeignKey(d => d.IdnguoiDung)
                .HasConstraintName("FK_CuocTroChuyen_NguoiDung");
        });

        modelBuilder.Entity<DanhMuc>(entity =>
        {
            entity.HasKey(e => e.IddanhMuc).HasName("PK_DanhMuc");

            entity.ToTable("DanhMuc");

            entity.HasIndex(e => e.Slug, "UX_DanhMuc_Slug")
                .IsUnique()
                .HasFilter("([Slug] IS NOT NULL AND [DeletedAt] IS NULL)");

            entity.Property(e => e.DeletedAt).HasColumnType("datetime2(0)");
            entity.Property(e => e.IddanhMuc).HasColumnName("IDDanhMuc");
            entity.Property(e => e.IddanhMucCha).HasColumnName("IDDanhMucCha");
            entity.Property(e => e.Slug).HasMaxLength(100);
            entity.Property(e => e.TenDanhMuc).HasMaxLength(100);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.IddanhMucChaNavigation).WithMany(p => p.InverseIddanhMucChaNavigation)
                .HasForeignKey(d => d.IddanhMucCha)
                .HasConstraintName("FK_DanhMuc_DanhMucCha");
        });

        modelBuilder.Entity<DanhGia>(entity =>
        {
            entity.HasKey(e => e.IddanhGia).HasName("PK_DanhGia");

            entity.ToTable("DanhGia");

            entity.HasIndex(e => new { e.IdsanPham, e.TrangThai, e.DeletedAt }, "IX_DanhGia_SanPham_TrangThai");

            entity.HasIndex(e => new { e.IdnguoiDung, e.IdsanPham }, "UQ_DanhGia_NguoiDung_SanPham").IsUnique();

            entity.Property(e => e.IddanhGia).HasColumnName("IDDanhGia");
            entity.Property(e => e.DeletedAt).HasColumnType("datetime2(0)");
            entity.Property(e => e.IdchiTietDonHang).HasColumnName("IDChiTietDonHang");
            entity.Property(e => e.IdnguoiDung).HasColumnName("IDNguoiDung");
            entity.Property(e => e.IdsanPham).HasColumnName("IDSanPham");
            entity.Property(e => e.NgayCapNhat).HasColumnType("datetime2(0)");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnType("datetime2(0)");
            entity.Property(e => e.NoiDung).HasMaxLength(2000);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.IdchiTietDonHangNavigation).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.IdchiTietDonHang)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_DanhGia_ChiTietDonHang");

            entity.HasOne(d => d.IdnguoiDungNavigation).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.IdnguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DanhGia_NguoiDung");

            entity.HasOne(d => d.IdsanPhamNavigation).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.IdsanPham)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DanhGia_SanPham");
        });

        modelBuilder.Entity<DiaChi>(entity =>
        {
            entity.HasKey(e => e.IddiaChi).HasName("PK_DiaChi");

            entity.ToTable("DiaChi");

            entity.HasIndex(e => e.IdnguoiDung, "UX_DiaChi_MacDinh")
                .IsUnique()
                .HasFilter("([LaMacDinh] = CONVERT([bit],(1)))");

            entity.Property(e => e.IddiaChi).HasColumnName("IDDiaChi");
            entity.Property(e => e.ChiTiet).HasMaxLength(255);
            entity.Property(e => e.IdnguoiDung).HasColumnName("IDNguoiDung");
            entity.Property(e => e.LaMacDinh).HasDefaultValue(false);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnType("datetime2(0)");
            entity.Property(e => e.PhuongXa).HasMaxLength(100);
            entity.Property(e => e.QuanHuyen).HasMaxLength(100);
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.TenNguoiNhan).HasMaxLength(100);
            entity.Property(e => e.TinhThanh).HasMaxLength(100);

            entity.HasOne(d => d.IdnguoiDungNavigation).WithMany(p => p.DiaChis)
                .HasForeignKey(d => d.IdnguoiDung)
                .HasConstraintName("FK_DiaChi_NguoiDung");
        });

        modelBuilder.Entity<DonHang>(entity =>
        {
            entity.HasKey(e => e.IddonHang).HasName("PK_DonHang");

            entity.ToTable("DonHang");

            entity.HasIndex(e => new { e.IdnguoiDung, e.NgayTao }, "IX_DonHang_NguoiDung_NgayTao");

            entity.HasIndex(e => new { e.IdtrangThai, e.NgayTao }, "IX_DonHang_TrangThai_NgayTao");

            entity.Property(e => e.IddonHang).HasColumnName("IDDonHang");
            entity.Property(e => e.DiaChiGiao).HasMaxLength(500);
            entity.Property(e => e.GhiChu).HasMaxLength(500);
            entity.Property(e => e.IdmaGiamGia).HasColumnName("IDMaGiamGia");
            entity.Property(e => e.IdnguoiDung).HasColumnName("IDNguoiDung");
            entity.Property(e => e.IdphuongThucThanhToan).HasColumnName("IDPhuongThucThanhToan");
            entity.Property(e => e.IdtrangThai).HasColumnName("IDTrangThai");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnType("datetime2(0)");
            entity.Property(e => e.NgayCapNhat).HasColumnType("datetime2(0)");
            entity.Property(e => e.NgayThanhToan).HasColumnType("datetime2(0)");
            entity.Property(e => e.PhiVanChuyen)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 0)");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.TenNguoiNhan).HasMaxLength(100);
            entity.Property(e => e.TienGiamGia)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 0)");
            entity.Property(e => e.TongThanhToan).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.TongTienHang).HasColumnType("decimal(18, 0)");

            entity.HasOne(d => d.IdmaGiamGiaNavigation).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.IdmaGiamGia)
                .HasConstraintName("FK_DonHang_MaGiamGia");

            entity.HasOne(d => d.IdnguoiDungNavigation).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.IdnguoiDung)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_DonHang_NguoiDung");

            entity.HasOne(d => d.IdphuongThucThanhToanNavigation).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.IdphuongThucThanhToan)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_DonHang_PhuongThucThanhToan");

            entity.HasOne(d => d.IdtrangThaiNavigation).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.IdtrangThai)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DonHang_TrangThaiDonHang");
        });

        modelBuilder.Entity<GiaoDichThanhToan>(entity =>
        {
            entity.HasKey(e => e.IdgiaoDich).HasName("PK_GiaoDichThanhToan");

            entity.ToTable("GiaoDichThanhToan");

            entity.HasIndex(e => new { e.IddonHang, e.NgayTao }, "IX_GiaoDichThanhToan_DonHang_NgayTao");

            entity.HasIndex(e => new { e.TrangThai, e.NgayTao }, "IX_GiaoDichThanhToan_TrangThai_NgayTao");

            entity.HasIndex(e => e.MaThamChieu, "UQ_GiaoDichThanhToan_MaThamChieu").IsUnique();

            entity.Property(e => e.IdgiaoDich).HasColumnName("IDGiaoDich");
            entity.Property(e => e.IddonHang).HasColumnName("IDDonHang");
            entity.Property(e => e.CongThanhToan)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.MaGiaoDichCong)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MaNganHang)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MaPhanHoi)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaThamChieu)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NgayCapNhat).HasColumnType("datetime2(0)");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnType("datetime2(0)");
            entity.Property(e => e.NgayThanhToan).HasColumnType("datetime2(0)");
            entity.Property(e => e.NoiDung).HasMaxLength(255);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.SoTien).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.TrangThai).HasDefaultValue((byte)1);

            entity.HasOne(d => d.IddonHangNavigation).WithMany(p => p.GiaoDichThanhToans)
                .HasForeignKey(d => d.IddonHang)
                .HasConstraintName("FK_GiaoDichThanhToan_DonHang");
        });

        modelBuilder.Entity<GioHang>(entity =>
        {
            entity.HasKey(e => new { e.IdnguoiDung, e.IdbienThe }).HasName("PK_GioHang");

            entity.ToTable("GioHang");

            entity.HasIndex(e => new { e.IdnguoiDung, e.NgayCapNhat }, "IX_GioHang_NgayCapNhat");

            entity.Property(e => e.IdnguoiDung).HasColumnName("IDNguoiDung");
            entity.Property(e => e.IdbienThe).HasColumnName("IDBienThe");
            entity.Property(e => e.NgayCapNhat)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnType("datetime2(0)");
            entity.Property(e => e.NgayThem)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnType("datetime2(0)");
            entity.Property(e => e.SoLuong).HasDefaultValue(1);

            entity.HasOne(d => d.IdbienTheNavigation).WithMany(p => p.GioHangs)
                .HasForeignKey(d => d.IdbienThe)
                .HasConstraintName("FK_GioHang_BienTheSanPham");

            entity.HasOne(d => d.IdnguoiDungNavigation).WithMany(p => p.GioHangs)
                .HasForeignKey(d => d.IdnguoiDung)
                .HasConstraintName("FK_GioHang_NguoiDung");
        });

        modelBuilder.Entity<HinhAnh>(entity =>
        {
            entity.HasKey(e => e.IdhinhAnh).HasName("PK_HinhAnh");

            entity.ToTable("HinhAnh");

            entity.Property(e => e.IdhinhAnh).HasColumnName("IDHinhAnh");
            entity.Property(e => e.DuongDan)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnType("datetime2(0)");
        });

        modelBuilder.Entity<HinhAnhBienThe>(entity =>
        {
            entity.HasKey(e => new { e.IdhinhAnh, e.IdbienThe }).HasName("PK_HinhAnhBienThe");

            entity.ToTable("HinhAnh_BienThe");

            entity.HasIndex(e => e.IdbienThe, "UX_HinhAnhBienThe_AnhChinh")
                .IsUnique()
                .HasFilter("([LaAnhChinh] = CONVERT([bit],(1)))");

            entity.Property(e => e.IdhinhAnh).HasColumnName("IDHinhAnh");
            entity.Property(e => e.IdbienThe).HasColumnName("IDBienThe");
            entity.Property(e => e.LaAnhChinh).HasDefaultValue(false);
            entity.Property(e => e.ThuTuHienThi).HasDefaultValue(0);

            entity.HasOne(d => d.IdbienTheNavigation).WithMany(p => p.HinhAnhBienThes)
                .HasForeignKey(d => d.IdbienThe)
                .HasConstraintName("FK_HinhAnhBienThe_BienTheSanPham");

            entity.HasOne(d => d.IdhinhAnhNavigation).WithMany(p => p.HinhAnhBienThes)
                .HasForeignKey(d => d.IdhinhAnh)
                .HasConstraintName("FK_HinhAnhBienThe_HinhAnh");
        });

        modelBuilder.Entity<KichThuoc>(entity =>
        {
            entity.HasKey(e => e.IdkichThuoc).HasName("PK_KichThuoc");

            entity.ToTable("KichThuoc");

            entity.HasIndex(e => e.TenKichThuoc, "UQ_KichThuoc_TenKichThuoc").IsUnique();

            entity.Property(e => e.IdkichThuoc).HasColumnName("IDKichThuoc");
            entity.Property(e => e.TenKichThuoc)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<LichSuDonHang>(entity =>
        {
            entity.HasKey(e => e.IdlichSu).HasName("PK_LichSuDonHang");

            entity.ToTable("LichSuDonHang");

            entity.HasIndex(e => new { e.IddonHang, e.NgayTao }, "IX_LichSuDonHang_DonHang_NgayTao");

            entity.Property(e => e.IdlichSu).HasColumnName("IDLichSu");
            entity.Property(e => e.GhiChu).HasMaxLength(500);
            entity.Property(e => e.IddonHang).HasColumnName("IDDonHang");
            entity.Property(e => e.IdnguoiThucHien).HasColumnName("IDNguoiThucHien");
            entity.Property(e => e.IdtrangThaiCu).HasColumnName("IDTrangThaiCu");
            entity.Property(e => e.IdtrangThaiMoi).HasColumnName("IDTrangThaiMoi");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnType("datetime2(0)");

            entity.HasOne(d => d.IddonHangNavigation).WithMany(p => p.LichSuDonHangs)
                .HasForeignKey(d => d.IddonHang)
                .HasConstraintName("FK_LichSuDonHang_DonHang");

            entity.HasOne(d => d.IdnguoiThucHienNavigation).WithMany(p => p.LichSuDonHangs)
                .HasForeignKey(d => d.IdnguoiThucHien)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_LichSuDonHang_NguoiDung");

            entity.HasOne(d => d.IdtrangThaiCuNavigation).WithMany(p => p.LichSuDonHangTrangThaiCus)
                .HasForeignKey(d => d.IdtrangThaiCu)
                .HasConstraintName("FK_LichSuDonHang_TrangThaiCu");

            entity.HasOne(d => d.IdtrangThaiMoiNavigation).WithMany(p => p.LichSuDonHangTrangThaiMois)
                .HasForeignKey(d => d.IdtrangThaiMoi)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichSuDonHang_TrangThaiMoi");
        });

        modelBuilder.Entity<LichSuTonKho>(entity =>
        {
            entity.HasKey(e => e.IdlichSu).HasName("PK_LichSuTonKho");

            entity.ToTable("LichSuTonKho");

            entity.HasIndex(e => new { e.IdbienThe, e.NgayTao }, "IX_LichSuTonKho_BienThe_NgayTao");

            entity.Property(e => e.IdlichSu).HasColumnName("IDLichSu");
            entity.Property(e => e.GhiChu).HasMaxLength(500);
            entity.Property(e => e.IdbienThe).HasColumnName("IDBienThe");
            entity.Property(e => e.IddonHang).HasColumnName("IDDonHang");
            entity.Property(e => e.IdnguoiThucHien).HasColumnName("IDNguoiThucHien");
            entity.Property(e => e.LoaiThayDoi).HasMaxLength(50);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnType("datetime2(0)");

            entity.HasOne(d => d.IdbienTheNavigation).WithMany(p => p.LichSuTonKhos)
                .HasForeignKey(d => d.IdbienThe)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichSuTonKho_BienTheSanPham");

            entity.HasOne(d => d.IddonHangNavigation).WithMany(p => p.LichSuTonKhos)
                .HasForeignKey(d => d.IddonHang)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_LichSuTonKho_DonHang");

            entity.HasOne(d => d.IdnguoiThucHienNavigation).WithMany(p => p.LichSuTonKhos)
                .HasForeignKey(d => d.IdnguoiThucHien)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_LichSuTonKho_NguoiDung");
        });

        modelBuilder.Entity<MaGiamGium>(entity =>
        {
            entity.HasKey(e => e.IdmaGiamGia).HasName("PK_MaGiamGia");

            entity.HasIndex(e => e.MaCode, "UQ_MaGiamGia_MaCode").IsUnique();

            entity.Property(e => e.IdmaGiamGia).HasColumnName("IDMaGiamGia");
            entity.Property(e => e.DeletedAt).HasColumnType("datetime2(0)");
            entity.Property(e => e.DonHangToiThieu)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 0)");
            entity.Property(e => e.GiaTri).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.GiamToiDa).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.MaCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NgayBatDau).HasColumnType("datetime2(0)");
            entity.Property(e => e.NgayKetThuc).HasColumnType("datetime2(0)");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnType("datetime2(0)");
            entity.Property(e => e.TenChuongTrinh).HasMaxLength(255);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);
        });

        modelBuilder.Entity<MauSac>(entity =>
        {
            entity.HasKey(e => e.IdmauSac).HasName("PK__MauSac__43136EAE77FC8133");

            entity.ToTable("MauSac");

            entity.Property(e => e.IdmauSac).HasColumnName("IDMauSac");
            entity.Property(e => e.MaMauHex)
                .HasMaxLength(7)
                .IsUnicode(false);
            entity.Property(e => e.TenMau).HasMaxLength(50);
        });

        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.HasKey(e => e.IdnguoiDung).HasName("PK_NguoiDung");

            entity.ToTable("NguoiDung");

            entity.HasIndex(e => e.SoDienThoai, "IX_NguoiDung_SoDienThoai")
                .IsUnique()
                .HasFilter("([SoDienThoai] IS NOT NULL)");

            entity.HasIndex(e => e.Email, "UQ_NguoiDung_Email").IsUnique();

            entity.Property(e => e.IdnguoiDung).HasColumnName("IDNguoiDung");
            entity.Property(e => e.DeletedAt).HasColumnType("datetime2(0)");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.IdvaiTro).HasColumnName("IDVaiTro");
            entity.Property(e => e.MatKhauHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.SecurityStamp)
                .HasDefaultValueSql("(newid())");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnType("datetime2(0)");
            entity.Property(e => e.NgayCapNhat).HasColumnType("datetime2(0)");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.IdvaiTroNavigation).WithMany(p => p.NguoiDungs)
                .HasForeignKey(d => d.IdvaiTro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NguoiDung_VaiTro");
        });

        modelBuilder.Entity<PhuongThucThanhToan>(entity =>
        {
            entity.HasKey(e => e.IdphuongThucThanhToan).HasName("PK_PhuongThucThanhToan");

            entity.ToTable("PhuongThucThanhToan");

            entity.HasIndex(e => e.TenPhuongThuc, "UQ_PhuongThucThanhToan_TenPhuongThuc").IsUnique();

            entity.HasIndex(e => e.MaPhuongThuc, "UQ_PhuongThucThanhToan_MaPhuongThuc").IsUnique();

            entity.Property(e => e.IdphuongThucThanhToan).HasColumnName("IDPhuongThucThanhToan");
            entity.Property(e => e.MaPhuongThuc)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.TenPhuongThuc).HasMaxLength(100);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);
        });

        modelBuilder.Entity<SanPham>(entity =>
        {
            entity.HasKey(e => e.IdsanPham).HasName("PK_SanPham");

            entity.ToTable("SanPham");

            entity.HasIndex(e => new { e.IddanhMuc, e.TrangThai, e.DeletedAt }, "IX_SanPham_DanhMuc_TrangThai");

            entity.HasIndex(e => new { e.IdthuongHieu, e.DeletedAt }, "IX_SanPham_ThuongHieu");

            entity.HasIndex(e => e.Slug, "UX_SanPham_Slug")
                .IsUnique()
                .HasFilter("([Slug] IS NOT NULL AND [DeletedAt] IS NULL)");

            entity.Property(e => e.IdsanPham).HasColumnName("IDSanPham");
            entity.Property(e => e.DeletedAt).HasColumnType("datetime2(0)");
            entity.Property(e => e.Gia)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 0)");
            entity.Property(e => e.GiaKhuyenMai).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.IddanhMuc).HasColumnName("IDDanhMuc");
            entity.Property(e => e.IdthuongHieu).HasColumnName("IDThuongHieu");
            entity.Property(e => e.NgayBatDauKm)
                .HasColumnType("datetime2(0)")
                .HasColumnName("NgayBatDauKM");
            entity.Property(e => e.NgayKetThucKm)
                .HasColumnType("datetime2(0)")
                .HasColumnName("NgayKetThucKM");
            entity.Property(e => e.NgayCapNhat).HasColumnType("datetime2(0)");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnType("datetime2(0)");
            entity.Property(e => e.Slug).HasMaxLength(255);
            entity.Property(e => e.TenSanPham).HasMaxLength(255);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.IddanhMucNavigation).WithMany(p => p.SanPhams)
                .HasForeignKey(d => d.IddanhMuc)
                .HasConstraintName("FK_SanPham_DanhMuc");

            entity.HasOne(d => d.IdthuongHieuNavigation).WithMany(p => p.SanPhams)
                .HasForeignKey(d => d.IdthuongHieu)
                .HasConstraintName("FK_SanPham_ThuongHieu");
        });

        modelBuilder.Entity<ThuongHieu>(entity =>
        {
            entity.HasKey(e => e.IdthuongHieu).HasName("PK_ThuongHieu");

            entity.ToTable("ThuongHieu");

            entity.HasIndex(e => e.TenThuongHieu, "UQ_ThuongHieu_TenThuongHieu").IsUnique();

            entity.Property(e => e.DeletedAt).HasColumnType("datetime2(0)");
            entity.Property(e => e.IdthuongHieu).HasColumnName("IDThuongHieu");
            entity.Property(e => e.TenThuongHieu).HasMaxLength(100);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);
        });

        modelBuilder.Entity<TinNhanChat>(entity =>
        {
            entity.HasKey(e => e.IdtinNhan).HasName("PK_TinNhanChat");

            entity.ToTable("TinNhanChat");

            entity.HasIndex(
                e => new { e.IdcuocTroChuyen, e.NgayTao, e.IdtinNhan },
                "IX_TinNhanChat_CuocTroChuyen_NgayTao");

            entity.Property(e => e.IdtinNhan).HasColumnName("IDTinNhan");
            entity.Property(e => e.IdcuocTroChuyen).HasColumnName("IDCuocTroChuyen");
            entity.Property(e => e.VaiTro)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NoiDung)
                .HasMaxLength(FashionHub.Web.Application.Chat.ChatLimits.MaxAssistantLength);
            entity.Property(e => e.DuLieuJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnType("datetime2(0)");

            entity.HasOne(d => d.IdcuocTroChuyenNavigation)
                .WithMany(p => p.TinNhanChats)
                .HasForeignKey(d => d.IdcuocTroChuyen)
                .HasConstraintName("FK_TinNhanChat_CuocTroChuyen");
        });

        modelBuilder.Entity<TrangThaiDonHang>(entity =>
        {
            entity.HasKey(e => e.IdtrangThai).HasName("PK_TrangThaiDonHang");

            entity.ToTable("TrangThaiDonHang");

            entity.HasIndex(e => e.TenTrangThai, "UQ_TrangThaiDonHang_TenTrangThai").IsUnique();

            entity.Property(e => e.IdtrangThai)
                .ValueGeneratedNever()
                .HasColumnName("IDTrangThai");
            entity.Property(e => e.TenTrangThai).HasMaxLength(100);
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.HasKey(e => e.IdvaiTro).HasName("PK_VaiTro");

            entity.ToTable("VaiTro");

            entity.HasIndex(e => e.TenVaiTro, "UQ_VaiTro_TenVaiTro").IsUnique();

            entity.Property(e => e.IdvaiTro)
                .ValueGeneratedNever()
                .HasColumnName("IDVaiTro");
            entity.Property(e => e.TenVaiTro)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<YeuThich>(entity =>
        {
            entity.HasKey(e => new { e.IdnguoiDung, e.IdsanPham }).HasName("PK_YeuThich");

            entity.ToTable("YeuThich");

            entity.Property(e => e.IdnguoiDung).HasColumnName("IDNguoiDung");
            entity.Property(e => e.IdsanPham).HasColumnName("IDSanPham");
            entity.Property(e => e.NgayThem)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnType("datetime2(0)");

            entity.HasOne(d => d.IdnguoiDungNavigation).WithMany(p => p.YeuThiches)
                .HasForeignKey(d => d.IdnguoiDung)
                .HasConstraintName("FK_YeuThich_NguoiDung");

            entity.HasOne(d => d.IdsanPhamNavigation).WithMany(p => p.YeuThiches)
                .HasForeignKey(d => d.IdsanPham)
                .HasConstraintName("FK_YeuThich_SanPham");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
