using System;

namespace QuanLySCL.Models
{
    public class CompactPriceEntry
    {
        public string CourtTypeId { get; set; }
        public string CourtTypeName { get; set; }
        public string SlotId { get; set; }
        public string SlotName { get; set; }
        
        public decimal PriceLe { get; set; }
        public string IdLe { get; set; } // MaGia for "Lẻ"
        
        public decimal PriceFixed { get; set; }
        public string IdFixed { get; set; } // MaGia for "Cố định"

        public string DisplayLabel => $"{CourtTypeName} - {SlotName}";
    }
}
