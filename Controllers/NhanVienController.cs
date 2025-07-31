using System.Security.Claims;
using emnhanvien.DTO;
using emnhanvien.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace emnhanvien.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class NhanVienController : ControllerBase
    {
        private readonly QuanLyNhanSuContext _context;

        public NhanVienController(QuanLyNhanSuContext context)
        {
            _context = context;
        }

        [HttpGet("nhanvien")]
        public async Task<IActionResult> GetNhanVienById()
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            var nhanVien = await _context.NhanViens
                .Where(nv => nv.MaNhanVien == id)
                .Select(nv => new
                {
                    nv.MaNhanVien,
                    nv.HoTen,
                    nv.NgaySinh,
                    nv.GioiTinh,
                    nv.SoDienThoai,
                    nv.Email,
                    nv.MaPhongBanNavigation.TenPhongBan,
                    nv.ChucVu,
                    nv.NgayVaoLam,
                    nv.LuongCoBan,
                })
                .FirstOrDefaultAsync();

            if (nhanVien == null)
                return NotFound(new { Message = "Không tìm thấy nhân viên" });

            return Ok(nhanVien);
        }

        [HttpGet("chamcong")]
        public async Task<IActionResult> GetChamCongByNhanVien(DateOnly ngayChamCong)
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            var danhSachNgayChamCong = await _context.ChamCongs
                .Where(cc => cc.MaNhanVien == id && cc.NgayChamCong == ngayChamCong)
                .Select(cc => new
                {
                    cc.NgayChamCong,
                    cc.GioVao,
                    cc.GioRa,
                    cc.TinhTrang,
                    cc.TangCa,
                    cc.SoGioTangCa
                })
                .FirstOrDefaultAsync();

            if (danhSachNgayChamCong == null)
                return NotFound(new { Message = "Không tìm thấy dữ liệu chấm công" });

            return Ok(danhSachNgayChamCong);
        }

        [HttpPut("nhanvien")]
        public async Task<IActionResult> UpdateNhanVien([FromBody] nhanVienUpdate updatedNhanVienDto)
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            if (updatedNhanVienDto == null ||
                string.IsNullOrWhiteSpace(updatedNhanVienDto.SoDienThoai) ||
                string.IsNullOrWhiteSpace(updatedNhanVienDto.Email))
            {
                return BadRequest("Dữ liệu không hợp lệ");
            }

            bool isEmailExists = await _context.NhanViens
                .AnyAsync(nv => nv.Email == updatedNhanVienDto.Email && nv.MaNhanVien != id);

            if (isEmailExists)
                return BadRequest("Email đã tồn tại");

            bool isPhoneExists = await _context.NhanViens
                .AnyAsync(nv => nv.SoDienThoai == updatedNhanVienDto.SoDienThoai && nv.MaNhanVien != id);

            if (isPhoneExists)
                return BadRequest("Số điện thoại đã tồn tại");

            var existingNhanVien = await _context.NhanViens.FindAsync(id);
            if (existingNhanVien == null)
                return NotFound("Không tìm thấy nhân viên");

            existingNhanVien.Email = updatedNhanVienDto.Email;
            existingNhanVien.SoDienThoai = updatedNhanVienDto.SoDienThoai;

            await _context.SaveChangesAsync();
            return Ok(new
            {
                Message = "Cập nhật nhân viên thành công",
            });
        }

        [HttpPut("CheckIn")]
        public async Task<IActionResult> CheckIn([FromQuery] DateOnly ngayChamCong)
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);

            if (ngayChamCong != today)
            {
                return BadRequest("Chỉ được check-in cho ngày hôm nay.");
            }

            if (now.Hour < 8)
            {
                return BadRequest("Chưa đến giờ làm, không thể check-in.");
            }

            if (now.Hour > 9)
            {
                return BadRequest("Đã quá giờ check-in.");
            }

            var chamCong = await _context.ChamCongs
                .FirstOrDefaultAsync(c => c.MaNhanVien == id && c.NgayChamCong == ngayChamCong);

            if (chamCong == null)
            {
                return NotFound("Không tìm thấy bản ghi chấm công.");
            }

            // Kiểm tra nếu đã có giờ vào
            if (chamCong.GioVao.HasValue)
            {
                return BadRequest("Nhân viên đã check-in, không thể check-in lại.");
            }

            // Cập nhật giờ vào thành thời gian hiện tại
            chamCong.GioVao = new TimeOnly(now.Hour, now.Minute);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Check-in thành công!" });
        }

        [HttpPut("CheckOut")]
        public async Task<IActionResult> CheckOut([FromQuery] DateOnly ngayChamCong)
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);

            if (ngayChamCong != today)
            {
                return BadRequest("Chỉ được check-out cho ngày hôm nay.");
            }

            if (now.Hour < 17)
            {
                return BadRequest("Chưa hết giờ làm, không thể check-out.");
            }

            var chamCong = await _context.ChamCongs
                .FirstOrDefaultAsync(c => c.MaNhanVien == id && c.NgayChamCong == ngayChamCong);

            if (chamCong == null)
            {
                return NotFound("Không tìm thấy bản ghi chấm công.");
            }

            if (!chamCong.GioVao.HasValue)
            {
                return BadRequest("Nhân viên chưa check-in, không thể check-out.");
            }

            if (chamCong.GioVao > new TimeOnly(8, 30))
            {
                chamCong.TinhTrang = "Muộn";
            }
            else
            {
                chamCong.TinhTrang = "Có mặt";
            }

            chamCong.GioRa = new TimeOnly(now.Hour, now.Minute);

            if (chamCong.TangCa == "True")
            {
                TimeOnly gioRa = chamCong.GioRa.Value;
                chamCong.SoGioTangCa = (decimal)(gioRa.Hour - 17) + (decimal)gioRa.Minute / 60;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Check-out thành công!" });
        }

        [HttpPost("DKTangCa")]
        public async Task<IActionResult> DangKyTangCa([FromQuery] DateOnly ngayChamCong)
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            if (ngayChamCong <= DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest("Không thể đăng ký tăng ca cho hôm nay và ngày trong quá khứ.");
            }

            // Kiểm tra MaChamCong có tồn tại không
            var chamCong = await _context.ChamCongs
                .FirstOrDefaultAsync(c => c.MaNhanVien == id && c.NgayChamCong == ngayChamCong);

            if (chamCong == null)
            {
                return NotFound("Không tìm thấy bản ghi chấm công.");
            }

            // Kiểm tra đã đăng ký tăng ca chưa
            bool daDangKy = await _context.DangKiTangCas.AnyAsync(d => d.MaChamCong == chamCong.MaChamCong);
            if (daDangKy)
            {
                return Conflict("Đã đăng ký tăng ca cho ngày này.");
            }

            // Tạo bản ghi mới
            var dangKiTangCa = new DangKiTangCa
            {
                MaChamCong = chamCong.MaChamCong,
                TinhTrang = "Chờ duyệt"
            };

            _context.DangKiTangCas.Add(dangKiTangCa);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký tăng ca thành công." });
        }

        [HttpGet("ChoDuyetDKTangCa")]
        public async Task<IActionResult> GetDangKyTangCaCD()
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            var query = _context.DangKiTangCas
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.MaChamCongNavigation.MaNhanVien == id && d.TinhTrang == "Chờ duyệt")
                .AsQueryable();

            var result = await query.Select(d => new
            {
                d.TinhTrang,
                NgayChamCong = d.MaChamCongNavigation.NgayChamCong
            }).ToListAsync();

            if (!result.Any())
            {
                return NotFound("Không tìm thấy đăng ký tăng ca phù hợp.");
            }

            return Ok(result);
        }

        [HttpGet("DaDuyetDKTangCa")]
        public async Task<IActionResult> GetDangKyTangCaDD()
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            var query = _context.DangKiTangCas
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.MaChamCongNavigation.MaNhanVien == id && d.TinhTrang == "đã duyệt")
                .AsQueryable();

            var result = await query.Select(d => new
            {
                d.TinhTrang,
                NgayChamCong = d.MaChamCongNavigation.NgayChamCong
            }).ToListAsync();

            if (!result.Any())
            {
                return NotFound("Không tìm thấy đăng ký tăng ca phù hợp.");
            }

            return Ok(result);
        }

        [HttpGet("TuChoiDKTangCa")]
        public async Task<IActionResult> GetDangKyTangCaTC()
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            var query = _context.DangKiTangCas
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.MaChamCongNavigation.MaNhanVien == id && d.TinhTrang == "Từ chối")
                .AsQueryable();

            var result = await query.Select(d => new
            {
                d.TinhTrang,
                NgayChamCong = d.MaChamCongNavigation.NgayChamCong
            }).ToListAsync();

            if (!result.Any())
            {
                return NotFound("Không tìm thấy đăng ký tăng ca phù hợp.");
            }

            return Ok(result);
        }

        [HttpDelete("XoaDKTangCa")]
        public async Task<IActionResult> XoaDangKyTangCa(DateOnly ngayLam)
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            var dangKy = await _context.DangKiTangCas
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.MaChamCongNavigation.MaNhanVien == id &&
                            d.MaChamCongNavigation.NgayChamCong == ngayLam)
                .FirstOrDefaultAsync();

            if (dangKy == null)
            {
                return NotFound("Không tìm thấy đăng ký tăng ca.");
            }

            _context.DangKiTangCas.Remove(dangKy);
            await _context.SaveChangesAsync();

            return Ok("Đăng ký tăng ca đã được xóa thành công.");
        }

        [HttpPost("QuenCheckOut")]
        public async Task<IActionResult> QuenCheckOut([FromQuery] DateOnly ngayChamCong, [FromQuery] string? LiDo)
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            if (ngayChamCong >= DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest("Chỉ có thể chọn ngày trước ngày hôm nay.");
            }

            var chamCong = await _context.ChamCongs
                .FirstOrDefaultAsync(c => c.MaNhanVien == id && c.NgayChamCong == ngayChamCong);

            if (chamCong == null)
            {
                return NotFound("Không tìm thấy bản ghi chấm công.");
            }

            // Kiểm tra nhân viên đã check in chưa
            if (chamCong.GioVao == null)
            {
                return BadRequest("Nhân viên chưa check in.");
            }

            // Kiểm tra nhân viên đã check out chưa
            if (chamCong.GioRa != null)
            {
                return BadRequest("Nhân viên đã check out, không thể báo quên check out.");
            }

            // Kiểm tra đã đăng ký quên check out chưa
            bool daDangKy = await _context.QuenCheckOuts.AnyAsync(d => d.MaChamCong == chamCong.MaChamCong);
            if (daDangKy)
            {
                return Conflict("Đã thông báo quên check-out cho ngày này.");
            }

            // Tạo bản ghi mới
            var quenCheckOut = new QuenCheckOut
            {
                MaChamCong = chamCong.MaChamCong,
                LiDo = LiDo,
                TinhTrang = "Chờ duyệt"
            };

            _context.QuenCheckOuts.Add(quenCheckOut);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Thông báo quên check-out thành công." });
        }

        [HttpGet("ChoDuyetQuenCO")]
        public async Task<IActionResult> GetQuenCheckOutCD()
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            var query = _context.QuenCheckOuts
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.MaChamCongNavigation.MaNhanVien == id && d.TinhTrang == "Chờ duyệt")
                .AsQueryable();

            var result = await query.Select(d => new
            {
                d.TinhTrang,
                d.LiDo,
                NgayChamCong = d.MaChamCongNavigation.NgayChamCong
            }).ToListAsync();

            if (!result.Any())
            {
                return NotFound("Không tìm thấy bản ghi phù hợp.");
            }

            return Ok(result);
        }

        [HttpGet("DaDuyetQuenCO")]
        public async Task<IActionResult> GetQuenCheckOutDD()
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            var query = _context.QuenCheckOuts
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.MaChamCongNavigation.MaNhanVien == id && d.TinhTrang == "Đã duyệt")
                .AsQueryable();

            var result = await query.Select(d => new
            {
                d.TinhTrang,
                d.LiDo,
                NgayChamCong = d.MaChamCongNavigation.NgayChamCong
            }).ToListAsync();

            if (!result.Any())
            {
                return NotFound("Không tìm thấy bản ghi phù hợp.");
            }

            return Ok(result);
        }

        [HttpGet("TuChoiQuenCO")]
        public async Task<IActionResult> GetQuenCheckOutTC()
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            var query = _context.QuenCheckOuts
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.MaChamCongNavigation.MaNhanVien == id && d.TinhTrang == "Từ chối")
                .AsQueryable();

            var result = await query.Select(d => new
            {
                d.TinhTrang,
                d.LiDo,
                NgayChamCong = d.MaChamCongNavigation.NgayChamCong
            }).ToListAsync();

            if (!result.Any())
            {
                return NotFound("Không tìm thấy bản ghi phù hợp.");
            }

            return Ok(result);
        }

        [HttpDelete("XoaQuenCO")]
        public async Task<IActionResult> XoaQuenCheckOut(DateOnly ngayLam)
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            var dangKy = await _context.QuenCheckOuts
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.MaChamCongNavigation.MaNhanVien == id &&
                            d.MaChamCongNavigation.NgayChamCong == ngayLam)
                .FirstOrDefaultAsync();

            if (dangKy == null)
            {
                return NotFound("Không tìm thấy quên check-out.");
            }

            _context.QuenCheckOuts.Remove(dangKy);
            await _context.SaveChangesAsync();

            return Ok("Quên check-out đã được xóa thành công.");
        }

        [HttpPost("XinNghiPhep")]
        public async Task<IActionResult> XinNghiPhep([FromQuery] DateOnly ngayChamCong, [FromQuery] string? LiDo)
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            if (ngayChamCong < DateOnly.FromDateTime(DateTime.Today).AddDays(2))
            {
                return BadRequest("Phải xin nghỉ phép trước hai ngày.");
            }

            var chamCong = await _context.ChamCongs
                .FirstOrDefaultAsync(c => c.MaNhanVien == id && c.NgayChamCong == ngayChamCong);

            if (chamCong == null)
            {
                return NotFound("Không tìm thấy bản ghi chấm công.");
            }

            // Kiểm tra đã đăng ký quên check out chưa
            bool daDangKy = await _context.NghiPheps.AnyAsync(d => d.MaChamCong == chamCong.MaChamCong);
            if (daDangKy)
            {
                return Conflict("Đã xin nghỉ phép cho ngày này.");
            }

            // Tạo bản ghi mới
            var NghiPhep = new NghiPhep
            {
                MaChamCong = chamCong.MaChamCong,
                LiDo = LiDo,
                TinhTrang = "Chờ duyệt"
            };

            _context.NghiPheps.Add(NghiPhep);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Thông báo xin nghỉ phép thành công." });
        }

        [HttpGet("ChoDuyetNghiPhep")]
        public async Task<IActionResult> GetNghiPhepCD()
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            var query = _context.NghiPheps
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.MaChamCongNavigation.MaNhanVien == id && d.TinhTrang == "Chờ duyệt")
                .AsQueryable();

            var result = await query.Select(d => new
            {
                d.TinhTrang,
                d.LiDo,
                NgayChamCong = d.MaChamCongNavigation.NgayChamCong
            }).ToListAsync();

            if (!result.Any())
            {
                return NotFound("Không tìm thấy bản ghi phù hợp.");
            }

            return Ok(result);
        }

        [HttpGet("DaDuyetNghiPhep")]
        public async Task<IActionResult> GetNghiPhepDD()
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            var query = _context.NghiPheps
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.MaChamCongNavigation.MaNhanVien == id && d.TinhTrang == "Đã duyệt")
                .AsQueryable();

            var result = await query.Select(d => new
            {
                d.TinhTrang,
                d.LiDo,
                NgayChamCong = d.MaChamCongNavigation.NgayChamCong
            }).ToListAsync();

            if (!result.Any())
            {
                return NotFound("Không tìm thấy bản ghi phù hợp.");
            }

            return Ok(result);
        }

        [HttpGet("TuChoiNghiPhep")]
        public async Task<IActionResult> GetNghiPhepTC()
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            var query = _context.NghiPheps
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.MaChamCongNavigation.MaNhanVien == id && d.TinhTrang == "Từ chối")
                .AsQueryable();

            var result = await query.Select(d => new
            {
                d.TinhTrang,
                d.LiDo,
                NgayChamCong = d.MaChamCongNavigation.NgayChamCong
            }).ToListAsync();

            if (!result.Any())
            {
                return NotFound("Không tìm thấy bản ghi phù hợp.");
            }

            return Ok(result);
        }

        [HttpDelete("XoaNghiPhep")]
        public async Task<IActionResult> XoaNghiPhep(DateOnly ngayLam)
        {
            var idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "NhanVien")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            if (!int.TryParse(idString, out int id) || id <= 0)
            {
                return Unauthorized(new { Message = "Không tìm thấy thông tin người dùng trong token" });
            }

            var dangKy = await _context.NghiPheps
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.MaChamCongNavigation.MaNhanVien == id &&
                            d.MaChamCongNavigation.NgayChamCong == ngayLam)
                .FirstOrDefaultAsync();

            if (dangKy == null)
            {
                return NotFound("Không tìm thấy xin nghỉ phép.");
            }

            _context.NghiPheps.Remove(dangKy);
            await _context.SaveChangesAsync();

            return Ok("Xin nghỉ phép đã được xóa thành công.");
        }
    }
}
