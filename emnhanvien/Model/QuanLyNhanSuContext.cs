using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace emnhanvien.Model;

public partial class QuanLyNhanSuContext : DbContext
{
    public QuanLyNhanSuContext()
    {
    }

    public QuanLyNhanSuContext(DbContextOptions<QuanLyNhanSuContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<ChamCong> ChamCongs { get; set; }

    public virtual DbSet<DangKiTangCa> DangKiTangCas { get; set; }

    public virtual DbSet<Luong> Luongs { get; set; }

    public virtual DbSet<NghiPhep> NghiPheps { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<PhongBan> PhongBans { get; set; }

    public virtual DbSet<QuenCheckOut> QuenCheckOuts { get; set; }

    public virtual DbSet<QuyDinh> QuyDinhs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Lấy thư mục gốc của ứng dụng
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.MaAdmin).HasName("PK__Admin__49341E3804C05979");

            entity.ToTable("Admin");

            entity.Property(e => e.MatKhau)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TaiKhoan)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ChamCong>(entity =>
        {
            entity.HasKey(e => e.MaChamCong).HasName("PK__ChamCong__307331A1D18CF8D3");

            entity.ToTable("ChamCong");

            entity.HasIndex(e => new { e.MaNhanVien, e.NgayChamCong }, "UQ__ChamCong__E89C0486ABB67B9A").IsUnique();

            entity.Property(e => e.SoGioTangCa).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TangCa)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TinhTrang).HasMaxLength(50);

            entity.HasOne(d => d.MaNhanVienNavigation).WithMany(p => p.ChamCongs)
                .HasForeignKey(d => d.MaNhanVien)
                .HasConstraintName("FK__ChamCong__MaNhan__7EF6D905");
        });

        modelBuilder.Entity<DangKiTangCa>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__dangKiTa__3214EC2786A9E1C4");

            entity.ToTable("dangKiTangCa");

            entity.HasIndex(e => e.MaChamCong, "UQ__dangKiTa__307331A0B13C0D8E").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.TinhTrang).HasMaxLength(20);

            entity.HasOne(d => d.MaChamCongNavigation).WithOne(p => p.DangKiTangCa)
                .HasForeignKey<DangKiTangCa>(d => d.MaChamCong)
                .HasConstraintName("FK__dangKiTan__MaCha__09746778");
        });

        modelBuilder.Entity<Luong>(entity =>
        {
            entity.HasKey(e => e.MaLuong).HasName("PK__Luong__6609A48D869789F7");

            entity.ToTable("Luong");

            entity.HasIndex(e => new { e.MaNhanVien, e.Thang, e.Nam }, "UQ_Luong").IsUnique();

            entity.Property(e => e.LuongCoBan).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LuongTangCa).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LuongTongCong)
                .HasComputedColumnSql("(([LuongCoBan]+[LuongTangCa])-[TienPhat])", true)
                .HasColumnType("decimal(20, 2)");
            entity.Property(e => e.TienPhat).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.MaNhanVienNavigation).WithMany(p => p.Luongs)
                .HasForeignKey(d => d.MaNhanVien)
                .HasConstraintName("FK__Luong__MaNhanVie__17C286CF");
        });

        modelBuilder.Entity<NghiPhep>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__nghiPhep__3214EC277A8EC47A");

            entity.ToTable("nghiPhep");

            entity.HasIndex(e => e.MaChamCong, "UQ__nghiPhep__307331A0EB8FF733").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.LiDo).HasMaxLength(255);
            entity.Property(e => e.TinhTrang).HasMaxLength(20);

            entity.HasOne(d => d.MaChamCongNavigation).WithOne(p => p.NghiPhep)
                .HasForeignKey<NghiPhep>(d => d.MaChamCong)
                .HasConstraintName("FK__nghiPhep__MaCham__11158940");
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.MaNhanVien).HasName("PK__NhanVien__77B2CA4751CFDE97");

            entity.ToTable("NhanVien");

            entity.HasIndex(e => e.SoDienThoai, "UQ__NhanVien__0389B7BD32B55F39").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__NhanVien__A9D105349FEF8691").IsUnique();

            entity.HasIndex(e => e.TaiKhoan, "UQ__NhanVien__D5B8C7F0700FE6C1").IsUnique();

            entity.Property(e => e.ChucVu).HasMaxLength(50);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.GioiTinh).HasMaxLength(10);
            entity.Property(e => e.HoTen).HasMaxLength(50);
            entity.Property(e => e.LuongCoBan).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MatKhau).HasMaxLength(100);
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.TaiKhoan).HasMaxLength(100);

            entity.HasOne(d => d.MaPhongBanNavigation).WithMany(p => p.NhanViens)
                .HasForeignKey(d => d.MaPhongBan)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NhanVien__MaPhon__793DFFAF");
        });

        modelBuilder.Entity<PhongBan>(entity =>
        {
            entity.HasKey(e => e.MaPhongBan).HasName("PK__PhongBan__D0910CC8FD16A0F9");

            entity.ToTable("PhongBan");

            entity.HasIndex(e => e.TenPhongBan, "UQ__PhongBan__997BDE38801ACD2F").IsUnique();

            entity.Property(e => e.TenPhongBan).HasMaxLength(100);
        });

        modelBuilder.Entity<QuenCheckOut>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__quenChec__3214EC274B7E0CD1");

            entity.ToTable("quenCheckOut");

            entity.HasIndex(e => e.MaChamCong, "UQ__quenChec__307331A0DA2BE6C0").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.LiDo).HasMaxLength(255);
            entity.Property(e => e.TinhTrang).HasMaxLength(20);

            entity.HasOne(d => d.MaChamCongNavigation).WithOne(p => p.QuenCheckOut)
                .HasForeignKey<QuenCheckOut>(d => d.MaChamCong)
                .HasConstraintName("FK__quenCheck__MaCha__0D44F85C");
        });

        modelBuilder.Entity<QuyDinh>(entity =>
        {
            entity.HasKey(e => e.MaQuyDinh).HasName("PK__QuyDinh__F79170494D9DB4AF");

            entity.ToTable("QuyDinh");

            entity.Property(e => e.MucPhatDiMuon).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TienPhatNghiKhongPhep).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TienPhatNghiQuaPhep).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TienThuongTangCa).HasColumnType("decimal(18, 2)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
