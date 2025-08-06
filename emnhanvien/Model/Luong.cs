using System;
using System.Collections.Generic;

namespace emnhanvien.Model;

public partial class Luong
{
    public int MaLuong { get; set; }

    public int MaNhanVien { get; set; }

    public int Thang { get; set; }

    public int Nam { get; set; }

    public decimal LuongCoBan { get; set; }

    public decimal LuongTangCa { get; set; }

    public decimal TienPhat { get; set; }

    public decimal? LuongTongCong { get; set; }

    public virtual NhanVien MaNhanVienNavigation { get; set; } = null!;
}
