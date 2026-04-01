using System;

namespace QuanLySCL.Models
{
    public class ServiceSaleInvoice
    {
        public string InvoiceId { get; set; }
        public string BookingId { get; set; }

        public string CustomerId { get; set; }
        private string _customerName;
        public string CustomerName 
        { 
            get => string.IsNullOrWhiteSpace(_customerName) ? "Khách vãng lai" : _customerName;
            set => _customerName = value;
        }

        public DateTime IssuedAt { get; set; }
        public string PaymentMethod { get; set; }

        public decimal ServiceSubtotal { get; set; } // TongTienDV
        public decimal Discount { get; set; } // SoTienGiam
        public decimal TotalPayable => Math.Max(0, ServiceSubtotal - Discount);
    }
}

