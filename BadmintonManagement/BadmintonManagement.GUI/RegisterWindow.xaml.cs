using Microsoft.Data.SqlClient;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace BadmintonManagement.GUI
{
    public partial class RegisterWindow : Window
    {
        string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=QLSCL;Integrated Security=True;TrustServerCertificate=True";

        public RegisterWindow()
        {
            InitializeComponent();
        }

        public void ShowAlert(string message, bool isError = true)
        {
            brdAlert.Background = isError ?
                (SolidColorBrush)new BrushConverter().ConvertFrom("#fab1a0") :
                (SolidColorBrush)new BrushConverter().ConvertFrom("#55efc4");

            txtAlertMessage.Text = message;
            brdAlert.Visibility = Visibility.Visible;

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, ev) => {
                brdAlert.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }
        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            // 1. Lấy dữ liệu và loại bỏ khoảng trắng dư thừa
            string fullName = txtFullName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string email = txtEmail.Text.Trim();
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Password;
            string confirmPass = txtConfirmPassword.Password;

            // 2. KIỂM TRA ĐỂ TRỐNG (Phải đặt lên đầu tiên)
            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(phone) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                ShowAlert("Vui lòng nhập đầy đủ tất cả các trường!");
                return;
            }

            // 3. KIỂM TRA ĐỊNH DẠNG (REGEX)
            // Tên đăng nhập: ít nhất 5 ký tự, không dấu cách
            if (!Regex.IsMatch(user, @"^\S{5,}$"))
            {
                ShowAlert("Tên đăng nhập phải ít nhất 5 ký tự và không có khoảng trắng!");
                return;
            }

            // Số điện thoại: bắt đầu bằng 0, đủ 10-11 số
            if (!Regex.IsMatch(phone, @"^0[0-9]{9,10}$"))
            {
                ShowAlert("Số điện thoại phải bắt đầu bằng số 0 và có 10-11 chữ số!");
                return;
            }

            // Gmail: đúng định dạng @gmail.com
            if (!Regex.IsMatch(email, @"^[\w-\.]+@gmail\.com$"))
            {
                ShowAlert("Vui lòng nhập đúng địa chỉ Gmail (ví dụ: user@gmail.com)!");
                return;
            }

            // Độ dài mật khẩu: ít nhất 6 ký tự
            if (pass.Length < 6)
            {
                ShowAlert("Mật khẩu phải có ít nhất 6 ký tự!");
                return;
            }

            // Xác nhận mật khẩu
            if (pass != confirmPass)
            {
                ShowAlert("Mật khẩu xác nhận không khớp!");
                return;
            }

            // 4. THỰC HIỆN LƯU VÀO DATABASE
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // --- BƯỚC MỚI: KIỂM TRA TRÙNG LẶP ---
                    string checkExistSql = @"SELECT 
            (SELECT COUNT(*) FROM TAI_KHOAN WHERE TenDangNhap = @user) as UserCount,
            (SELECT COUNT(*) FROM KHACH_HANG WHERE SDT = @phone) as PhoneCount,
            (SELECT COUNT(*) FROM KHACH_HANG WHERE Email = @email) as EmailCount";

                    SqlCommand cmdCheck = new SqlCommand(checkExistSql, conn);
                    cmdCheck.Parameters.AddWithValue("@user", user);
                    cmdCheck.Parameters.AddWithValue("@phone", phone);
                    cmdCheck.Parameters.AddWithValue("@email", email);

                    using (SqlDataReader reader = cmdCheck.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if ((int)reader["UserCount"] > 0)
                            {
                                ShowAlert("Tên đăng nhập này đã có người sử dụng!");
                                return;
                            }
                            if ((int)reader["PhoneCount"] > 0)
                            {
                                ShowAlert("Số điện thoại này đã được đăng ký!");
                                return;
                            }
                            if ((int)reader["EmailCount"] > 0)
                            {
                                ShowAlert("Gmail này đã được liên kết với một tài khoản khác!");
                                return;
                            }
                        }
                    }
                    // Kết thúc kiểm tra trùng - Nếu pass qua đây mới chạy tiếp phần dưới

                    // 2. Tự động tạo mã Khách hàng mới (KHxxx)
                    string getNextMaKH = "SELECT ISNULL(MAX(CAST(SUBSTRING(MaKH, 3, 3) AS INT)), 0) + 1 FROM KHACH_HANG";
                    SqlCommand cmdGetMa = new SqlCommand(getNextMaKH, conn);
                    int nextNum = (int)cmdGetMa.ExecuteScalar();
                    string maKH = "KH" + nextNum.ToString("D3");

                    SqlTransaction trans = conn.BeginTransaction();
                    try
                    {
                        // Thêm vào bảng KHACH_HANG
                        string sqlKH = "INSERT INTO KHACH_HANG (MaKH, HoTen, SDT, Email) VALUES (@ma, @ten, @sdt, @email)";
                        SqlCommand cmdKH = new SqlCommand(sqlKH, conn, trans);
                        cmdKH.Parameters.AddWithValue("@ma", maKH);
                        cmdKH.Parameters.AddWithValue("@ten", fullName);
                        cmdKH.Parameters.AddWithValue("@sdt", phone);
                        cmdKH.Parameters.AddWithValue("@email", email);
                        cmdKH.ExecuteNonQuery();

                        // Thêm vào bảng TAI_KHOAN (Gán mật khẩu - sau này sẽ Hash)
                        string sqlTK = "INSERT INTO TAI_KHOAN (TenDangNhap, MatKhauHash, VaiTro, MaKH, TrangThai) VALUES (@user, CAST(@pass AS varbinary), N'KhachHang', @ma, 1)";
                        SqlCommand cmdTK = new SqlCommand(sqlTK, conn, trans);
                        cmdTK.Parameters.AddWithValue("@user", user);
                        cmdTK.Parameters.AddWithValue("@pass", pass);
                        cmdTK.Parameters.AddWithValue("@ma", maKH);
                        cmdTK.ExecuteNonQuery();

                        trans.Commit();

                        SuccessDialog dialog = new SuccessDialog(maKH);
                        if (dialog.ShowDialog() == true)
                        {
                            btnBackToLogin_Click(null, null);
                        }
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        ShowAlert("Lỗi lưu dữ liệu: " + ex.Message);
                    }
                }
                catch (Exception ex) { ShowAlert("Lỗi kết nối: " + ex.Message); }
            }
        }

        private void btnBackToLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}