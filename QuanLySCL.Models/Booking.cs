using System;

namespace QuanLySCL.Models
{
    public class Booking
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        private string _customer = string.Empty;
        public string Customer 
        { 
            get => string.IsNullOrWhiteSpace(_customer) ? "Khách vãng lai" : _customer;
            set => _customer = value;
        }
        public string Phone { get; set; } = string.Empty;
        public string Court { get; set; } = string.Empty;
        public string CourtId { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Today;
        public string Time { get; set; } = string.Empty;
        public string Type { get; set; } = "Casual"; // Casual, Fixed
        public string Status { get; set; } = "Pending"; // Pending, Checked-in, Completed, Cancelled
        public decimal Amount { get; set; }
        public bool IsMyBooking { get; set; }
    }
}
