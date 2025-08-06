using System;
using System.Collections.Generic;

namespace emnhanvien.Model;

public partial class ChamCong
{
    public int MaChamCong { get; set; }

    public int MaNhanVien { get; set; }

    public DateOnly NgayChamCong { get; set; }

    public TimeOnly? GioVao { get; set; }

    public TimeOnly? GioRa { get; set; }

    public string? TinhTrang { get; set; }

    public string TangCa { get; set; } = null!;

    public decimal SoGioTangCa { get; set; }

    public virtual DangKiTangCa? DangKiTangCa { get; set; }

    public virtual NhanVien MaNhanVienNavigation { get; set; } = null!;

    public virtual NghiPhep? NghiPhep { get; set; }

    public virtual QuenCheckOut? QuenCheckOut { get; set; }
}
