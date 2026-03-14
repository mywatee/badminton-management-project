using System;
using System.Collections.Generic;
using System.Text;

namespace BadmintonManagement.DTO
{
    public class SanDTO
    {
        public string MaSan { get; set; } // varchar(10) trong SQL
        public string TenSan { get; set; } // nvarchar(50)
        public string MaLoaiSan { get; set; } // varchar(10)
        public string TrangThai { get; set; } // nvarchar(20)
    }
}
