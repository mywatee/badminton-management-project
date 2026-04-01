using System;
using System.Collections.Generic;
using System.Data;
using QuanLySCL.Models;

namespace QuanLySCL.DAL
{
    public class ReportingDAL : BaseDAL
    {
        public ReportSummary GetReportSummary(string timeRange)
        {
            // Simplified summary calculation based on timeRange (Daily, Weekly, Monthly, Yearly)
            string dateFilter = GetDateFilter(timeRange);
            string prevDateFilter = GetPreviousDateFilter(timeRange);

            string query = $@"
                SELECT 
                    ISNULL(SUM(TongTienSan + TongTienDV - SoTienGiam), 0) AS TotalRevenue,
                    COUNT(MaPhieuDat) AS TotalBookings
                FROM HOA_DON
                WHERE {dateFilter.Replace("NgayLapPhieu", "NgayXuat")}";

            string prevQuery = $@"
                SELECT 
                    ISNULL(SUM(TongTienSan + TongTienDV - SoTienGiam), 0) AS PrevRevenue,
                    COUNT(MaPhieuDat) AS PrevBookings
                FROM HOA_DON
                WHERE {prevDateFilter.Replace("NgayLapPhieu", "NgayXuat")}";

            DataTable dt = ExecuteQuery(query);
            DataTable dtPrev = ExecuteQuery(prevQuery);

            decimal revenue = Convert.ToDecimal(dt.Rows[0]["TotalRevenue"]);
            int bookings = Convert.ToInt32(dt.Rows[0]["TotalBookings"]);
            decimal prevRevenue = Convert.ToDecimal(dtPrev.Rows[0]["PrevRevenue"]);
            int prevBookings = Convert.ToInt32(dtPrev.Rows[0]["PrevBookings"]);

            decimal growth = prevRevenue > 0 ? ((revenue - prevRevenue) / prevRevenue) * 100 : 0;
            
            return new ReportSummary
                {
                    TotalRevenue = revenue,
                    TotalBookings = bookings,
                    AvgRevenuePerDay = revenue / GetDaysInTimeRange(timeRange),
                    RevenueGrowth = Math.Round(growth, 1)
                };
        }

        public List<RevenueByMonth> GetRevenueTrends(string timeRange)
        {
            var list = new List<RevenueByMonth>();
            string dateFilter = GetDateFilter(timeRange).Replace("NgayLapPhieu", "NgayXuat");

            string format = "'MM/yyyy'";
            string groupBy = "YEAR(NgayXuat), MONTH(NgayXuat)";
            
            if (timeRange == "Theo ngày")
            {
                format = "'HH:00'";
                groupBy = "DATEPART(HOUR, NgayXuat)";
            }
            else if (timeRange == "Theo tuần" || timeRange == "Theo tháng")
            {
                format = "'dd/MM'";
                groupBy = "YEAR(NgayXuat), MONTH(NgayXuat), DAY(NgayXuat)";
            }

            string query = $@"
                SELECT 
                    FORMAT(NgayXuat, {format}) AS [Month],
                    SUM(TongTienSan + TongTienDV - SoTienGiam) AS Revenue
                FROM HOA_DON
                WHERE {dateFilter}
                GROUP BY FORMAT(NgayXuat, {format}), {groupBy}
                ORDER BY {groupBy}";

            DataTable dt = ExecuteQuery(query);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new RevenueByMonth
                {
                    Month = row["Month"]?.ToString() ?? string.Empty,
                    Revenue = Convert.ToDecimal(row["Revenue"])
                });
            }
            return list;
        }

        public List<TopCustomerReport> GetTopCustomers(int top = 5, string timeRange = "Theo năm")
        {
            var list = new List<TopCustomerReport>();
            string dateFilter = GetDateFilter(timeRange).Replace("NgayLapPhieu", "HD.NgayXuat");

            string query = $@"
                SELECT TOP {top}
                    KH.HoTen,
                    COUNT(DS.MaPhieuDat) AS TotalBookings,
                    SUM(HD.TongTienSan + HD.TongTienDV - HD.SoTienGiam) AS TotalSpent
                FROM KHACH_HANG KH
                JOIN DAT_SAN DS ON KH.MaKH = DS.MaKH
                JOIN HOA_DON HD ON DS.MaPhieuDat = HD.MaPhieuDat
                WHERE {dateFilter}
                GROUP BY KH.MaKH, KH.HoTen
                ORDER BY TotalSpent DESC";

            DataTable dt = ExecuteQuery(query);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new TopCustomerReport
                {
                    Name = row["HoTen"]?.ToString() ?? string.Empty,
                    TotalBookings = Convert.ToInt32(row["TotalBookings"]),
                    TotalSpent = Convert.ToDecimal(row["TotalSpent"])
                });
            }
            return list;
        }

        public List<RevenueByCategory> GetRevenueByCategory(string timeRange)
        {
            var list = new List<RevenueByCategory>();
            string dateFilter = GetDateFilter(timeRange).Replace("NgayLapPhieu", "NgayXuat");

            // Court Revenue vs Service Revenue
            string query = $@"
                SELECT 'Courts' AS Category, SUM(TongTienSan) AS Revenue FROM HOA_DON WHERE {dateFilter}
                UNION ALL
                SELECT 'Services' AS Category, SUM(TongTienDV) AS Revenue FROM HOA_DON WHERE {dateFilter}";

            DataTable dt = ExecuteQuery(query);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new RevenueByCategory
                {
                    Category = row["Category"]?.ToString() ?? string.Empty,
                    Revenue = row["Revenue"] != DBNull.Value ? Convert.ToDecimal(row["Revenue"]) : 0
                });
            }
            return list;
        }

        private string GetDateFilter(string range) => range switch
        {
            "Theo ngày" => "CAST(NgayLapPhieu AS DATE) = CAST(GETDATE() AS DATE)",
            "Theo tuần" => "DATEPART(WEEK, NgayLapPhieu) = DATEPART(WEEK, GETDATE()) AND YEAR(NgayLapPhieu) = YEAR(GETDATE())",
            "Theo tháng" => "MONTH(NgayLapPhieu) = MONTH(GETDATE()) AND YEAR(NgayLapPhieu) = YEAR(GETDATE())",
            "Theo năm" => "YEAR(NgayLapPhieu) = YEAR(GETDATE())",
            _ => "1=1"
        };

        private string GetPreviousDateFilter(string range) => range switch
        {
            "Theo ngày" => "CAST(NgayLapPhieu AS DATE) = CAST(DATEADD(DAY, -1, GETDATE()) AS DATE)",
            "Theo tuần" => "DATEPART(WEEK, NgayLapPhieu) = DATEPART(WEEK, DATEADD(WEEK, -1, GETDATE())) AND YEAR(NgayLapPhieu) = YEAR(DATEADD(WEEK, -1, GETDATE()))",
            "Theo tháng" => "MONTH(NgayLapPhieu) = MONTH(DATEADD(MONTH, -1, GETDATE())) AND YEAR(NgayLapPhieu) = YEAR(DATEADD(MONTH, -1, GETDATE()))",
            "Theo năm" => "YEAR(NgayLapPhieu) = YEAR(GETDATE()) - 1",
            _ => "1=0"
        };

        private int GetDaysInTimeRange(string range) => range switch
        {
            "Theo ngày" => 1,
            "Theo tuần" => 7,
            "Theo tháng" => DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month),
            "Theo năm" => 365,
            _ => 1
        };
    }
}
