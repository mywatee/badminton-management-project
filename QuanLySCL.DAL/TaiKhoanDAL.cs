using Microsoft.Data.SqlClient;
using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;
using System.Data;

namespace QuanLySCL.DAL
{
    public class TaiKhoanDAL : BaseDAL
    {
        public (string maKH, string vaiTro) KiemTraDangNhap(string username, string password)
        {
            object saltObj = GetSaltByUsername(username);
            if (saltObj == null || saltObj == DBNull.Value) return (null, null);

            Guid salt = (Guid)saltObj;
            byte[] hashedInput = SecurityHelper.HashPassword(password, salt);

            // Read status separately to show a better message when an account is locked.
            string query = "SELECT MaKH, VaiTro, TrangThai FROM TAI_KHOAN WHERE TenDangNhap = @u AND MatKhauHash = @p";
            DataTable dt = ExecuteQuery(query, new object[] { username, hashedInput });

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                string maKH = row["MaKH"] == DBNull.Value ? null : row["MaKH"].ToString();
                string vaiTro = row["VaiTro"]?.ToString();

                bool isActive = row["TrangThai"] != DBNull.Value && Convert.ToBoolean(row["TrangThai"]);
                if (!isActive) return (maKH, "LOCKED");

                return (maKH, vaiTro);
            }

            return (null, null);
        }

        private object GetSaltByUsername(string username)
        {
            string query = "SELECT MuoiSalt FROM TAI_KHOAN WHERE TenDangNhap = @u";
            DataTable dt = ExecuteQuery(query, new object[] { username });
            return (dt.Rows.Count > 0) ? dt.Rows[0][0] : null;
        }

        public (bool success, string message) DangKyTaiKhoan(string hoTen, string sdt, string email, string username, string password)
        {
            var check = KiemTraTrung(username, sdt, email);
            if (check.Item1 > 0) return (false, "Tên đăng nhập đã tồn tại!");
            if (check.Item2 > 0) return (false, "Số điện thoại đã được đăng ký!");
            if (check.Item3 > 0) return (false, "Email đã được sử dụng!");

            string maKH = TaoMaKHMoi();

            Guid newSalt = Guid.NewGuid();
            byte[] hashedPass = SecurityHelper.HashPassword(password, newSalt);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    string sqlKH = "INSERT INTO KHACH_HANG (MaKH, HoTen, SDT, Email, DiemTichLuy) VALUES (@ma, @ten, @sdt, @email, 0)";
                    ExecuteNonQueryTrans(conn, trans, sqlKH, new object[] { maKH, hoTen, sdt, email });

                    string sqlTK = "INSERT INTO TAI_KHOAN (MaKH, TenDangNhap, MatKhauHash, MuoiSalt, VaiTro, TrangThai) VALUES (@ma, @u, @p, @s, N'KhachHang', 1)";
                    ExecuteNonQueryTrans(conn, trans, sqlTK, new object[] { maKH, username, hashedPass, newSalt });

                    trans.Commit();
                    return (true, maKH);
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    return (false, "Lỗi hệ thống: " + ex.Message);
                }
            }
        }

        public (int user, int phone, int email) KiemTraTrung(string user, string phone, string email)
        {
            string sql = @"SELECT 
                (SELECT COUNT(*) FROM TAI_KHOAN WHERE TenDangNhap = @u),
                (SELECT COUNT(*) FROM KHACH_HANG WHERE SDT = @p),
                (SELECT COUNT(*) FROM KHACH_HANG WHERE Email = @e)";

            DataTable dt = ExecuteQuery(sql, new object[] { user, phone, email });
            if (dt.Rows.Count > 0)
            {
                return (
                    Convert.ToInt32(dt.Rows[0][0]),
                    Convert.ToInt32(dt.Rows[0][1]),
                    Convert.ToInt32(dt.Rows[0][2])
                );
            }
            return (0, 0, 0);
        }

        private string TaoMaKHMoi()
        {
            string sql = "SELECT ISNULL(MAX(CAST(SUBSTRING(MaKH, 3, LEN(MaKH)-2) AS INT)), 0) + 1 FROM KHACH_HANG WHERE MaKH LIKE 'KH%'";
            DataTable dt = ExecuteQuery(sql);

            if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
            {
                int nextNum = Convert.ToInt32(dt.Rows[0][0]);
                return "KH" + nextNum.ToString("D3");
            }

            return "KH001";
        }

        public string LayMaKHTheoThongTin(string info, bool laEmail)
        {
            string col = laEmail ? "Email" : "SDT";
            string sql = $"SELECT MaKH FROM KHACH_HANG WHERE {col} = @i";
            DataTable dt = ExecuteQuery(sql, new object[] { info });
            return (dt.Rows.Count > 0) ? dt.Rows[0][0].ToString() : null;
        }

        public bool CapNhatMatKhau(string maKH, string matKhauMoi)
        {
            Guid newSalt = Guid.NewGuid();
            byte[] hashedPass = SecurityHelper.HashPassword(matKhauMoi, newSalt);

            string sql = "UPDATE TAI_KHOAN SET MatKhauHash = @p, MuoiSalt = @s WHERE MaKH = @m";
            int res = ExecuteNonQuery(sql, new object[] { hashedPass, newSalt, maKH });
            return res > 0;
        }

        public ObservableCollection<Account> GetAllAccounts()
        {
            ObservableCollection<Account> list = new ObservableCollection<Account>();
            string sql = @"
                SELECT TenDangNhap, VaiTro, TrangThai, MaKH, MaNV
                FROM TAI_KHOAN
                ORDER BY VaiTro, TenDangNhap";

            DataTable dt = ExecuteQuery(sql);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Account
                {
                    Username = row["TenDangNhap"]?.ToString(),
                    Role = row["VaiTro"]?.ToString(),
                    IsActive = row["TrangThai"] != DBNull.Value && Convert.ToBoolean(row["TrangThai"]),
                    CustomerId = row["MaKH"] == DBNull.Value ? null : row["MaKH"].ToString(),
                    StaffId = row["MaNV"] == DBNull.Value ? null : row["MaNV"].ToString()
                });
            }

            return list;
        }

        public bool SetAccountStatus(string username, bool isActive)
        {
            string sql = "UPDATE TAI_KHOAN SET TrangThai = @s WHERE TenDangNhap = @u";
            int res = ExecuteNonQuery(sql, new object[] { isActive ? 1 : 0, username });
            return res > 0;
        }

        public Account GetAccountByStaffId(string staffId)
        {
            if (string.IsNullOrWhiteSpace(staffId)) return null;

            string sql = @"
                SELECT TOP 1 TenDangNhap, VaiTro, TrangThai, MaKH, MaNV
                FROM TAI_KHOAN
                WHERE MaNV = @id";

            DataTable dt = ExecuteQuery(sql, new object[] { staffId.Trim() });
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new Account
            {
                Username = row["TenDangNhap"]?.ToString(),
                Role = row["VaiTro"]?.ToString(),
                IsActive = row["TrangThai"] != DBNull.Value && Convert.ToBoolean(row["TrangThai"]),
                CustomerId = row["MaKH"] == DBNull.Value ? null : row["MaKH"].ToString(),
                StaffId = row["MaNV"] == DBNull.Value ? null : row["MaNV"].ToString()
            };
        }

        public (bool ok, string message) TaoTaiKhoanNhanVien(string staffId, string username, string password, string role, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(staffId)) return (false, "Thiếu mã nhân viên.");
            if (string.IsNullOrWhiteSpace(username)) return (false, "Thiếu tên đăng nhập.");
            if (string.IsNullOrWhiteSpace(password)) return (false, "Thiếu mật khẩu.");

            string staff = staffId.Trim();
            string user = username.Trim();

            // Prevent duplicate username
            var dupUser = ExecuteQuery("SELECT COUNT(*) FROM TAI_KHOAN WHERE TenDangNhap = @u", new object[] { user });
            if (dupUser.Rows.Count > 0 && Convert.ToInt32(dupUser.Rows[0][0]) > 0)
                return (false, "Tên đăng nhập đã tồn tại!");

            // Prevent multiple accounts for one staff
            var dupStaff = ExecuteQuery("SELECT COUNT(*) FROM TAI_KHOAN WHERE MaNV = @id", new object[] { staff });
            if (dupStaff.Rows.Count > 0 && Convert.ToInt32(dupStaff.Rows[0][0]) > 0)
                return (false, "Nhân viên này đã có tài khoản!");

            string normalizedRole = string.IsNullOrWhiteSpace(role) ? "NhanVien" : role.Trim();
            if (!string.Equals(normalizedRole, "NhanVien", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(normalizedRole, "Admin", StringComparison.OrdinalIgnoreCase))
                normalizedRole = "NhanVien";

            Guid salt = Guid.NewGuid();
            byte[] hashedPass = SecurityHelper.HashPassword(password, salt);

            string insertSql = @"
                INSERT INTO TAI_KHOAN (TenDangNhap, MatKhauHash, MuoiSalt, MaNV, MaKH, VaiTro, TrangThai)
                VALUES (@u, @p, @s, @nv, NULL, @r, @st)";

            try
            {
                int rows = ExecuteNonQuery(insertSql, new object[] { user, hashedPass, salt, staff, normalizedRole, isActive ? 1 : 0 });
                return rows > 0 ? (true, "OK") : (false, "Không thể tạo tài khoản.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public (bool ok, string message) ResetMatKhauTheoTenDangNhap(string username, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(username)) return (false, "Thiếu tên đăng nhập.");
            if (string.IsNullOrWhiteSpace(newPassword)) return (false, "Thiếu mật khẩu mới.");

            string user = username.Trim();

            var exists = ExecuteQuery("SELECT COUNT(*) FROM TAI_KHOAN WHERE TenDangNhap = @u", new object[] { user });
            if (exists.Rows.Count == 0 || Convert.ToInt32(exists.Rows[0][0]) <= 0)
                return (false, "Không tìm thấy tài khoản.");

            Guid newSalt = Guid.NewGuid();
            byte[] hashedPass = SecurityHelper.HashPassword(newPassword, newSalt);

            string sql = "UPDATE TAI_KHOAN SET MatKhauHash = @p, MuoiSalt = @s WHERE TenDangNhap = @u";
            try
            {
                int res = ExecuteNonQuery(sql, new object[] { hashedPass, newSalt, user });
                return res > 0 ? (true, "OK") : (false, "Không thể cập nhật mật khẩu.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public (bool ok, string message) CapNhatQuyenTheoTenDangNhap(string username, string role)
        {
            if (string.IsNullOrWhiteSpace(username)) return (false, "Thiếu tên đăng nhập.");
            string user = username.Trim();

            string normalizedRole = string.IsNullOrWhiteSpace(role) ? "NhanVien" : role.Trim();
            if (!string.Equals(normalizedRole, "NhanVien", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(normalizedRole, "Admin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(normalizedRole, "KhachHang", StringComparison.OrdinalIgnoreCase))
                return (false, "Quyền không hợp lệ.");

            try
            {
                int res = ExecuteNonQuery("UPDATE TAI_KHOAN SET VaiTro = @r WHERE TenDangNhap = @u", new object[] { normalizedRole, user });
                return res > 0 ? (true, "OK") : (false, "Không thể cập nhật quyền.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public (bool ok, string message) DoiTenDangNhap(string oldUsername, string newUsername)
        {
            if (string.IsNullOrWhiteSpace(oldUsername)) return (false, "Thiếu tên đăng nhập cũ.");
            if (string.IsNullOrWhiteSpace(newUsername)) return (false, "Thiếu tên đăng nhập mới.");

            string oldU = oldUsername.Trim();
            string newU = newUsername.Trim();
            if (string.Equals(oldU, newU, StringComparison.OrdinalIgnoreCase)) return (true, "OK");

            var dup = ExecuteQuery("SELECT COUNT(*) FROM TAI_KHOAN WHERE TenDangNhap = @u", new object[] { newU });
            if (dup.Rows.Count > 0 && Convert.ToInt32(dup.Rows[0][0]) > 0)
                return (false, "Tên đăng nhập mới đã tồn tại!");

            try
            {
                int res = ExecuteNonQuery("UPDATE TAI_KHOAN SET TenDangNhap = @new WHERE TenDangNhap = @old", new object[] { newU, oldU });
                return res > 0 ? (true, "OK") : (false, "Không thể đổi tên đăng nhập.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public (bool ok, string message) XoaTaiKhoanTheoTenDangNhap(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return (false, "Thiếu tên đăng nhập.");
            string user = username.Trim();

            try
            {
                int res = ExecuteNonQuery("DELETE FROM TAI_KHOAN WHERE TenDangNhap = @u", new object[] { user });
                return res > 0 ? (true, "OK") : (false, "Không thể xóa tài khoản.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

    }
}
