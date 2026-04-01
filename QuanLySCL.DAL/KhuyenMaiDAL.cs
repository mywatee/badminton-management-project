using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace QuanLySCL.DAL
{
    public class KhuyenMaiDAL : BaseDAL
    {
        public DataTable GetAllActivePromotions()
        {
            string query = "SELECT * FROM KHUYEN_MAI WHERE TrangThai = 1 AND (NgayBD IS NULL OR NgayBD <= GETDATE()) AND (NgayKT IS NULL OR NgayKT >= GETDATE())";
            return ExecuteQuery(query);
        }

        public DataTable GetAllPromotions()
        {
            return ExecuteQuery("SELECT * FROM KHUYEN_MAI ORDER BY TrangThai DESC, MaKM ASC");
        }

        public DataRow GetPromotionByCode(string code)
        {
            string query = "SELECT TOP 1 * FROM KHUYEN_MAI WHERE UPPER(MaKM) = @code AND TrangThai = 1";
            DataTable dt = ExecuteQuery(query, new object[] { code.Trim().ToUpperInvariant() });
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public bool InsertPromotion(string maKM, string tenKM, string kieu, decimal giaTri, decimal? donToiThieu, DateTime? ngayBD, DateTime? ngayKT, bool trangThai)
        {
            string query = @"INSERT INTO KHUYEN_MAI (MaKM, TenKM, Kieu, GiaTri, DonToiThieu, NgayBD, NgayKT, TrangThai) 
                             VALUES (@MaKM, @TenKM, @Kieu, @GiaTri, @DonToiThieu, @NgayBD, @NgayKT, @TrangThai)";
            return ExecuteNonQuery(query, new object[] { maKM, tenKM, kieu, giaTri, donToiThieu ?? (object)DBNull.Value, ngayBD ?? (object)DBNull.Value, ngayKT ?? (object)DBNull.Value, trangThai }) > 0;
        }

        public bool UpdatePromotion(string maKM, string tenKM, string kieu, decimal giaTri, decimal? donToiThieu, DateTime? ngayBD, DateTime? ngayKT, bool trangThai)
        {
            string query = @"UPDATE KHUYEN_MAI SET TenKM = @TenKM, Kieu = @Kieu, GiaTri = @GiaTri, DonToiThieu = @DonToiThieu, 
                             NgayBD = @NgayBD, NgayKT = @NgayKT, TrangThai = @TrangThai WHERE MaKM = @MaKM";
            return ExecuteNonQuery(query, new object[] { tenKM, kieu, giaTri, donToiThieu ?? (object)DBNull.Value, ngayBD ?? (object)DBNull.Value, ngayKT ?? (object)DBNull.Value, trangThai, maKM }) > 0;
        }

        public bool DeletePromotion(string maKM)
        {
            string checkQuery = "SELECT COUNT(*) FROM HOA_DON WHERE MaKM = @MaKM";
            var dt = ExecuteQuery(checkQuery, new object[] { maKM });
            int count = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : 0;
            if (count > 0)
            {
                // Can't physical delete if it's already used in invoices. Set status to 0 instead.
                return ExecuteNonQuery("UPDATE KHUYEN_MAI SET TrangThai = 0 WHERE MaKM = @MaKM", new object[] { maKM }) > 0;
            }

            return ExecuteNonQuery("DELETE FROM KHUYEN_MAI WHERE MaKM = @MaKM", new object[] { maKM }) > 0;
        }
    }
}
