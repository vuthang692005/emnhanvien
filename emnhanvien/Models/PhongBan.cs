using System;
using System.Collections.Generic;

namespace emnhanvien.Models;

public partial class PhongBan
{
    public int MaPhongBan { get; set; }

    public string TenPhongBan { get; set; } = null!;

    public virtual ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();
}
