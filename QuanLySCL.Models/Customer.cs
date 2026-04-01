using System;

namespace QuanLySCL.Models
{
    public class Customer
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime MemberSince { get; set; }
        public string Status { get; set; } = "New"; // VIP, Regular, New
    }
}
