namespace QuanLySCL.Models
{
    public class Court
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; } // LoaiSan (string name)
        public string Status { get; set; } = "Available";
        public string TypeId { get; set; } = string.Empty; // MaLoaiSan
        public bool IsMyBooking { get; set; }
    }
    
    public class CourtType
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
    }
}
