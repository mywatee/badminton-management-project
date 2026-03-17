using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace BadmintonManagement.DAL
{
    public class TaiKhoanDAL
    {
        string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=QLSCL;Integrated Security=True;TrustServerCertificate=True";

        public (string maKH, string vaiTro) CheckLogin(string user, byte[] hashedPass)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                // Câu lệnh SQL kiểm tra mật khẩu dạng Binary
                string sql = "SELECT MaKH, VaiTro FROM TAI_KHOAN WHERE TenDangNhap = @user AND MatKhauHash = @pass AND TrangThai = 1";

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@user", user);
                cmd.Parameters.Add("@pass", SqlDbType.VarBinary).Value = hashedPass;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return (reader["MaKH"].ToString(), reader["VaiTro"].ToString());
                    }
                }
            }
            return (null, null);
        }
        public object GetSalt(string user)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT MuoiSalt FROM TAI_KHOAN WHERE TenDangNhap = @user";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@user", user);
                return cmd.ExecuteScalar();
            }
        }

        // Hàm kiểm tra trùng lặp (Tách từ code cũ của bạn)
        public (int userCount, int phoneCount, int emailCount) CheckExist(string user, string phone, string email)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"SELECT 
                    (SELECT COUNT(*) FROM TAI_KHOAN WHERE TenDangNhap = @user) as UserCount,
                    (SELECT COUNT(*) FROM KHACH_HANG WHERE SDT = @phone) as PhoneCount,
                    (SELECT COUNT(*) FROM KHACH_HANG WHERE Email = @email) as EmailCount";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@user", user);
                cmd.Parameters.AddWithValue("@phone", phone);
                cmd.Parameters.AddWithValue("@email", email);
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read()) return ((int)r["UserCount"], (int)r["PhoneCount"], (int)r["EmailCount"]);
                }
            }
            return (0, 0, 0);
        }

        // Hàm lấy số thứ tự để tạo mã KH (KH001, KH002...)
        public int GetNextMaKHNumber()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT ISNULL(MAX(CAST(SUBSTRING(MaKH, 3, 3) AS INT)), 0) + 1 FROM KHACH_HANG";
                SqlCommand cmd = new SqlCommand(sql, conn);
                return (int)cmd.ExecuteScalar();
            }
        }

        // Hàm thực hiện Transaction lưu 2 bảng (Quan trọng nhất)
        public bool insertRegistration(string maKH, string hoTen, string sdt, string email, string user, byte[] hash, Guid salt)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    // Chèn Khách hàng
                    string sqlKH = "INSERT INTO KHACH_HANG (MaKH, HoTen, SDT, Email, DiemTichLuy) VALUES (@ma, @ten, @sdt, @email, 0)";
                    SqlCommand cmdKH = new SqlCommand(sqlKH, conn, trans);
                    cmdKH.Parameters.AddWithValue("@ma", maKH);
                    cmdKH.Parameters.AddWithValue("@ten", hoTen);
                    cmdKH.Parameters.AddWithValue("@sdt", sdt);
                    cmdKH.Parameters.AddWithValue("@email", email);
                    cmdKH.ExecuteNonQuery();

                    // Chèn Tài khoản (Dùng byte[] và Guid chuẩn file test01.sql)
                    string sqlTK = "INSERT INTO TAI_KHOAN (TenDangNhap, MatKhauHash, MuoiSalt, VaiTro, MaKH, TrangThai) VALUES (@user, @hash, @salt, N'KhachHang', @ma, 1)";
                    SqlCommand cmdTK = new SqlCommand(sqlTK, conn, trans);
                    cmdTK.Parameters.AddWithValue("@user", user);
                    cmdTK.Parameters.Add("@hash", SqlDbType.VarBinary).Value = hash;
                    cmdTK.Parameters.AddWithValue("@salt", salt);
                    cmdTK.Parameters.AddWithValue("@ma", maKH);
                    cmdTK.ExecuteNonQuery();

                    trans.Commit();
                    return true;
                }
                catch
                {
                    trans.Rollback();
                    return false;
                }
            }
        }
    }
}