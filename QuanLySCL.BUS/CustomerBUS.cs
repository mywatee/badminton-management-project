using QuanLySCL.DAL;
using QuanLySCL.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System;

namespace QuanLySCL.BUS
{
    public class CustomerBUS
    {
        private CustomerDAL _customerDal = new CustomerDAL();

        public ObservableCollection<Customer> GetAllCustomers()
        {
            return _customerDal.GetAllCustomers();
        }

        /// <summary>
        /// Tìm kiếm khách hàng theo tên hoặc SĐT
        /// </summary>
        public ObservableCollection<Customer> SearchCustomers(string keyword)
        {
            var allCustomers = _customerDal.GetAllCustomers();

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
            return _customerDal.GetCustomerByPhone(phone.Trim());
        }

        public Customer? GetCustomerById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return _customerDal.GetCustomerById(id.Trim());
        }

        public (bool ok, string? customerId, string? error) CreateCustomer(string name, string phone, string? email)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
                return (false, null, "Vui lòng nhập họ tên và số điện thoại.");

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
