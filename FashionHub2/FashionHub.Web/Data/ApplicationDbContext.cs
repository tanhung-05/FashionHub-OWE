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

    public virtual DbSet<BienTheSanPham> BienTheSanPhams { get; set; }

    public virtual DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }

    public virtual DbSet<DanhMuc> DanhMucs { get; set; }

    public virtual DbSet<DiaChi> DiaChis { get; set; }

    public virtual DbSet<DonHang> DonHangs { get; set; }

    public virtual DbSet<GioHang> GioHangs { get; set; }

    public virtual DbSet<HinhAnh> HinhAnhs { get; set; }

    public virtual DbSet<HinhAnhBienThe> HinhAnhBienThes { get; set; }

    public virtual DbSet<KichThuoc> KichThuocs { get; set; }

    public virtual DbSet<MaGiamGium> MaGiamGia { get; set; }

    public virtual DbSet<MauSac> MauSacs { get; set; }

    public virtual DbSet<NguoiDung> NguoiDungs { get; set; }

    public virtual DbSet<PhuongThucThanhToan> PhuongThucThanhToans { get; set; }

    public virtual DbSet<SanPham> SanPhams { get; set; }

    public virtual DbSet<ThuongHieu> ThuongHieus { get; set; }

    public virtual DbSet<TrangThaiDonHang> TrangThaiDonHangs { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BienTheSanPham>(entity =>
        {
            entity.HasKey(e => e.IdbienThe).HasName("PK__BienTheS__9463A93D33F3B48C");

            entity.ToTable("BienTheSanPham");

            entity.HasIndex(e => e.Sku, "UQ__BienTheS__CA1ECF0D66A11039").IsUnique();

            entity.Property(e => e.IdbienThe).HasColumnName("IDBienThe");
            entity.Property(e => e.Gia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IdkichThuoc).HasColumnName("IDKichThuoc");
            entity.Property(e => e.IdmauSac).HasColumnName("IDMauSac");
            entity.Property(e => e.IdsanPham).HasColumnName("IDSanPham");
            entity.Property(e => e.Sku)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("SKU");

            entity.HasOne(d => d.IdkichThuocNavigation).WithMany(p => p.BienTheSanPhams)
                .HasForeignKey(d => d.IdkichThuoc)
                .HasConstraintName("FK__BienTheSa__IDKic__5AEE82B9");

            entity.HasOne(d => d.IdmauSacNavigation).WithMany(p => p.BienTheSanPhams)
                .HasForeignKey(d => d.IdmauSac)
                .HasConstraintName("FK__BienTheSa__IDMau__59FA5E80");

            entity.HasOne(d => d.IdsanPhamNavigation).WithMany(p => p.BienTheSanPhams)
                .HasForeignKey(d => d.IdsanPham)
                .HasConstraintName("FK__BienTheSa__IDSan__59063A47");
        });

        modelBuilder.Entity<ChiTietDonHang>(entity =>
        {
            entity.HasKey(e => e.IdchiTietDonHang).HasName("PK__ChiTietD__EB5BBDC098B6C11B");

            entity.ToTable("ChiTietDonHang");

            entity.HasIndex(e => new { e.IddonHang, e.IdbienThe }, "UQ_DonHang_BienThe").IsUnique();

            entity.Property(e => e.IdchiTietDonHang).HasColumnName("IDChiTietDonHang");
            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 2)");
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
                .HasConstraintName("FK__ChiTietDo__IDBie__797309D9");

            entity.HasOne(d => d.IddonHangNavigation).WithMany(p => p.ChiTietDonHangs)
                .HasForeignKey(d => d.IddonHang)
                .HasConstraintName("FK__ChiTietDo__IDDon__787EE5A0");
        });

        modelBuilder.Entity<DanhMuc>(entity =>
        {
            entity.HasKey(e => e.IddanhMuc).HasName("PK__DanhMuc__DF6C0BD28C775B9C");

            entity.ToTable("DanhMuc");

            entity.Property(e => e.IddanhMuc).HasColumnName("IDDanhMuc");
            entity.Property(e => e.IddanhMucCha).HasColumnName("IDDanhMucCha");
            entity.Property(e => e.TenDanhMuc).HasMaxLength(100);

            entity.HasOne(d => d.IddanhMucChaNavigation).WithMany(p => p.InverseIddanhMucChaNavigation)
                .HasForeignKey(d => d.IddanhMucCha)
                .HasConstraintName("FK__DanhMuc__IDDanhM__3E52440B");
        });

        modelBuilder.Entity<DiaChi>(entity =>
        {
            entity.HasKey(e => e.IddiaChi).HasName("PK__DiaChi__7B67D63AF4DCF6CC");

            entity.ToTable("DiaChi");

            entity.Property(e => e.IddiaChi).HasColumnName("IDDiaChi");
            entity.Property(e => e.ChiTiet).HasMaxLength(255);
            entity.Property(e => e.IdnguoiDung).HasColumnName("IDNguoiDung");
            entity.Property(e => e.LaMacDinh).HasDefaultValue(false);
            entity.Property(e => e.PhuongXa).HasMaxLength(100);
            entity.Property(e => e.QuanHuyen).HasMaxLength(100);
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.TenNguoiNhan).HasMaxLength(100);
            entity.Property(e => e.TinhThanh).HasMaxLength(100);

            entity.HasOne(d => d.IdnguoiDungNavigation).WithMany(p => p.DiaChis)
                .HasForeignKey(d => d.IdnguoiDung)
                .HasConstraintName("FK__DiaChi__IDNguoiD__4D94879B");
        });

        modelBuilder.Entity<DonHang>(entity =>
        {
            entity.HasKey(e => e.IddonHang).HasName("PK__DonHang__9CA232F7813ED797");

            entity.ToTable("DonHang");

            entity.Property(e => e.IddonHang).HasColumnName("IDDonHang");
            entity.Property(e => e.DiaChiGiao).HasMaxLength(500);
            entity.Property(e => e.IdmaGiamGia).HasColumnName("IDMaGiamGia");
            entity.Property(e => e.IdnguoiDung).HasColumnName("IDNguoiDung");
            entity.Property(e => e.IdphuongThucThanhToan).HasColumnName("IDPhuongThucThanhToan");
            entity.Property(e => e.IdtrangThai).HasColumnName("IDTrangThai");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PhiVanChuyen)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.TenNguoiNhan).HasMaxLength(100);
            entity.Property(e => e.TienGiamGia)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TongThanhToan).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TongTienHang).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdmaGiamGiaNavigation).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.IdmaGiamGia)
                .HasConstraintName("FK__DonHang__IDMaGia__72C60C4A");

            entity.HasOne(d => d.IdnguoiDungNavigation).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.IdnguoiDung)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__DonHang__IDNguoi__71D1E811");

            entity.HasOne(d => d.IdphuongThucThanhToanNavigation).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.IdphuongThucThanhToan)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__DonHang__IDPhuon__73BA3083");

            entity.HasOne(d => d.IdtrangThaiNavigation).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.IdtrangThai)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DonHang__IDTrang__74AE54BC");
        });

        modelBuilder.Entity<GioHang>(entity =>
        {
            entity.HasKey(e => new { e.IdnguoiDung, e.IdbienThe }).HasName("PK__GioHang__3591E19A3908837D");

            entity.ToTable("GioHang");

            entity.Property(e => e.IdnguoiDung).HasColumnName("IDNguoiDung");
            entity.Property(e => e.IdbienThe).HasColumnName("IDBienThe");
            entity.Property(e => e.SoLuong).HasDefaultValue(1);

            entity.HasOne(d => d.IdbienTheNavigation).WithMany(p => p.GioHangs)
                .HasForeignKey(d => d.IdbienThe)
                .HasConstraintName("FK__GioHang__IDBienT__66603565");

            entity.HasOne(d => d.IdnguoiDungNavigation).WithMany(p => p.GioHangs)
                .HasForeignKey(d => d.IdnguoiDung)
                .HasConstraintName("FK__GioHang__IDNguoi__656C112C");
        });

        modelBuilder.Entity<HinhAnh>(entity =>
        {
            entity.HasKey(e => e.IdhinhAnh).HasName("PK__HinhAnh__2B573EE874073B1C");

            entity.ToTable("HinhAnh");

            entity.Property(e => e.IdhinhAnh).HasColumnName("IDHinhAnh");
            entity.Property(e => e.DuongDan)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.MoTa).HasMaxLength(255);
        });

        modelBuilder.Entity<HinhAnhBienThe>(entity =>
        {
            entity.HasKey(e => new { e.IdhinhAnh, e.IdbienThe }).HasName("PK__HinhAnh___E211047B1E02CC30");

            entity.ToTable("HinhAnh_BienThe");

            entity.Property(e => e.IdhinhAnh).HasColumnName("IDHinhAnh");
            entity.Property(e => e.IdbienThe).HasColumnName("IDBienThe");
            entity.Property(e => e.LaAnhChinh).HasDefaultValue(false);

            entity.HasOne(d => d.IdbienTheNavigation).WithMany(p => p.HinhAnhBienThes)
                .HasForeignKey(d => d.IdbienThe)
                .HasConstraintName("FK__HinhAnh_B__IDBie__619B8048");

            entity.HasOne(d => d.IdhinhAnhNavigation).WithMany(p => p.HinhAnhBienThes)
                .HasForeignKey(d => d.IdhinhAnh)
                .HasConstraintName("FK__HinhAnh_B__IDHin__60A75C0F");
        });

        modelBuilder.Entity<KichThuoc>(entity =>
        {
            entity.HasKey(e => e.IdkichThuoc).HasName("PK__KichThuo__CEC1D50488AB2956");

            entity.ToTable("KichThuoc");

            entity.Property(e => e.IdkichThuoc).HasColumnName("IDKichThuoc");
            entity.Property(e => e.TenKichThuoc)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<MaGiamGium>(entity =>
        {
            entity.HasKey(e => e.IdmaGiamGia).HasName("PK__MaGiamGi__7DEBD11EC9BC665F");

            entity.HasIndex(e => e.MaCode, "UQ__MaGiamGi__152C7C5C1B28FA2F").IsUnique();

            entity.Property(e => e.IdmaGiamGia).HasColumnName("IDMaGiamGia");
            entity.Property(e => e.DonHangToiThieu)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GiaTri).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GiamToiDa).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MaCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NgayBatDau).HasColumnType("datetime");
            entity.Property(e => e.NgayKetThuc).HasColumnType("datetime");
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
            entity.HasKey(e => e.IdnguoiDung).HasName("PK__NguoiDun__FCD7DB09766DC533");

            entity.ToTable("NguoiDung");

            entity.HasIndex(e => e.SoDienThoai, "IX_NguoiDung_SoDienThoai")
                .IsUnique()
                .HasFilter("([SoDienThoai] IS NOT NULL)");

            entity.HasIndex(e => e.Email, "UQ__NguoiDun__A9D1053499223ED1").IsUnique();

            entity.Property(e => e.IdnguoiDung).HasColumnName("IDNguoiDung");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.IdvaiTro).HasColumnName("IDVaiTro");
            entity.Property(e => e.MatKhauHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.IdvaiTroNavigation).WithMany(p => p.NguoiDungs)
                .HasForeignKey(d => d.IdvaiTro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NguoiDung__IDVai__49C3F6B7");
        });

        modelBuilder.Entity<PhuongThucThanhToan>(entity =>
        {
            entity.HasKey(e => e.IdphuongThucThanhToan).HasName("PK__PhuongTh__0A586C8CC7A50E01");

            entity.ToTable("PhuongThucThanhToan");

            entity.Property(e => e.IdphuongThucThanhToan).HasColumnName("IDPhuongThucThanhToan");
            entity.Property(e => e.TenPhuongThuc).HasMaxLength(100);
        });

        modelBuilder.Entity<SanPham>(entity =>
        {
            entity.HasKey(e => e.IdsanPham).HasName("PK__SanPham__9D45E58AE6B3F732");

            entity.ToTable("SanPham");

            entity.Property(e => e.IdsanPham).HasColumnName("IDSanPham");
            entity.Property(e => e.Gia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GiaKhuyenMai).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IddanhMuc).HasColumnName("IDDanhMuc");
            entity.Property(e => e.IdthuongHieu).HasColumnName("IDThuongHieu");
            entity.Property(e => e.NgayBatDauKm)
                .HasColumnType("datetime")
                .HasColumnName("NgayBatDauKM");
            entity.Property(e => e.NgayKetThucKm)
                .HasColumnType("datetime")
                .HasColumnName("NgayKetThucKM");
            entity.Property(e => e.TenSanPham).HasMaxLength(255);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.IddanhMucNavigation).WithMany(p => p.SanPhams)
                .HasForeignKey(d => d.IddanhMuc)
                .HasConstraintName("FK__SanPham__IDDanhM__52593CB8");

            entity.HasOne(d => d.IdthuongHieuNavigation).WithMany(p => p.SanPhams)
                .HasForeignKey(d => d.IdthuongHieu)
                .HasConstraintName("FK__SanPham__IDThuon__534D60F1");
        });

        modelBuilder.Entity<ThuongHieu>(entity =>
        {
            entity.HasKey(e => e.IdthuongHieu).HasName("PK__ThuongHi__D4ADEAC86EA91726");

            entity.ToTable("ThuongHieu");

            entity.Property(e => e.IdthuongHieu).HasColumnName("IDThuongHieu");
            entity.Property(e => e.TenThuongHieu).HasMaxLength(100);
        });

        modelBuilder.Entity<TrangThaiDonHang>(entity =>
        {
            entity.HasKey(e => e.IdtrangThai).HasName("PK__TrangTha__5565860090FA9521");

            entity.ToTable("TrangThaiDonHang");

            entity.Property(e => e.IdtrangThai)
                .ValueGeneratedNever()
                .HasColumnName("IDTrangThai");
            entity.Property(e => e.TenTrangThai).HasMaxLength(100);
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.HasKey(e => e.IdvaiTro).HasName("PK__VaiTro__45D3FF490812289D");

            entity.ToTable("VaiTro");

            entity.HasIndex(e => e.TenVaiTro, "UQ__VaiTro__1DA55814F3F531B4").IsUnique();

            entity.Property(e => e.IdvaiTro).HasColumnName("IDVaiTro");
            entity.Property(e => e.TenVaiTro)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
