
using emnhanvien.Controllers;
using emnhanvien.DTO;
using emnhanvien.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace emnhanvien.Tests;
public class LoginControllerTests
{
    private QuanLyNhanSuContext GetInMemoryDb()
    {
        var opt = new DbContextOptionsBuilder<QuanLyNhanSuContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

        var db = new QuanLyNhanSuContext(opt);
        return db;
    }

    private IConfiguration GetFakeConfig()
    {
        var dic = new Dictionary<string, string>
        {
            ["Jwt:Key"] = "supersecretkey123456",
            ["Jwt:Issuer"] = "test",
            ["Jwt:Audience"] = "test",
            ["Jwt:ExpiresInHours"] = "1"
        };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(dic)
            .Build();
    }

    [Fact]
    public async Task Login_AdminSuccess_ReturnsOk()
    {
        // Arrange
        var db = GetInMemoryDb();
        db.Admins.Add(new Admin
        {
            TaiKhoan = "admin1",
            MatKhau = BCrypt.Net.BCrypt.HashPassword("123")
        });
        db.SaveChanges();

        var controller = new LoginController(db, GetFakeConfig());

        var request = new LoginRequest { TaiKhoan = "admin1", MatKhau = "123" };

        // Act
        var result = await controller.Login(request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(((dynamic)ok.Value).Token);
        Assert.Equal("Admin", ((dynamic)ok.Value).role);
    }
}

