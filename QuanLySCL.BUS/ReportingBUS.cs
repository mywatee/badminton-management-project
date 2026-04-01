using System;
using System.Collections.Generic;
using QuanLySCL.DAL;
using QuanLySCL.Models;

namespace QuanLySCL.BUS
{
    public class ReportingBUS
    {
        private readonly ReportingDAL _reportingDal = new ReportingDAL();

        public ReportSummary GetSummary(string timeRange)
        {
            try
            {
                return _reportingDal.GetReportSummary(timeRange);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ReportingBUS Error: " + ex.Message);
                return new ReportSummary();
            }
        }

        public List<RevenueByMonth> GetMonthlyRevenue(string timeRange)
        {
            try
            {
                return _reportingDal.GetRevenueTrends(timeRange);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ReportingBUS Error: " + ex.Message);
                return new List<RevenueByMonth>();
            }
        }

        public List<TopCustomerReport> GetTopCustomers(int top, string timeRange)
        {
            try
            {
                return _reportingDal.GetTopCustomers(top, timeRange);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ReportingBUS Error: " + ex.Message);
                return new List<TopCustomerReport>();
            }
        }

        public List<RevenueByCategory> GetCategoryRevenue(string timeRange)
        {
            try
            {
                return _reportingDal.GetRevenueByCategory(timeRange);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ReportingBUS Error: " + ex.Message);
                return new List<RevenueByCategory>();
            }
        }
    }
}
