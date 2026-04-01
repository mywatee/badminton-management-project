using QuanLySCL.BUS;
using QuanLySCL.Models;
using System;
using System.Linq;
using System.Windows;

namespace QuanLySCL.GUI.ViewModels
{
    public class CheckOutViewModel : BaseViewModel
    {
        private readonly BookingBUS _bookingBus = new BookingBUS();
        private readonly ServiceBUS _serviceBus = new ServiceBUS();

        private Booking _booking = new Booking();
        public Booking Booking
        {
            get => _booking;
            set => SetProperty(ref _booking, value);
        }

        private decimal _serviceFee;
        public decimal ServiceFee
        {
            get => _serviceFee;
            set
            {
                if (SetProperty(ref _serviceFee, value))
                    OnPropertyChanged(nameof(TotalAmount));
            }
        }

        private decimal _discount;
        public decimal Discount
        {
            get => _discount;
            set
            {
                if (SetProperty(ref _discount, value))
                    OnPropertyChanged(nameof(TotalAmount));
            }
        }

        private string _paymentMethod = "Tiền mặt";
        public string PaymentMethod
        {
            get => _paymentMethod;
            set => SetProperty(ref _paymentMethod, value);
        }

        public decimal TotalAmount => (Booking?.Amount ?? 0) + ServiceFee - Discount;

        public CheckOutViewModel(string bookingId)
        {
            LoadBooking(bookingId);
        }

        private readonly CustomerBUS _customerBus = new CustomerBUS();

        private System.Collections.Generic.List<BookingServiceDetail> _services = new System.Collections.Generic.List<BookingServiceDetail>();

        private void LoadBooking(string bookingId)
        {
            Booking = _bookingBus.GetBookingById(bookingId);
            if (Booking != null)
            {
                _services = _serviceBus.GetServiceDetailsByBooking(bookingId).ToList();
                ServiceFee = _services.Sum(s => s.Total);

                // Auto-apply member discount
                var customer = _customerBus.GetAllCustomers().FirstOrDefault(c => c.Phone == Booking.Phone);
                if (customer != null)
                {
                    string rank = _customerBus.GetCustomerRank(customer);
                    Discount = _customerBus.GetRankDiscount(rank, Booking.Amount + ServiceFee);
                }
            }
        }

        public bool ConfirmCheckOut(out string error)
        {
            var invoice = CreateInvoice();
            bool ok = _bookingBus.PerformCheckOut(invoice, out error);
            if (ok)
            {
                // After success, prompt to print
                if (MessageBox.Show("Thanh toán thành công! Bạn có muốn in hóa đơn không?", "In hóa đơn", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    PrintInvoice(invoice);
                }
            }
            return ok;
        }

        private Invoice CreateInvoice()
        {
            return new Invoice
            {
                Id = "HD" + DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                BookingId = Booking.Id,
                CourtFee = Booking.Amount,
                ServiceFee = ServiceFee,
                Discount = Discount,
                PaymentMethod = PaymentMethod,
                IssuedAt = DateTime.Now
            };
        }

        public void PrintInvoice(Invoice? invoice = null)
        {
            if (invoice == null) invoice = CreateInvoice();
            Helpers.PrintHelper.PrintInvoice(invoice, Booking, _services);
        }
    }
}
