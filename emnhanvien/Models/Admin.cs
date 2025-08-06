using System;
using System.Collections.Generic;

namespace emnhanvien.Models;

public partial class Admin
{
    public int MaAdmin { get; set; }

    public string TaiKhoan { get; set; } = null!;

    public string MatKhau { get; set; } = null!;
}
