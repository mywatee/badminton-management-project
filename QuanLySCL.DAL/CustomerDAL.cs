using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;
using System.Data;

namespace QuanLySCL.DAL
{
    public class CustomerDAL : BaseDAL
    {
        public ObservableCollection<Customer> GetAllCustomers()
        {
            ObservableCollection<Customer> list = new ObservableCollection<Customer>();
            // Performance: avoid correlated subqueries per customer.
            // IMPORTANT: Customer screen expects "lượt đặt sân" from court bookings (CT_DAT_SAN),
            // not POS service-only sales (which are DAT_SAN rows without CT_DAT_SAN).
            string query = @"
                SELECT
                    KH.MaKH AS Id,
                    KH.HoTen AS Name,
                    KH.SDT AS Phone,
                    KH.Email,
                    COUNT(DISTINCT CASE WHEN CT.MaPhieuDat IS NOT NULL AND DS.TrangThai <> N'Hủy' THEN DS.MaPhieuDat END) AS TotalBookings,
                    ISNULL(SUM(CASE WHEN CT.MaPhieuDat IS NOT NULL AND DS.TrangThai <> N'Hủy' THEN ISNULL(HD.TongThanhToan, 0) ELSE 0 END), 0) AS TotalSpent,
                    MIN(CASE WHEN CT.MaPhieuDat IS NOT NULL THEN DS.NgayLapPhieu END) AS MemberSince
                FROM KHACH_HANG KH
                LEFT JOIN DAT_SAN DS ON DS.MaKH = KH.MaKH
                LEFT JOIN CT_DAT_SAN CT ON CT.MaPhieuDat = DS.MaPhieuDat
                LEFT JOIN HOA_DON HD ON HD.MaPhieuDat = DS.MaPhieuDat
                GROUP BY KH.MaKH, KH.HoTen, KH.SDT, KH.Email
                ORDER BY KH.MaKH";

            DataTable dt = ExecuteQuery(query);

            foreach (DataRow row in dt.Rows)
            {
                // Logic xếp hạng VIP: Bạc (30 lượt), Vàng (50 lượt/5tr), Kim cương (100 lượt/10tr)
                int totalBookings = row["TotalBookings"] != DBNull.Value ? (int)row["TotalBookings"] : 0;
                decimal totalSpent = row["TotalSpent"] != DBNull.Value ? (decimal)row["TotalSpent"] : 0;

                string status = "New";
                if (totalBookings >= 100 || totalSpent >= 10000000) status = "VIP";
                else if (totalBookings >= 50 || totalSpent >= 5000000) status = "Gold";
                else if (totalBookings >= 30) status = "Silver";
                else if (totalBookings > 5) status = "Regular";

                list.Add(new Customer
                {
                    Id = row["Id"].ToString(),
                    Name = row["Name"].ToString(),
                    Phone = row["Phone"].ToString(),
                    Email = row["Email"].ToString(),
                    TotalBookings = totalBookings,
                    TotalSpent = totalSpent,
                    MemberSince = row["MemberSince"] != DBNull.Value ? (DateTime)row["MemberSince"] : DateTime.Now,
                    Status = status
                });
            }
            return list;
        }

        public Customer GetCustomerByPhone(string phone)
        {
            string query = @"
                SELECT TOP 1 MaKH, HoTen, SDT, Email
                FROM KHACH_HANG
                WHERE SDT = @p";

            DataTable dt = ExecuteQuery(query, new object[] { phone });
            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new Customer
            {
                Id = row["MaKH"].ToString(),
                Name = row["HoTen"].ToString(),
                Phone = row["SDT"].ToString(),
                Email = row["Email"].ToString()
            };
        }

        public Customer GetCustomerById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            string query = @"
                SELECT
                    KH.MaKH AS Id,
                    KH.HoTen AS Name,
                    KH.SDT AS Phone,
                    KH.Email,
                    COUNT(DISTINCT CASE WHEN CT.MaPhieuDat IS NOT NULL AND DS.TrangThai <> N'Hủy' THEN DS.MaPhieuDat END) AS TotalBookings,
                    ISNULL(SUM(CASE WHEN CT.MaPhieuDat IS NOT NULL AND DS.TrangThai <> N'Hủy' THEN ISNULL(HD.TongThanhToan, 0) ELSE 0 END), 0) AS TotalSpent,
                    MIN(CASE WHEN CT.MaPhieuDat IS NOT NULL THEN DS.NgayLapPhieu END) AS MemberSince
                FROM KHACH_HANG KH
                LEFT JOIN DAT_SAN DS ON DS.MaKH = KH.MaKH
                LEFT JOIN CT_DAT_SAN CT ON CT.MaPhieuDat = DS.MaPhieuDat
                LEFT JOIN HOA_DON HD ON HD.MaPhieuDat = DS.MaPhieuDat
                WHERE KH.MaKH = @p
                GROUP BY KH.MaKH, KH.HoTen, KH.SDT, KH.Email";

            DataTable dt = ExecuteQuery(query, new object[] { id.Trim() });
            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            int totalBookings = row["TotalBookings"] != DBNull.Value ? (int)row["TotalBookings"] : 0;
            decimal totalSpent = row["TotalSpent"] != DBNull.Value ? (decimal)row["TotalSpent"] : 0;

            string status = "New";
            if (totalBookings >= 100 || totalSpent >= 10000000) status = "VIP";
            else if (totalBookings >= 50 || totalSpent >= 5000000) status = "Gold";
            else if (totalBookings >= 30) status = "Silver";
            else if (totalBookings > 5) status = "Regular";

            return new Customer
            {
                Id = row["Id"].ToString(),
                Name = row["Name"].ToString(),
                Phone = row["Phone"].ToString(),
                Email = row["Email"].ToString(),
                TotalBookings = totalBookings,
                TotalSpent = totalSpent,
                MemberSince = row["MemberSince"] != DBNull.Value ? (DateTime)row["MemberSince"] : DateTime.Now,
                Status = status
            };
        }

        public string InsertCustomer(string name, string phone, string email)
        {
            string newId = CreateNewCustomerId();

            string query = @"
                INSERT INTO KHACH_HANG (MaKH, HoTen, SDT, Email, DiemTichLuy, NgayDangKy)
                VALUES (@id, @name, @phone, @email, 0, GETDATE())";

            int res = ExecuteNonQuery(query, new object[] { newId, name, phone, email });
            return res > 0 ? newId : null;
        }

        private string CreateNewCustomerId()
        {
            string sql = "SELECT ISNULL(MAX(CAST(SUBSTRING(MaKH, 3, LEN(MaKH)-2) AS INT)), 0) + 1 FROM KHACH_HANG WHERE MaKH LIKE 'KH%'";
            DataTable dt = ExecuteQuery(sql);
            int nextNum = (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value) ? Convert.ToInt32(dt.Rows[0][0]) : 1;
            return "KH" + nextNum.ToString("D3");
        }
    }
}
