using BadmintonManagement.DAL;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows;

namespace BadmintonManagement.GUI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            TaoTaiKhoanAdmin(); // Chạy hàm này đầu tiên
        }

        private void TaoTaiKhoanAdmin()
        {
            string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=QLSCL;Integrated Security=True;TrustServerCertificate=True";
            string username = "admin";
            string password = "admin123";
            string maNV = "NV001";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // 1. Tạo nhân viên nếu chưa có
                    string checkNvSql = "SELECT COUNT(*) FROM NHAN_VIEN WHERE MaNV = @maNV";
                    SqlCommand cmdCheckNv = new SqlCommand(checkNvSql, conn);
                    cmdCheckNv.Parameters.AddWithValue("@maNV", maNV);
                    if ((int)cmdCheckNv.ExecuteScalar() == 0)
                    {
                        string insertNvSql = "INSERT INTO NHAN_VIEN (MaNV, HoTen, SDT, ChucVu) VALUES (@ma, @ten, @sdt, @cv)";
                        SqlCommand cmdInsertNv = new SqlCommand(insertNvSql, conn);
                        cmdInsertNv.Parameters.AddWithValue("@ma", maNV);
                        cmdInsertNv.Parameters.AddWithValue("@ten", "Quan Tri Vien");
                        cmdInsertNv.Parameters.AddWithValue("@sdt", "0909999999");
                        cmdInsertNv.Parameters.AddWithValue("@cv", "Admin");
                        cmdInsertNv.ExecuteNonQuery();
                    }

                    // 2. Mã hóa mật khẩu CHUẨN bằng SecurityHelper của dự án
                    Guid salt = Guid.NewGuid();
                    byte[] hash = SecurityHelper.HashPasswordWithSalt(password, salt);

                    // 3. Xóa tài khoản admin cũ (nếu có) để làm lại từ đầu
                    string deleteSql = "DELETE FROM TAI_KHOAN WHERE TenDangNhap = @user";
                    SqlCommand cmdDelete = new SqlCommand(deleteSql, conn);
                    cmdDelete.Parameters.AddWithValue("@user", username);
                    cmdDelete.ExecuteNonQuery();

                    // 4. Insert tài khoản Admin mới tinh với Hash đúng chuẩn
                    string insertSql = @"INSERT INTO TAI_KHOAN (TenDangNhap, MatKhauHash, MuoiSalt, MaNV, MaKH, VaiTro, TrangThai) 
                                         VALUES (@user, @hash, @salt, @maNV, NULL, N'Admin', 1)";

                    SqlCommand cmdInsert = new SqlCommand(insertSql, conn);
                    cmdInsert.Parameters.AddWithValue("@user", username);
                    cmdInsert.Parameters.Add("@hash", SqlDbType.VarBinary).Value = hash; // Truyền byte[] trực tiếp
                    cmdInsert.Parameters.AddWithValue("@salt", salt);
                    cmdInsert.Parameters.AddWithValue("@maNV", maNV);
                    cmdInsert.ExecuteNonQuery();

                    MessageBox.Show("Đã tạo mới hoàn toàn tài khoản Admin!\nUser: admin\nPass: admin123", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi: " + ex.Message, "Thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}