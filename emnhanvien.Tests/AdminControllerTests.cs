using emnhanvien.Controllers;
using emnhanvien.DTO;
using emnhanvien.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace emnhanvien.Tests;
public class AdminControllerTests
{
    private QuanLyNhanSuContext GetInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<QuanLyNhanSuContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging() // Giúp debug dễ hơn
            .Options;

        return new QuanLyNhanSuContext(options);
    }

    private AdminController GetController(QuanLyNhanSuContext db, bool isAdmin = true)
    {
        var controller = new AdminController(db);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, isAdmin ? "Admin" : "User")
        }, "mock"));

        controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };

        return controller;
    }

    [Fact]
    public async Task GetNhanVien_ReturnsOk()
    {
        // Arrange
        var db = GetInMemoryDb();
        db.PhongBans.Add(new PhongBan { MaPhongBan = 1, TenPhongBan = "IT" });

        // Thêm đầy đủ thông tin nhân viên
        db.NhanViens.Add(new NhanVien
        {
            MaNhanVien = 1,
            HoTen = "Nguyen Van A",
            TaiKhoan = "nva123",
            MatKhau = BCrypt.Net.BCrypt.HashPassword("password123"),
            NgaySinh = DateOnly.FromDateTime(DateTime.Now.AddYears(-30)),
            GioiTinh = "Nam",
            SoDienThoai = "1234567890",
            Email = "a@gmail.com",
            MaPhongBan = 1,
            ChucVu = "Developer",
            NgayVaoLam = DateOnly.FromDateTime(DateTime.Now),
            LuongCoBan = 1000
        });

        await db.SaveChangesAsync();

        var controller = GetController(db, true);

        // Act
        var result = await controller.GetNhanVien(null, null, null, null);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AddNhanVien_Success_ReturnsCreated()
    {
        // Arrange
        var db = GetInMemoryDb();
        db.PhongBans.Add(new PhongBan { MaPhongBan = 1, TenPhongBan = "IT" });
        await db.SaveChangesAsync();

        var controller = GetController(db);

        var dto = new NhanVienDto
        {
            HoTen = "Nguyen Van B",
            NgaySinh = DateOnly.FromDateTime(DateTime.Now.AddYears(-25)),
            GioiTinh = "Nam",
            SoDienThoai = "0987654321",
            Email = "b@gmail.com",
            MaPhongBan = 1,
            ChucVu = "Tester",
            LuongCoBan = 800
        };

        // Act
        var result = await controller.AddNhanVien(dto);

        // Assert
        Assert.IsType<CreatedResult>(result);
        var nhanVien = await db.NhanViens.FirstOrDefaultAsync();
        Assert.NotNull(nhanVien);
        Assert.Equal("Nguyen Van B", nhanVien.HoTen);
        Assert.Equal("Nam", nhanVien.GioiTinh);
        // Kiểm tra controller đã tự tạo tài khoản/mật khẩu mặc định
        Assert.False(string.IsNullOrEmpty(nhanVien.TaiKhoan));
        Assert.False(string.IsNullOrEmpty(nhanVien.MatKhau));
    }

    [Fact]
    public async Task UpdateNhanVien_Success_ReturnsOk()
    {
        // Arrange
        var db = GetInMemoryDb();
        db.PhongBans.Add(new PhongBan { MaPhongBan = 1, TenPhongBan = "IT" });

        db.NhanViens.Add(new NhanVien
        {
            MaNhanVien = 1,
            HoTen = "Nguyen Van C",
            TaiKhoan = "nvc123",
            MatKhau = BCrypt.Net.BCrypt.HashPassword("password123"),
            NgaySinh = DateOnly.FromDateTime(DateTime.Now.AddYears(-28)),
            GioiTinh = "Nam",
            SoDienThoai = "0912345678",
            Email = "c@gmail.com",
            MaPhongBan = 1,
            ChucVu = "Dev",
            NgayVaoLam = DateOnly.FromDateTime(DateTime.Now),
            LuongCoBan = 900
        });

        await db.SaveChangesAsync();

        var controller = GetController(db);

        var updated = new NhanVienUpdateDto
        {
            TenPhongBan = "IT",
            ChucVu = "Senior Dev",
            LuongCoBan = 1200
        };

        // Act
        var res = await controller.UpdateNhanVien(1, updated);

        // Assert
        Assert.IsType<OkObjectResult>(res);
        var updatedNhanVien = await db.NhanViens.FindAsync(1);
        Assert.Equal("Senior Dev", updatedNhanVien.ChucVu);
        Assert.Equal(1200, updatedNhanVien.LuongCoBan);
    }

    [Fact]
    public async Task DeleteNhanVien_Success_ReturnsOk()
    {
        // Arrange
        var db = GetInMemoryDb();
        db.PhongBans.Add(new PhongBan { MaPhongBan = 1, TenPhongBan = "IT" });

        db.NhanViens.Add(new NhanVien
        {
            MaNhanVien = 1,
            HoTen = "Nguyen Van D",
            TaiKhoan = "nvd123",
            MatKhau = BCrypt.Net.BCrypt.HashPassword("password123"),
            NgaySinh = DateOnly.FromDateTime(DateTime.Now.AddYears(-25)),
            GioiTinh = "Nam",
            SoDienThoai = "222222",
            Email = "d@gmail.com",
            MaPhongBan = 1,
            ChucVu = "dev",
            NgayVaoLam = DateOnly.FromDateTime(DateTime.Now),
            LuongCoBan = 1000
        });
        await db.SaveChangesAsync();

        var controller = GetController(db);

        // Act
        var res = await controller.DeleteNhanVien(1);

        // Assert
        Assert.IsType<OkObjectResult>(res);
    }
}
