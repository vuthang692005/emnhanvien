using System;
using System.Collections.Generic;

namespace emnhanvien.Model;

public partial class NhanVien
{
    public int MaNhanVien { get; set; }

    public string HoTen { get; set; } = null!;

    public string TaiKhoan { get; set; } = null!;

    public string MatKhau { get; set; } = null!;

    public DateOnly NgaySinh { get; set; }

    public string GioiTinh { get; set; } = null!;

    public string SoDienThoai { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int MaPhongBan { get; set; }

    public string ChucVu { get; set; } = null!;

    public DateOnly NgayVaoLam { get; set; }

    public decimal LuongCoBan { get; set; }

    public virtual ICollection<ChamCong> ChamCongs { get; set; } = new List<ChamCong>();

    public virtual ICollection<Luong> Luongs { get; set; } = new List<Luong>();

    public virtual PhongBan MaPhongBanNavigation { get; set; } = null!;
}
