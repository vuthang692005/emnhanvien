using System;
using System.Collections.Generic;

namespace emnhanvien.Models;

public partial class DangKiTangCa
{
    public int Id { get; set; }

    public int MaChamCong { get; set; }

    public string TinhTrang { get; set; } = null!;

    public virtual ChamCong MaChamCongNavigation { get; set; } = null!;
}
