using System;
using System.Collections.Generic;

namespace QuanLySCL.Models
{
    public class RevenueByMonth
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class TopCustomerReport
    {
        public string Name { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class RevenueByCategory
    {
        public string Category { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class CourtUtilization
    {
        public string CourtName { get; set; } = string.Empty;
        public double UtilizationRate { get; set; }
    }

    public class ReportSummary
    {
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public decimal AvgRevenuePerDay { get; set; }
        public double AvgUtilization { get; set; }
        public decimal RevenueGrowth { get; set; }
        public int BookingGrowth { get; set; }
    }
}
