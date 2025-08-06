using emnhanvien.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using emnhanvien.DTO;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace emnhanvien.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly QuanLyNhanSuContext _context;
        private readonly IConfiguration _configuration;

        public LoginController(QuanLyNhanSuContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginUserDto)
        {
            if (loginUserDto == null || string.IsNullOrEmpty(loginUserDto.TaiKhoan) || string.IsNullOrEmpty(loginUserDto.MatKhau))
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            // Kiểm tra Admin trước
            var admin = await _context.Admins
                .FirstOrDefaultAsync(u => u.TaiKhoan == loginUserDto.TaiKhoan);

            if (admin != null && BCrypt.Net.BCrypt.Verify(loginUserDto.MatKhau, admin.MatKhau))
            {
                var token = GenerateJwtToken(admin.TaiKhoan, "Admin", admin.MaAdmin);
                return Ok(new { Token = token, role = "Admin"});
            }

            // Kiểm tra NhanVien nếu không phải Admin
            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(u => u.TaiKhoan == loginUserDto.TaiKhoan);

            if (nhanVien != null && BCrypt.Net.BCrypt.Verify(loginUserDto.MatKhau, nhanVien.MatKhau))
            {
                var token = GenerateJwtToken(nhanVien.TaiKhoan, "NhanVien", nhanVien.MaNhanVien);
                return Ok(new { Token = token,role = "NhanVien" });
            }

            return Unauthorized("Tên đăng nhập hoặc mật khẩu không chính xác!");
        }

        private string GenerateJwtToken(string username, string role, int? maNhanVien = null)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var tokenHandler = new JwtSecurityTokenHandler();
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, username),  // Lưu username
        new Claim(ClaimTypes.Role, role)       // Lưu role
    };

            // Lưu mã nhân viên nếu có
            if (maNhanVien.HasValue)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, maNhanVien.Value.ToString())); // Chuyển int thành string
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(int.Parse(_configuration["Jwt:ExpiresInHours"])),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }




    }
}
