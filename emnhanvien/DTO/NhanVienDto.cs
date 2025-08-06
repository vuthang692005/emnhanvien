namespace emnhanvien.DTO
{
    public class NhanVienDto
    {
        public string HoTen { get; set; } = null!;
        public DateOnly NgaySinh { get; set; } 
        public string GioiTinh { get; set; } = null!;
        public string? SoDienThoai { get; set; } = null!;
        public string? Email { get; set; } = null!;
        public int MaPhongBan { get; set; } 
        public string ChucVu { get; set; } = null!;
        public decimal LuongCoBan { get; set; } 
       
    }
}
