using System;

namespace QuanLySCL.Models
{
    public class Account
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "KhachHang"; // Admin, NhanVien, KhachHang
        public string? Email { get; set; }
        
        // Missing properties for DAL
        public bool IsActive { get; set; }
        public string? CustomerId { get; set; }
        public string? StaffId { get; set; }
    }
}
