using QuanLySCL.DAL;
using QuanLySCL.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace QuanLySCL.BUS
{
    public class ServiceBUS
    {
        private readonly ServiceDAL _dal = new ServiceDAL();
        private const string CannotDeleteServiceWithHistoryMessage =
            "Dịch vụ này đã có lịch sử giao dịch. Không thể xóa, vui lòng ngừng bán hoặc cập nhật thông tin dịch vụ!";

        public ObservableCollection<Service> GetAllServices()
        {
            EnsureShuttlecockProducts();

            // Yêu cầu: bỏ phần "thuê giày" khỏi danh sách dịch vụ.
            var all = _dal.GetAllServices() ?? new ObservableCollection<Service>();
            var filtered = all
                .Where(s =>
                    s != null &&
                    !(string.Equals(s.Category, "Equipment", StringComparison.OrdinalIgnoreCase) &&
                      !string.IsNullOrWhiteSpace(s.Name) &&
                      s.Name.Contains("giày", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return new ObservableCollection<Service>(filtered);
        }

        public ObservableCollection<ServiceSaleInvoice> GetServiceSaleInvoices(DateTime? fromDate, DateTime? toDate, string customerId, int limit = 20, int offset = 0)
            => _dal.GetServiceSaleInvoices(fromDate, toDate, customerId, limit, offset);

        public ObservableCollection<BookingServiceDetail> GetServiceDetailsByBooking(string bookingId)
            => _dal.GetServiceDetailsByBooking(bookingId);

        public bool AddServiceToBooking(BookingServiceDetail detail, out string? error)
        {
            error = null;
            try
            {
                return _dal.AddServiceToBooking(detail) > 0;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public (bool ok, string? id, string? error) CreateServiceAutoId(string category, string name, string? unit, decimal price)
            => CreateServiceAutoId(category, name, unit, price, stock: 0);

        public (bool ok, string? id, string? error) CreateServiceAutoId(string category, string name, string? unit, decimal price, int stock)
        {
            if (string.IsNullOrWhiteSpace(name))
                return (false, null, "Tên dịch vụ không được để trống.");
            if (price < 0)
                return (false, null, "Giá không hợp lệ.");
            if (stock < 0)
                return (false, null, "Tồn kho không hợp lệ.");

            string id = _dal.GetNextServiceId(category);
            try
            {
                int rows = _dal.CreateService(id, category, name.Trim(), unit?.Trim(), price, stock);
                return rows > 0 ? (true, id, null) : (false, null, "Không thể thêm dịch vụ.");
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        public (bool ok, string? error) UpdateService(string id, string category, string name, string? unit, decimal price, int stock)
        {
            if (string.IsNullOrWhiteSpace(id))
                return (false, "Mã dịch vụ không hợp lệ.");
            if (string.IsNullOrWhiteSpace(name))
                return (false, "Tên dịch vụ không được để trống.");
            if (price < 0)
                return (false, "Giá không hợp lệ.");
            if (stock < 0)
                return (false, "Tồn kho không hợp lệ.");

            try
            {
                int rows = _dal.UpdateService(id.Trim(), category, name.Trim(), unit?.Trim(), price, stock);
                return rows > 0 ? (true, null) : (false, "Không thể cập nhật dịch vụ.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public (bool ok, string? error) DeleteService(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return (false, "Mã dịch vụ không hợp lệ.");

            try
            {
                int rows = _dal.DeleteService(id.Trim());
                return rows > 0 ? (true, null) : (false, "Không thể xóa dịch vụ.");
            }
            catch (SqlException ex) when (ex.Number == 547) // Foreign key conflict
            {
                return (false, CannotDeleteServiceWithHistoryMessage);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(ex.Message) &&
                    ex.Message.IndexOf("REFERENCE constraint", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return (false, CannotDeleteServiceWithHistoryMessage);
                }

                return (false, ex.Message);
            }
        }

        public bool UpdateStock(string id, int change) => _dal.UpdateStock(id, change) > 0;

        public (bool ok, string? promotionId, decimal discount, string? error) TryApplyPromotion(string promoCode, decimal subtotal)
        {
            bool ok = _dal.TryApplyPromotion(promoCode, subtotal, out string promotionId, out decimal discount, out string error);
            return (ok, promotionId, discount, error);
        }

        public (bool ok, string? bookingId, string? invoiceId, string? error) CheckoutPosSale(string? customerId, ObservableCollection<CartItem> items, string promoCode, string paymentMethod)
        {
            if (items == null || items.Count == 0)
                return (false, null, null, "Giỏ hàng trống.");

            if (items.Any(i => i == null || string.IsNullOrWhiteSpace(i.ServiceId)))
                return (false, null, null, "Có sản phẩm không hợp lệ trong giỏ hàng.");

            if (items.Any(i => i == null || i.Quantity <= 0))
                return (false, null, null, "Số lượng sản phẩm không hợp lệ.");

            bool ok = _dal.CheckoutPosSale(customerId, items, promoCode, paymentMethod, out string bookingId, out string invoiceId, out string error);
            return (ok, bookingId, invoiceId, error);
        }

        private void EnsureShuttlecockProducts()
        {
            try
            {
                var all = _dal.GetAllServices() ?? new ObservableCollection<Service>();

                const string category = "Equipment";
                const string unit = "Ống";

                var desired = new List<(string name, decimal price)>
                {
                    ("Ống cầu Hải Yến S70", 300000m),
                    ("Ống cầu lông Yonex AS40", 1650000m),
                    ("Ống cầu lông Thành Công", 335000m),
                };

                bool ExistsByName(string n) => all.Any(s =>
                    s != null &&
                    !string.IsNullOrWhiteSpace(s.Name) &&
                    string.Equals(s.Name.Trim(), n, StringComparison.OrdinalIgnoreCase));

                Service? legacy = all.FirstOrDefault(s =>
                    s != null &&
                    string.Equals(s.Category, category, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(s.Name) &&
                    string.Equals(s.Name.Trim(), "Ống cầu", StringComparison.OrdinalIgnoreCase));

                // Reuse the legacy "Ống cầu" service as the first desired product to avoid breaking references by ID.
                if (legacy != null)
                {
                    if (!ExistsByName(desired[0].name))
                    {
                        int legacyStock = Math.Max(0, legacy.Stock);
                        _dal.UpdateService(legacy.Id, category, desired[0].name, unit, desired[0].price, legacyStock);
                    }
                }
                else if (!ExistsByName(desired[0].name))
                {
                    string id = _dal.GetNextServiceId(category);
                    _dal.CreateService(id, category, desired[0].name, unit, desired[0].price, stock: 300);
                }

                foreach (var item in desired.Skip(1))
                {
                    if (ExistsByName(item.name)) continue;
                    string id = _dal.GetNextServiceId(category);
                    _dal.CreateService(id, category, item.name, unit, item.price, stock: 300);
                }
            }
            catch
            {
                // Best-effort data upgrade; ignore failures and let UI load existing data.
            }
        }
    }
}


