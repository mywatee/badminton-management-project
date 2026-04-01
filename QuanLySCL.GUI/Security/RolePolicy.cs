using System;

namespace QuanLySCL.GUI.Security
{
    public static class RolePolicy
    {
        public static bool CanNavigate(string role, string navTag)
        {
            role ??= string.Empty;
            navTag ??= string.Empty;

            // Normalize to match DB: Admin/NhanVien/KhachHang
            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return true;

            if (role.Equals("NhanVien", StringComparison.OrdinalIgnoreCase))
            {
                // Staff can access operational pages but not admin panel.
                return navTag is "Dashboard" or "Booking" or "Services" or "Customers" or "Staff" or "Reports";
            }

            if (role.Equals("KhachHang", StringComparison.OrdinalIgnoreCase))
            {
                // Customers can view dashboard and manage bookings only.
                return navTag is "Dashboard" or "Booking";
            }

            // Unknown role: safest default.
            return false;
        }
    }
}

