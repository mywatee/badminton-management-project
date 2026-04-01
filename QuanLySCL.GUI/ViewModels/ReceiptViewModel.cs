using QuanLySCL.Models;
using System;
using System.Collections.Generic;
using System.Windows.Documents;

namespace QuanLySCL.GUI.ViewModels
{
    public class ReceiptViewModel : BaseViewModel
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string BookingId { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; } = DateTime.Now;
        public string CustomerName { get; set; } = "Khách vãng lai";
        
        public IEnumerable<CartItem> Items { get; set; } = new List<CartItem>();
        
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal TotalPayable { get; set; }
        
        public bool HasDiscount => Discount > 0;
    }
}
