using System;

namespace QuanLySCL.Models
{
    public class Invoice
    {
        public string Id { get; set; } = string.Empty; // MaHD
        public string BookingId { get; set; } = string.Empty; // MaPhieuDat
        public decimal CourtFee { get; set; } // TongTienSan
        public decimal ServiceFee { get; set; } // TongTienDV
        public decimal Discount { get; set; } // SoTienGiam
        public decimal TotalAmount { get; set; } // TongThanhToan
        public DateTime IssuedAt { get; set; } = DateTime.Now; // NgayXuat
        public string PaymentMethod { get; set; } = "Tiền mặt"; // HinhThucThanhToan
        
        // Navigation / UI properties
        private string _customerName = string.Empty;
        public string CustomerName 
        { 
            get => string.IsNullOrWhiteSpace(_customerName) ? "Khách vãng lai" : _customerName;
            set => _customerName = value;
        }
        public string CourtName { get; set; } = string.Empty;
    }
}
