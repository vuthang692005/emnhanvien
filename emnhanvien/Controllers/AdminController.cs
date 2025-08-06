using System.Globalization;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using emnhanvien.DTO;
using emnhanvien.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace emnhanvien.Controllers
{
    [Route("api/admin")]
    [Authorize]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly QuanLyNhanSuContext _context;

        public AdminController(QuanLyNhanSuContext context)
        {
            _context = context;
        }

        [HttpGet("nhanvien")]
        public async Task<IActionResult> GetNhanVien(
            [FromQuery] int? maNhanVien,
            [FromQuery] string? hoTen,
            [FromQuery] int? maPhongBan,
            [FromQuery] string? chucVu,
            [FromQuery] int page = 1)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }
            int pageSize = 10; // Giá trị cố định

            var query = _context.NhanViens.AsQueryable();

            if (maNhanVien.HasValue)
            {
                query = query.Where(nv => nv.MaNhanVien == maNhanVien.Value);
            }

            if (!string.IsNullOrEmpty(hoTen))
            {
                query = query.Where(nv => nv.HoTen.Contains(hoTen));
            }

            if (maPhongBan.HasValue)
            {
                query = query.Where(nv => nv.MaPhongBan == maPhongBan.Value);
            }

            if (!string.IsNullOrEmpty(chucVu))
            {
                query = query.Where(nv => nv.ChucVu == chucVu.ToLower());
            }

            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var nhanViens = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(nv => new
                {
                    MaNhanVien = nv.MaNhanVien,
                    HoTen = nv.HoTen,
                    NgaySinh = nv.NgaySinh,
                    GioiTinh = nv.GioiTinh,
                    SoDienThoai = nv.SoDienThoai,
                    Email = nv.Email,
                    MaPhongBan = nv.MaPhongBanNavigation.TenPhongBan,
                    ChucVu = nv.ChucVu,
                    NgayVaoLam = nv.NgayVaoLam,
                    LuongCoBan = nv.LuongCoBan
                })
                .ToListAsync();

            if (!nhanViens.Any())
            {
                return NotFound("Không tìm thấy nhân viên nào.");
            }

            return Ok(new
            {
                TotalPages = totalPages,
                CurrentPage = page,
                Data = nhanViens
            });
        }




        [HttpPost("nhanvien")]
        public async Task<IActionResult> AddNhanVien([FromBody] NhanVienDto nhanVienDto)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            if (nhanVienDto == null)
                return BadRequest("Dữ liệu không hợp lệ");
            string HoTen = nhanVienDto.HoTen;
            string TaiKhoan = RemoveDiacritics(HoTen).Replace(" ", "") + nhanVienDto.SoDienThoai;

            // Kiểm tra trùng tài khoản hoặc email
            bool isDuplicate = await _context.NhanViens.AnyAsync(nv =>
                nv.Email == nhanVienDto.Email ||
                nv.SoDienThoai == nhanVienDto.SoDienThoai);

            if (isDuplicate)
                return Conflict("SĐT hoặc email đã tồn tại");

            // Mã hóa mật khẩu trước khi lưu
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword("12345678");

            // Tạo đối tượng NhanVien
            var nhanVien = new NhanVien
            {
                HoTen = nhanVienDto.HoTen,
                NgaySinh = nhanVienDto.NgaySinh,
                GioiTinh = nhanVienDto.GioiTinh,
                SoDienThoai = nhanVienDto.SoDienThoai,
                Email = nhanVienDto.Email,
                MaPhongBan = nhanVienDto.MaPhongBan,
                ChucVu = nhanVienDto.ChucVu,
                NgayVaoLam = DateOnly.FromDateTime(DateTime.Today),
                LuongCoBan = nhanVienDto.LuongCoBan,
                TaiKhoan = TaiKhoan,
                MatKhau = hashedPassword
            };

            _context.NhanViens.Add(nhanVien);
            await _context.SaveChangesAsync();

            return Created("",new { Message = "Nhân viên được tạo thành công" });

        }

        public static string RemoveDiacritics(string text)
        {
            string normalized = text.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            foreach (char c in normalized)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        [HttpPut("nhanvien/{id}")]
        public async Task<IActionResult> UpdateNhanVien(int id, [FromBody] NhanVienUpdateDto updatedNhanVienDto)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            if (updatedNhanVienDto == null)
                return BadRequest("Dữ liệu không hợp lệ");

            var existingNhanVien = await _context.NhanViens.FindAsync(id);
            if (existingNhanVien == null)
                return NotFound("Không tìm thấy nhân viên");

            // Tìm MaPhongBan theo TenPhongBan
            var phongBan = await _context.PhongBans
                .FirstOrDefaultAsync(pb => pb.TenPhongBan == updatedNhanVienDto.TenPhongBan);
            if (phongBan == null)
                return NotFound("Không tìm thấy phòng ban");

            // Cập nhật thông tin nhân viên
            existingNhanVien.MaPhongBan = phongBan.MaPhongBan;
            existingNhanVien.ChucVu = updatedNhanVienDto.ChucVu;
            existingNhanVien.LuongCoBan = updatedNhanVienDto.LuongCoBan;

            await _context.SaveChangesAsync();
            return Ok(new
            {
                Message = "Cập nhật nhân viên thành công",
            });
        }



        [HttpDelete("nhanvien/{id}")]
        public async Task<IActionResult> DeleteNhanVien(int id)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien == null)
                return NotFound("Không tìm thấy nhân viên");

            _context.NhanViens.Remove(nhanVien);
            await _context.SaveChangesAsync();
            return Ok("Xóa nhân viên thành công");
        }

        [HttpPost("tao-cham-cong")]
        public async Task<IActionResult> TaoChamCong([FromQuery] DateOnly ngayChamCong)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            // Lấy danh sách tất cả nhân viên
            var danhSachNhanVien = await _context.NhanViens.ToListAsync();

            if (danhSachNhanVien.Count == 0)
            {
                return BadRequest("Không có nhân viên nào trong hệ thống.");
            }

            // Tạo danh sách chấm công mới
            var chamCongs = danhSachNhanVien.Select(nv => new ChamCong
            {
                MaNhanVien = nv.MaNhanVien,
                NgayChamCong = ngayChamCong,
                GioVao = null,
                GioRa = null,
                TinhTrang = null,
                TangCa = "False",
                SoGioTangCa = 0
            }).ToList();

            // Thêm vào database
            await _context.ChamCongs.AddRangeAsync(chamCongs);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Chấm công đã được tạo thành công!", ngayChamCong });
        }

        [HttpGet("KiemTraNgay")]
        public async Task<IActionResult> KiemTraNgay([FromQuery] DateOnly ngayChamCong)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var danhSachChamCong = await _context.ChamCongs
                .Where(u => u.NgayChamCong == ngayChamCong)
                .Select(u => new
                {
                    u.MaChamCong,
                    u.MaNhanVien,
                    TenNhanVien = u.MaNhanVienNavigation.HoTen, // Giả sử cột họ tên là HoTen
                    u.NgayChamCong,
                    u.GioVao,
                    u.GioRa,
                    u.TinhTrang,
                    u.TangCa,
                    u.SoGioTangCa
                })
                .ToListAsync();

            if (!danhSachChamCong.Any())
            {
                return BadRequest("Không có ngày nào trong hệ thống.");
            }
            return Ok(danhSachChamCong);
        }


        [HttpPost("XoaChamCong")]
        public async Task<IActionResult> XoaChamCong([FromQuery] DateOnly ngayChamCong)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var danhSachChamCong = await _context.ChamCongs
                .Where(u => u.NgayChamCong == ngayChamCong)
                .ToListAsync();

            if (!danhSachChamCong.Any())
            {
                return BadRequest("Không có ngày nào trong hệ thống.");
            }

            _context.ChamCongs.RemoveRange(danhSachChamCong);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa chấm công của ngày " + ngayChamCong });
        }

        [HttpPut("SuaChamCong")]
        public async Task<IActionResult> SuaChamCong(int maChamCong, [FromBody] ChamCongUpdateDto model)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var chamCong = await _context.ChamCongs.FindAsync(maChamCong);

            if (chamCong == null)
            {
                return NotFound(new { message = "Không tìm thấy bản ghi chấm công!" });
            }

            // Cập nhật nếu có dữ liệu nhập vào
            chamCong.TinhTrang = model.TinhTrang;
            chamCong.TangCa = model.TangCa;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thành công!"});
        }

        [HttpGet("quydinh")]
        public async Task<IActionResult> GetQuyDinh()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var quyDinh = await _context.QuyDinhs
                .Where(qd => qd.MaQuyDinh == 1)
                .Select(qd => new {
                    qd.MucPhatDiMuon,
                    qd.TienPhatNghiQuaPhep,
                    qd.TienPhatNghiKhongPhep,
                    qd.TienThuongTangCa,
                    qd.SoNgayPhepMotThang
                })
                .FirstOrDefaultAsync();

            if (quyDinh == null)
            {
                return NotFound("Không tìm thấy quy định.");
            }

            return Ok(quyDinh);
        }

        [HttpPut("quydinh")]
        public async Task<IActionResult> UpdateQuyDinh([FromBody] QuyDinh model)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var quyDinh = await _context.QuyDinhs.FirstOrDefaultAsync(qd => qd.MaQuyDinh == 1);

            if (quyDinh == null)
            {
                return NotFound("Không tìm thấy quy định.");
            }

            quyDinh.MucPhatDiMuon = model.MucPhatDiMuon;
            quyDinh.TienPhatNghiQuaPhep = model.TienPhatNghiQuaPhep;
            quyDinh.TienPhatNghiKhongPhep = model.TienPhatNghiKhongPhep;
            quyDinh.TienThuongTangCa = model.TienThuongTangCa;
            quyDinh.SoNgayPhepMotThang = model.SoNgayPhepMotThang;

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("ChoDuyetDKTangCa")]
        public async Task<IActionResult> GetDangKyTangCaCD()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var query = _context.DangKiTangCas
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.TinhTrang == "Chờ duyệt")
                .AsQueryable();

            var result = await query.Select(d => new
            {
                d.TinhTrang,
                NgayChamCong = d.MaChamCongNavigation.NgayChamCong,
                MaNhanVien = d.MaChamCongNavigation.MaNhanVien,
                HoTen = d.MaChamCongNavigation.MaNhanVienNavigation.HoTen
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
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var query = _context.DangKiTangCas
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.TinhTrang == "đã duyệt")
                .AsQueryable();

            var result = await query.Select(d => new
            {
                d.TinhTrang,
                NgayChamCong = d.MaChamCongNavigation.NgayChamCong,
                MaNhanVien = d.MaChamCongNavigation.MaNhanVien,
                HoTen = d.MaChamCongNavigation.MaNhanVienNavigation.HoTen
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
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            var query = _context.DangKiTangCas
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.TinhTrang == "Từ chối")
                .AsQueryable();

            var result = await query.Select(d => new
            {
                d.TinhTrang,
                NgayChamCong = d.MaChamCongNavigation.NgayChamCong,
                MaNhanVien = d.MaChamCongNavigation.MaNhanVien,
                HoTen = d.MaChamCongNavigation.MaNhanVienNavigation.HoTen
            }).ToListAsync();

            if (!result.Any())
            {
                return NotFound("Không tìm thấy đăng ký tăng ca phù hợp.");
            }

            return Ok(result);
        }

        [HttpPut("DuyetDKTC")]
        public async Task<IActionResult> duyetDKTC([FromQuery] int maNhanVien, [FromQuery] DateOnly ngayChamCong)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var chamCong = await _context.ChamCongs
                .FirstOrDefaultAsync(c => c.MaNhanVien == maNhanVien && c.NgayChamCong == ngayChamCong);

            if (chamCong == null)
            {
                return NotFound("Không tìm thấy bản ghi chấm công.");
            }

            var tangCa = await _context.DangKiTangCas
                .FirstOrDefaultAsync(c => c.MaChamCong == chamCong.MaChamCong);

            if (tangCa == null)
            {
                return NotFound("Không tìm thấy bản ghi tăng ca.");
            }

            chamCong.TangCa = "True";
            tangCa.TinhTrang = "Đã duyệt";

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã duyệt đăng kí tăng ca!" });
        }

        [HttpPut("TuChoiDKTC")]
        public async Task<IActionResult> tuChoiDKTC([FromQuery] int maNhanVien, [FromQuery] DateOnly ngayChamCong)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var chamCong = await _context.ChamCongs
                .FirstOrDefaultAsync(c => c.MaNhanVien == maNhanVien && c.NgayChamCong == ngayChamCong);

            if (chamCong == null)
            {
                return NotFound("Không tìm thấy bản ghi chấm công.");
            }

            var tangCa = await _context.DangKiTangCas
                .FirstOrDefaultAsync(c => c.MaChamCong == chamCong.MaChamCong);

            if (tangCa == null)
            {
                return NotFound("Không tìm thấy bản ghi tăng ca.");
            }

            chamCong.TangCa = "False";
            tangCa.TinhTrang = "Từ chối";

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã từ chối đăng kí tăng ca!" });
        }

        [HttpGet("ChoDuyetQCO")]
        public async Task<IActionResult> GetQuenCheckOutCD()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var query = _context.QuenCheckOuts
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.TinhTrang == "Chờ duyệt")
                .AsQueryable();

            var result = await query.Select(d => new
            {
                d.TinhTrang,
                d.LiDo,
                NgayChamCong = d.MaChamCongNavigation.NgayChamCong,
                MaNhanVien = d.MaChamCongNavigation.MaNhanVien,
                HoTen = d.MaChamCongNavigation.MaNhanVienNavigation.HoTen
            }).ToListAsync();

            if (!result.Any())
            {
                return NotFound("Không tìm thấy bản ghi phù hợp.");
            }

            return Ok(result);
        }

        [HttpGet("DaDuyetQCO")]
        public async Task<IActionResult> GetQuenCheckOutDD()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var query = _context.QuenCheckOuts
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.TinhTrang == "đã duyệt")
                .AsQueryable();

            var result = await query.Select(d => new
            {
                d.TinhTrang,
                d.LiDo,
                NgayChamCong = d.MaChamCongNavigation.NgayChamCong,
                MaNhanVien = d.MaChamCongNavigation.MaNhanVien,
                HoTen = d.MaChamCongNavigation.MaNhanVienNavigation.HoTen
            }).ToListAsync();

            if (!result.Any())
            {
                return NotFound("Không tìm thấy bản ghi phù hợp.");
            }

            return Ok(result);
        }

        [HttpGet("TuChoiQCO")]
        public async Task<IActionResult> GetQuenCheckOutTC()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            var query = _context.QuenCheckOuts
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.TinhTrang == "Từ chối")
                .AsQueryable();

            var result = await query.Select(d => new
            {
                d.TinhTrang,
                d.LiDo,
                NgayChamCong = d.MaChamCongNavigation.NgayChamCong,
                MaNhanVien = d.MaChamCongNavigation.MaNhanVien,
                HoTen = d.MaChamCongNavigation.MaNhanVienNavigation.HoTen
            }).ToListAsync();

            if (!result.Any())
            {
                return NotFound("Không tìm thấy bản ghi phù hợp.");
            }

            return Ok(result);
        }

        [HttpPut("DuyetQCO")]
        public async Task<IActionResult> duyetQCO([FromQuery] int maNhanVien, [FromQuery] DateOnly ngayChamCong)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var chamCong = await _context.ChamCongs
                .FirstOrDefaultAsync(c => c.MaNhanVien == maNhanVien && c.NgayChamCong == ngayChamCong);

            if (chamCong == null)
            {
                return NotFound("Không tìm thấy bản ghi chấm công.");
            }

            var checkOut = await _context.QuenCheckOuts
                .FirstOrDefaultAsync(c => c.MaChamCong == chamCong.MaChamCong);

            if (checkOut == null)
            {
                return NotFound("Không tìm thấy bản ghi quên check-out.");
            }

            if (chamCong.GioVao <= new TimeOnly(8, 30))
            {
                chamCong.TinhTrang = "Có mặt";
            }
            else
            {
                chamCong.TinhTrang = "Muộn";
            }

            checkOut.TinhTrang = "Đã duyệt";

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã duyệt Quên Check-Out thành công!" });
        }

        [HttpPut("TuChoiQCO")]
        public async Task<IActionResult> tuChoiQCO([FromQuery] int maNhanVien, [FromQuery] DateOnly ngayChamCong)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var chamCong = await _context.ChamCongs
                .FirstOrDefaultAsync(c => c.MaNhanVien == maNhanVien && c.NgayChamCong == ngayChamCong);

            if (chamCong == null)
            {
                return NotFound("Không tìm thấy bản ghi chấm công.");
            }

            var checkOut = await _context.QuenCheckOuts
                .FirstOrDefaultAsync(c => c.MaChamCong == chamCong.MaChamCong);

            if (checkOut == null)
            {
                return NotFound("Không tìm thấy bản ghi quên check-out.");
            }

            chamCong.TinhTrang = null;
            checkOut.TinhTrang = "Từ chối";

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã từ chối quên check-out!" });
        }

        [HttpGet("ChoDuyetNghiPhep")]
        public async Task<IActionResult> GetNghiPhepCD()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var query = _context.NghiPheps
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.TinhTrang == "Chờ duyệt")
                .AsQueryable();

            var result = await query.Select(d => new
            {
                d.TinhTrang,
                d.LiDo,
                NgayChamCong = d.MaChamCongNavigation.NgayChamCong,
                MaNhanVien = d.MaChamCongNavigation.MaNhanVien,
                HoTen = d.MaChamCongNavigation.MaNhanVienNavigation.HoTen
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
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var query = _context.NghiPheps
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.TinhTrang == "đã duyệt")
                .AsQueryable();

            var result = await query.Select(d => new
            {
                d.TinhTrang,
                d.LiDo,
                NgayChamCong = d.MaChamCongNavigation.NgayChamCong,
                MaNhanVien = d.MaChamCongNavigation.MaNhanVien,
                HoTen = d.MaChamCongNavigation.MaNhanVienNavigation.HoTen
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
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là nhân viên để dùng chức năng này" });
            }

            var query = _context.NghiPheps
                .Include(d => d.MaChamCongNavigation)
                .Where(d => d.TinhTrang == "Từ chối")
                .AsQueryable();

            var result = await query.Select(d => new
            {
                d.TinhTrang,
                d.LiDo,
                NgayChamCong = d.MaChamCongNavigation.NgayChamCong,
                MaNhanVien = d.MaChamCongNavigation.MaNhanVien,
                HoTen = d.MaChamCongNavigation.MaNhanVienNavigation.HoTen
            }).ToListAsync();

            if (!result.Any())
            {
                return NotFound("Không tìm thấy bản ghi phù hợp.");
            }

            return Ok(result);
        }

        [HttpPut("DuyetNghiPhep")]
        public async Task<IActionResult> duyetNghiPhep([FromQuery] int maNhanVien, [FromQuery] DateOnly ngayChamCong)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var chamCong = await _context.ChamCongs
                .FirstOrDefaultAsync(c => c.MaNhanVien == maNhanVien && c.NgayChamCong == ngayChamCong);

            if (chamCong == null)
            {
                return NotFound("Không tìm thấy bản ghi chấm công.");
            }

            var checkOut = await _context.NghiPheps
                .FirstOrDefaultAsync(c => c.MaChamCong == chamCong.MaChamCong);

            if (checkOut == null)
            {
                return NotFound("Không tìm thấy bản ghi xin nghỉ phép.");
            }

            chamCong.TinhTrang = "Nghỉ phép";
            checkOut.TinhTrang = "Đã duyệt";

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã duyệt xin nghỉ phép thành công!" });
        }

        [HttpPut("TuChoiNghiPhep")]
        public async Task<IActionResult> tuChoiNghiPhep([FromQuery] int maNhanVien, [FromQuery] DateOnly ngayChamCong)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var chamCong = await _context.ChamCongs
                .FirstOrDefaultAsync(c => c.MaNhanVien == maNhanVien && c.NgayChamCong == ngayChamCong);

            if (chamCong == null)
            {
                return NotFound("Không tìm thấy bản ghi chấm công.");
            }

            var checkOut = await _context.NghiPheps
                .FirstOrDefaultAsync(c => c.MaChamCong == chamCong.MaChamCong);

            if (checkOut == null)
            {
                return NotFound("Không tìm thấy bản ghi xin nghỉ phép.");
            }

            chamCong.TinhTrang = null;
            checkOut.TinhTrang = "Từ chối";

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã từ chối xin nghỉ phép!" });
        }

        [HttpPost("TinhLuongThang")]
        public async Task<IActionResult> TinhLuongThang()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            // Tính tháng và năm của tháng trước
            int thang = currentMonth == 1 ? 12 : currentMonth - 1;
            int nam = currentMonth == 1 ? currentYear - 1 : currentYear;

            var nhanViens = await _context.NhanViens.ToListAsync();
            if (!nhanViens.Any()) return NotFound("Không có nhân viên nào trong hệ thống.");

            var quyDinh = await _context.QuyDinhs.FirstOrDefaultAsync(q => q.MaQuyDinh == 1);
            if (quyDinh == null) return BadRequest("Chưa có quy định lương.");

            // Lấy toàn bộ chấm công trong tháng để xử lý
            var chamCongThang = await _context.ChamCongs
                .Where(c => c.NgayChamCong.Month == thang && c.NgayChamCong.Year == nam)
                .ToListAsync();

            if (!chamCongThang.Any())
            {
                return BadRequest(new { message = $"Không có chấm công nào trong tháng {thang}/{nam}." });
            }

            // Lấy danh sách lương đã tính để tránh trùng
            var daTinhLuong = await _context.Luongs
                .AnyAsync(l => l.Thang == thang && l.Nam == nam);

            if (daTinhLuong)
            {
                return BadRequest(new { message = $"Lương tháng {thang}/{nam} đã được tính." });
            }

            List<Luong> bangLuongsMoi = new List<Luong>();

            foreach (var nhanVien in nhanViens)
            {
                var chamCongs = chamCongThang
                    .Where(c => c.MaNhanVien == nhanVien.MaNhanVien)
                    .ToList();

                if (!chamCongs.Any()) continue;

                // Tính Lương
                decimal tongGioTangCa = chamCongs.Sum(c => c.SoGioTangCa);

                int ngayDiMuon = chamCongs.Count(c => c.TinhTrang == "Muộn");
                int nghiPhep = chamCongs.Count(c => c.TinhTrang == "Nghỉ phép");
                int nghiKoPhep = chamCongs.Count(c => c.TinhTrang == null);

                decimal tienPhatDiMuon = ngayDiMuon * quyDinh.MucPhatDiMuon;
                decimal tienPhatNghiQuaPhep = Math.Max(0, nghiPhep - quyDinh.SoNgayPhepMotThang) * quyDinh.TienPhatNghiQuaPhep;
                decimal tienPhatNghiKoPhep = nghiKoPhep * quyDinh.TienPhatNghiKhongPhep;

                decimal luongCoBan = nhanVien.LuongCoBan;
                decimal luongTangCa = tongGioTangCa * quyDinh.TienThuongTangCa;
                decimal tienPhat = tienPhatDiMuon + tienPhatNghiQuaPhep + tienPhatNghiKoPhep;

                // Đảm bảo lương ko âm
                decimal tongLuongTruocPhat = luongCoBan + luongTangCa;
                tienPhat = Math.Min(tienPhat, tongLuongTruocPhat);

                bangLuongsMoi.Add(new Luong
                {
                    MaNhanVien = nhanVien.MaNhanVien,
                    Thang = thang,
                    Nam = nam,
                    LuongCoBan = luongCoBan,
                    LuongTangCa = luongTangCa,
                    TienPhat = tienPhat
                });
            }

            if (bangLuongsMoi.Any())
            {
                _context.Luongs.AddRange(bangLuongsMoi);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = $"Tính lương tháng {thang}/{nam} thành công!" });
        }

        [HttpGet("luong")]
        public async Task<IActionResult> GetLuong(
            [FromQuery] int? maNhanVien,
            [FromQuery] string? hoTen,
            [FromQuery] int? thang,
            [FromQuery] int? nam,
            [FromQuery] int page = 1)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }
            int pageSize = 10; // Giá trị cố định

            var query = _context.Luongs.AsQueryable();

            if (maNhanVien.HasValue)
            {
                query = query.Where(nv => nv.MaNhanVien == maNhanVien.Value);
            }

            if (!string.IsNullOrEmpty(hoTen))
            {
                query = query.Where(nv => nv.MaNhanVienNavigation.HoTen.Contains(hoTen));
            }

            if (thang.HasValue)
            {
                query = query.Where(nv => nv.Thang == thang.Value);
            }

            if (nam.HasValue)
            {
                query = query.Where(nv => nv.Nam == nam.Value);
            }

            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var luongs = await query
                .OrderByDescending(l => l.MaLuong)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(nv => new
                {
                    MaNhanVien = nv.MaNhanVien,
                    HoTen = nv.MaNhanVienNavigation.HoTen,
                    Nam = nv.Nam,
                    Thang = nv.Thang,
                    LuongCoBan = nv.LuongCoBan,
                    LuongTangCa = nv.LuongTangCa,
                    TienPhat = nv.TienPhat,
                    TongCong = nv.LuongTongCong,
                })
                .ToListAsync();

            return Ok(new
            {
                TotalPages = totalPages,
                CurrentPage = page,
                Data = luongs
            });
        }

        [HttpGet("chiTietLuong")]
        public async Task<IActionResult> GetChiTietLuong(
            [FromQuery] int maNhanVien,
            [FromQuery] int thang,
            [FromQuery] int nam)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            var chamCongThang = await _context.ChamCongs
                .Where(c => c.NgayChamCong.Month == thang && c.NgayChamCong.Year == nam && c.MaNhanVien == maNhanVien)
                .ToListAsync();

            if (!chamCongThang.Any())
            {
                return BadRequest($"Không có chấm công nào trong tháng {thang}/{nam}."); // Trả về lỗi nếu không có dữ liệu
            }

            decimal tongGioTangCa = chamCongThang.Sum(c => c.SoGioTangCa);
            int ngayDiMuon = chamCongThang.Count(c => c.TinhTrang == "Muộn");
            int nghiPhep = chamCongThang.Count(c => c.TinhTrang == "Nghỉ phép");
            int nghiKoPhep = chamCongThang.Count(c => c.TinhTrang == null);

            return Ok(new
            {
                TongGioTangCa = tongGioTangCa,
                NgayDiMuon = ngayDiMuon,
                NghiPhep = nghiPhep,
                NghiKoPhep = nghiKoPhep
            });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var maAdminClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(maAdminClaim) || !int.TryParse(maAdminClaim, out int maAdmin))
            {
                return BadRequest("Không thể xác định người dùng.");
            }

            if (role != "Admin")
            {
                return Unauthorized(new { Message = "Người dùng phải là admin để dùng chức năng này" });
            }

            if (request == null || string.IsNullOrWhiteSpace(request.MatKhauCu) || string.IsNullOrWhiteSpace(request.MatKhauMoi) || string.IsNullOrWhiteSpace(request.NhapLaiMatKhau))
            {
                return BadRequest("Dữ liệu không hợp lệ. Vui lòng điền đầy đủ thông tin.");
            }

            var taiKhoan = await _context.Admins
                .FirstOrDefaultAsync(a => a.MaAdmin == maAdmin);

            if (taiKhoan == null)
            {
                return BadRequest("Tài khoản không tồn tại.");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.MatKhauCu, taiKhoan.MatKhau))
            {
                return BadRequest("Mật khẩu cũ không đúng.");
            }

            if (request.MatKhauMoi != request.NhapLaiMatKhau)
            {
                return BadRequest("Mật khẩu mới và nhập lại mật khẩu không khớp.");
            }

            taiKhoan.MatKhau = BCrypt.Net.BCrypt.HashPassword(request.MatKhauMoi);
            await _context.SaveChangesAsync();

            return Ok("Đổi mật khẩu thành công.");
        }

    }
}
