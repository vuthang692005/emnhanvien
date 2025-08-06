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
        var opt = new DbContextOptionsBuilder<QuanLyNhanSuContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new QuanLyNhanSuContext(opt);
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
        db.NhanViens.Add(new NhanVien
        {
            MaNhanVien = 1,
            HoTen = "Nguyen Van A",
            MaPhongBan = 1,
            ChucVu = "dev",
            Email = "a@gmail.com",
            SoDienThoai = "123456",
            NgaySinh = DateOnly.FromDateTime(DateTime.Now),
            NgayVaoLam = DateOnly.FromDateTime(DateTime.Now),
            LuongCoBan = 1000
        });
        db.SaveChanges();

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
        var controller = GetController(db);

        var dto = new NhanVienDto
        {
            HoTen = "Nguyen Van B",
            NgaySinh = DateOnly.FromDateTime(DateTime.Now),
            GioiTinh = "Nam",
            SoDienThoai = "789456",
            Email = "b@gmail.com",
            MaPhongBan = 1,
            ChucVu = "dev",
            LuongCoBan = 1000
        };

        db.PhongBans.Add(new PhongBan { MaPhongBan = 1, TenPhongBan = "IT" });
        db.SaveChanges();

        // Act
        var result = await controller.AddNhanVien(dto);

        // Assert
        Assert.IsType<CreatedResult>(result);
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
            MaPhongBan = 1,
            Email = "c@gmail.com",
            SoDienThoai = "111111",
            ChucVu = "dev",
            NgaySinh = DateOnly.FromDateTime(DateTime.Now),
            NgayVaoLam = DateOnly.FromDateTime(DateTime.Now),
            LuongCoBan = 1000
        });
        db.SaveChanges();

        var controller = GetController(db);

        var updated = new NhanVienUpdateDto
        {
            TenPhongBan = "IT",
            ChucVu = "lead dev",
            LuongCoBan = 2000
        };

        // Act
        var res = await controller.UpdateNhanVien(1, updated);

        // Assert
        Assert.IsType<OkObjectResult>(res);
    }

    [Fact]
    public async Task DeleteNhanVien_Success_ReturnsOk()
    {
        // Arrange
        var db = GetInMemoryDb();

        db.NhanViens.Add(new NhanVien
        {
            MaNhanVien = 1,
            HoTen = "Nguyen Van D",
            MaPhongBan = 1,
            Email = "d@gmail.com",
            SoDienThoai = "222222",
            ChucVu = "dev",
            NgaySinh = DateOnly.FromDateTime(DateTime.Now),
            NgayVaoLam = DateOnly.FromDateTime(DateTime.Now),
            LuongCoBan = 1000
        });
        db.SaveChanges();

        var controller = GetController(db);

        // Act
        var res = await controller.DeleteNhanVien(1);

        // Assert
        Assert.IsType<OkObjectResult>(res);
    }
}
