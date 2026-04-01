using QuanLySCL.DAL;
using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace QuanLySCL.BUS
{
    public class ServiceBUS
    {
        private readonly ServiceDAL _dal = new ServiceDAL();

        public ObservableCollection<Service> GetAllServices() => _dal.GetAllServices();

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
            catch (Exception ex)
            {
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
    }
}

