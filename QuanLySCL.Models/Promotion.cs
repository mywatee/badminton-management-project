using System;

namespace QuanLySCL.Models
{
    public class Promotion
    {
        public string MaKM { get; set; } = string.Empty;
        public string TenKM { get; set; } = string.Empty;
        public string Kieu { get; set; } = "AMOUNT"; // PERCENT | AMOUNT
        public decimal GiaTri { get; set; }
        public decimal? DonToiThieu { get; set; }
        public DateTime? NgayBD { get; set; }
        public DateTime? NgayKT { get; set; }
        public bool TrangThai { get; set; } = true;
    }
}
