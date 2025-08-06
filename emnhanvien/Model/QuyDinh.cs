using System;
using System.Collections.Generic;

namespace emnhanvien.Model;

public partial class QuyDinh
{
    public int MaQuyDinh { get; set; }

    public decimal MucPhatDiMuon { get; set; }

    public decimal TienPhatNghiQuaPhep { get; set; }

    public decimal TienPhatNghiKhongPhep { get; set; }

    public decimal TienThuongTangCa { get; set; }

    public int SoNgayPhepMotThang { get; set; }
}
