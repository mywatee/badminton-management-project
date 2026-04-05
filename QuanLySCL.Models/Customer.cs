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

        // Rank/Status (derived; not persisted)
        public string Status { get; set; } = "New"; // New, Silver, Gold, VIP
        public int RankProgress { get; set; } // 0..100 (progress to next tier)
        public string NextRank { get; set; } = string.Empty; // Silver/Gold/VIP or empty when maxed
    }
}
