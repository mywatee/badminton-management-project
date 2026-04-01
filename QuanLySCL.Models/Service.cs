namespace QuanLySCL.Models
{
    public class Service
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int LowStockThreshold { get; set; } = 5;

        public bool IsOutOfStock => Category != "Equipment" && Stock <= 0;
        public bool IsLowStock => Category != "Equipment" && Stock > 0 && Stock <= LowStockThreshold;
    }
}
