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

        public ReportSummary GetSummaryByDateRange(DateTime fromDate, DateTime toDate)
        {
            try
            {
                return _reportingDal.GetReportSummaryByDateRange(fromDate, toDate);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ReportingBUS Error: " + ex.Message);
                return new ReportSummary();
            }
        }

        public List<RevenueByMonth> GetRevenueTrendsByDateRange(DateTime fromDate, DateTime toDate)
        {
            try
            {
                return _reportingDal.GetRevenueTrendsByDateRange(fromDate, toDate);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ReportingBUS Error: " + ex.Message);
                return new List<RevenueByMonth>();
            }
        }

        public List<TopCustomerReport> GetTopCustomersWithId(int top, string timeRange)
        {
            try
            {
                return _reportingDal.GetTopCustomersWithId(top, timeRange);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ReportingBUS Error: " + ex.Message);
                return new List<TopCustomerReport>();
            }
        }

        public List<TopCustomerReport> GetTopCustomersByDateRange(DateTime fromDate, DateTime toDate, int top = 20)
        {
            try
            {
                return _reportingDal.GetTopCustomersByDateRange(fromDate, toDate, top);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ReportingBUS Error: " + ex.Message);
                return new List<TopCustomerReport>();
            }
        }

        public List<RevenueByCategory> GetCategoryRevenueByDateRange(DateTime fromDate, DateTime toDate)
        {
            try
            {
                return _reportingDal.GetRevenueByCategoryByDateRange(fromDate, toDate);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ReportingBUS Error: " + ex.Message);
                return new List<RevenueByCategory>();
            }
        }

        public List<RevenueByCourt> GetCourtRevenueByDateRange(DateTime fromDate, DateTime toDate)
        {
            try
            {
                return _reportingDal.GetRevenueByCourtByDateRange(fromDate, toDate);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ReportingBUS Error: " + ex.Message);
                return new List<RevenueByCourt>();
            }
        }

        public List<RevenueByService> GetServiceRevenueByDateRange(DateTime fromDate, DateTime toDate)
        {
            try
            {
                return _reportingDal.GetRevenueByServiceByDateRange(fromDate, toDate);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ReportingBUS Error: " + ex.Message);
                return new List<RevenueByService>();
            }
        }

        public List<RevenueByCourt> GetCourtRevenue(string timeRange)
        {
            try
            {
                return _reportingDal.GetRevenueByCourt(timeRange);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ReportingBUS Error: " + ex.Message);
                return new List<RevenueByCourt>();
            }
        }

        public List<RevenueByService> GetServiceRevenue(string timeRange)
        {
            try
            {
                return _reportingDal.GetRevenueByService(timeRange);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ReportingBUS Error: " + ex.Message);
                return new List<RevenueByService>();
            }
        }
    }
}
