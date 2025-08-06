
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
        var options = new DbContextOptionsBuilder<QuanLyNhanSuContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging() // Giúp debug dễ hơn
            .Options;

        return new QuanLyNhanSuContext(options);
    }

    private IConfiguration GetFakeConfig()
    {
        var dic = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "supersecretkey1234567890supersecretkey123",
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
            MatKhau = BCrypt.Net.BCrypt.HashPassword("123456")
        });
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Jwt:Key"] = "this_is_a_very_long_secret_key_at_least_32_chars",
                ["Jwt:Issuer"] = "test",
                ["Jwt:Audience"] = "test",
                ["Jwt:ExpiresInHours"] = "1"
            })
            .Build();

        var controller = new LoginController(db, config);

        var request = new LoginRequest
        {
            TaiKhoan = "admin1",
            MatKhau = "123456"
        };

        // Act
        var result = await controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as dynamic;
        Assert.NotNull(response);

        // Kiểm tra token và role thông qua reflection
        var tokenProperty = response.GetType().GetProperty("Token");
        var roleProperty = response.GetType().GetProperty("role");

        Assert.NotNull(tokenProperty);
        Assert.NotNull(roleProperty);

        var tokenValue = tokenProperty.GetValue(response, null);
        var roleValue = roleProperty.GetValue(response, null);

        Assert.NotNull(tokenValue);
        Assert.Equal("Admin", roleValue?.ToString());
    }
}

