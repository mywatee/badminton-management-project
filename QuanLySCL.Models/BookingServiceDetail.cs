using System;

namespace QuanLySCL.Models
{
    public class BookingServiceDetail
    {
        public string Id { get; set; } = string.Empty;
        public string BookingId { get; set; } = string.Empty;
        public string ServiceId { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        
        // Both names used in DAL
        public decimal UnitPrice { get; set; }
        public decimal Price { get => UnitPrice; set => UnitPrice = value; }
        
        public decimal Total => UnitPrice * Quantity;

        public BookingServiceDetail() { }

        public BookingServiceDetail(string bookingId, Service service, int quantity)
        {
            BookingId = bookingId ?? string.Empty;
            if (service != null)
            {
                ServiceId = service.Id ?? string.Empty;
                ServiceName = service.Name ?? string.Empty;
                UnitPrice = service.Price;
            }
            Quantity = quantity;
            // Generate a temporary ID or let DAL handle it
            Id = "TMP" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }
    }
}
