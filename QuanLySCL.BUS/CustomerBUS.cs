using QuanLySCL.DAL;
using QuanLySCL.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace QuanLySCL.BUS
{
    public class CustomerBUS
    {
        private CustomerDAL _customerDal = new CustomerDAL();

        private static bool IsValidPhone(string? phone)
        {
            string p = (phone ?? string.Empty).Trim();
            return Regex.IsMatch(p, @"^0[0-9]{9,10}$");
        }

        private void EnrichRank(Customer? customer)
        {
            if (customer == null) return;

            string rank = GetCustomerRank(customer);
            customer.Status = rank;

            var (progress, nextRank) = GetRankProgress(customer, rank);
            customer.RankProgress = progress;
            customer.NextRank = nextRank;
        }

        private static (int progress, string nextRank) GetRankProgress(Customer customer, string rank)
        {
            int totalBookings = Math.Max(0, customer.TotalBookings);
            decimal totalSpent = Math.Max(0, customer.TotalSpent);

            if (string.Equals(rank, "VIP", StringComparison.OrdinalIgnoreCase))
                return (100, string.Empty);

            if (string.Equals(rank, "Gold", StringComparison.OrdinalIgnoreCase))
            {
                double pBookings = totalBookings / 100.0;
                double pSpent = (double)(totalSpent / 10000000m);
                int percent = (int)Math.Round(Math.Min(1.0, Math.Max(pBookings, pSpent)) * 100);
                return (percent, "VIP");
            }

            if (string.Equals(rank, "Silver", StringComparison.OrdinalIgnoreCase))
            {
                double pBookings = totalBookings / 50.0;
                double pSpent = (double)(totalSpent / 5000000m);
                int percent = (int)Math.Round(Math.Min(1.0, Math.Max(pBookings, pSpent)) * 100);
                return (percent, "Gold");
            }

            // New -> Silver
            {
                double pBookings = totalBookings / 30.0;
                int percent = (int)Math.Round(Math.Min(1.0, Math.Max(0.0, pBookings)) * 100);
                return (percent, "Silver");
            }
        }

        public ObservableCollection<Customer> GetAllCustomers()
        {
            var list = _customerDal.GetAllCustomers();
            if (list != null)
            {
                foreach (var c in list) EnrichRank(c);
            }
            return list;
        }

        /// <summary>
        /// Tìm kiếm khách hàng theo tên hoặc SĐT
        /// </summary>
        public ObservableCollection<Customer> SearchCustomers(string keyword)
        {
            var allCustomers = GetAllCustomers();

            if (string.IsNullOrEmpty(keyword))
                return allCustomers;

            var searchResult = new ObservableCollection<Customer>();
            keyword = keyword.ToLower();

            foreach (var cus in allCustomers)
            {
                if (cus.Name.ToLower().Contains(keyword) ||
                    cus.Phone.Contains(keyword) ||
                    cus.Id.ToLower().Contains(keyword))
                {
                    searchResult.Add(cus);
                }
            }
            return searchResult;
        }

        public Customer? GetCustomerByPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            var c = _customerDal.GetCustomerByPhone(phone.Trim());
            EnrichRank(c);
            return c;
        }

        public Customer? GetCustomerById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            var c = _customerDal.GetCustomerById(id.Trim());
            EnrichRank(c);
            return c;
        }

        public (bool ok, string? customerId, string? error) CreateCustomer(string name, string phone, string? email)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
                return (false, null, "Vui lòng nhập họ tên và số điện thoại.");

            if (!IsValidPhone(phone))
                return (false, null, "Số điện thoại không hợp lệ (phải bắt đầu bằng 0 và có 10-11 số).");

            var existing = _customerDal.GetCustomerByPhone(phone.Trim());
            if (existing != null) return (true, existing.Id, null);

            string id = _customerDal.InsertCustomer(name.Trim(), phone.Trim(), (email ?? string.Empty).Trim());
            if (string.IsNullOrWhiteSpace(id))
                return (false, null, "Không thể tạo khách hàng.");

            return (true, id, null);
        }

        /// <summary>
        /// Thêm khách hàng mới
        /// </summary>
        public bool AddCustomer(string hoTen, string sdt, string? email)
        {
            // Kiểm tra SĐT trùng
            if (!IsValidPhone(sdt)) return false;

            var list = GetAllCustomers();
            if (list.Any(c => c.Phone == sdt))
                return false;

            // Gọi DAL để thêm
            string id = _customerDal.InsertCustomer(hoTen, sdt, email ?? string.Empty);
            return !string.IsNullOrEmpty(id);
        }

        public string GetCustomerRank(Customer? customer)
        {
            if (customer == null) return "New";
            
            // Tier thresholds
            if (customer.TotalBookings >= 100 || customer.TotalSpent >= 10000000) return "VIP";
            if (customer.TotalBookings >= 50 || customer.TotalSpent >= 5000000) return "Gold";
            if (customer.TotalBookings >= 30) return "Silver";
            
            return "New";
        }

        public decimal GetRankDiscount(string rank, decimal subtotal)
        {
            decimal percentage = rank switch
            {
                "VIP" => 0.15m,
                "Gold" => 0.10m,
                "Silver" => 0.05m,
                _ => 0m
            };
            return Math.Round(subtotal * percentage, 0);
        }
    }
}
