using System;

namespace QuanLySCL.Models
{
    public class PriceEntry
    {
        public string Id { get; set; } // MaGia
        public string CourtTypeId { get; set; }
        public string CourtTypeName { get; set; }
        public string SlotId { get; set; }
        public string SlotName { get; set; }
        public string BookingType { get; set; } // "Lẻ" or "Cố định"
        public decimal Price { get; set; }

        public string Description => $"Loại: {CourtTypeName} | Ca: {SlotName} | Loại đặt: {BookingType}";
    }
}
