using System;

namespace QuanLySCL.Models
{
    public class Staff
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // ChucVu
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = "Chung";
        public decimal Salary { get; set; }
        public DateTime JoinDate { get; set; } = DateTime.Today;
        public string Status { get; set; } = "Active";
        
        // Aliases for compatibility
        public string Position { get => Role; set => Role = value; }
        public DateTime HireDate { get => JoinDate; set => JoinDate = value; }
    }
}
