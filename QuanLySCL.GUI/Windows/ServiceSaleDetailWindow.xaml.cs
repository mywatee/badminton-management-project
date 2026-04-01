using QuanLySCL.BUS;
using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace QuanLySCL.GUI.Windows
{
    public partial class ServiceSaleDetailWindow : Window
    {
        public class Vm
        {
            public string HeaderLine { get; set; }
            public string CustomerLine { get; set; }
            public string PaymentLine { get; set; }
            public string IssuedAtLine { get; set; }
            public string InvoiceIdLine { get; set; }

            public ObservableCollection<BookingServiceDetail> Lines { get; set; } = new ObservableCollection<BookingServiceDetail>();

            public decimal Subtotal { get; set; }
            public decimal Discount { get; set; }
            public decimal TotalPayable => Math.Max(0, Subtotal - Discount);
        }

        public ServiceSaleDetailWindow(ServiceSaleInvoice invoice)
        {
            InitializeComponent();

            var bus = new ServiceBUS();
            var lines = bus.GetServiceDetailsByBooking(invoice?.BookingId) ?? new ObservableCollection<BookingServiceDetail>();

            var vm = new Vm
            {
                HeaderLine = $"Mã phiếu: {invoice?.BookingId}",
                CustomerLine = string.IsNullOrWhiteSpace(invoice?.CustomerName)
                    ? "Khách hàng: Khách vãng lai"
                    : $"Khách hàng: {invoice.CustomerName} ({invoice.CustomerId})",
                PaymentLine = $"Thanh toán: {invoice?.PaymentMethod ?? "Tiền mặt"}",
                IssuedAtLine = invoice != null ? $"Ngày: {invoice.IssuedAt:dd/MM/yyyy HH:mm}" : string.Empty,
                InvoiceIdLine = invoice != null ? $"Mã HD: {invoice.InvoiceId}" : string.Empty,
                Lines = lines,
                Subtotal = lines.Sum(l => l?.Total ?? 0),
                Discount = invoice?.Discount ?? 0
            };

            DataContext = vm;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}

