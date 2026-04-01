using System;

namespace QuanLySCL.Models
{
    public class CourtScheduleItem
    {
        // UI support properties
        public string DayName { get; set; } = string.Empty;
        public string DayNumber { get; set; } = string.Empty; // "30"
        public string TimeLabel { get; set; } = string.Empty; // "06:00 - 07:30"
        public string StatusKey { get; set; } = string.Empty; // "Available", "Pending", "Checked-in"
        public string StatusText { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public bool IsHighlighted { get; set; }
        public Booking? Booking { get; set; }

        // DAL support properties
        public string? BookingId { get; set; }
        public string? Customer { get; set; }
        public string? CustomerId { get; set; }
        public string? Phone { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
        public decimal Amount { get; set; }
        public string? CourtId { get; set; }
        public string? CourtName { get; set; }
        public string? SlotId { get; set; }
        public string? SlotName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DateTime Date { get; set; }
    }
}
