using System;
using System.Collections.Generic;

namespace emnhanvien.Models;

public partial class NghiPhep
{
    public int Id { get; set; }

    public int MaChamCong { get; set; }

    public string? LiDo { get; set; }

    public string TinhTrang { get; set; } = null!;

    public virtual ChamCong MaChamCongNavigation { get; set; } = null!;
}
