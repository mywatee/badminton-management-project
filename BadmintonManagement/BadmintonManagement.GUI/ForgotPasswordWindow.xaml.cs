using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using System.Windows.Controls;

namespace BadmintonManagement.GUI
{
    public partial class ForgotPasswordWindow : Window
    {
        string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=QLSCL;Integrated Security=True;TrustServerCertificate=True";
        private string loggedInMaKH = "";
        private string currentOTP;

        public ForgotPasswordWindow()
        {
            InitializeComponent();
        }

        // 1. Khi chọn phương thức nhận mã
        private void cbMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Kiểm tra an toàn
            if (stkInputRecovery == null || lblInputTitle == null) return;

            // Nếu Index >= 0 (đã chọn 1 trong 2 mục)
            if (cbMethod.SelectedIndex >= 0)
            {
                stkInputRecovery.Visibility = Visibility.Visible; // Hiện ô nhập Gmail/SĐT

                if (cbMethod.SelectedIndex == 0)
                    lblInputTitle.Text = "Nhập Gmail đã đăng ký:";
                else
                    lblInputTitle.Text = "Nhập Số điện thoại đã đăng ký:";
            }
            else
            {
                // Nếu chưa chọn (Index = -1) thì ẩn hết đi cho đẹp
                stkInputRecovery.Visibility = Visibility.Collapsed;
                stkOTP.Visibility = Visibility.Collapsed;
                stkNewPassword.Visibility = Visibility.Collapsed;
            }
        }

        // 2. Gửi mã OTP
        private void btnSendOTP_Click(object sender, RoutedEventArgs e)
        {
            string input = txtInputRecovery.Text.Trim();
            bool isGmail = cbMethod.SelectedIndex == 0;

            if (string.IsNullOrEmpty(input))
            {
                ShowAlert("Vui lòng không để trống thông tin!");
                return;
            }

            // 1. Kiểm tra định dạng Regex
            if (isGmail)
            {
                if (!Regex.IsMatch(input, @"^[\w-\.]+@gmail\.com$"))
                {
                    ShowAlert("Gmail không đúng định dạng!");
                    return;
                }
            }
            else
            {
                if (!Regex.IsMatch(input, @"^0[0-9]{9,10}$"))
                {
                    ShowAlert("Số điện thoại không hợp lệ!");
                    return;
                }
            }

            // 2. KIỂM TRA DATABASE VÀ LẤY MaKH
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    // Nếu là Gmail thì tìm theo cột Email, nếu là SĐT thì tìm theo cột SDT
                    string sql = isGmail ?
                        "SELECT MaKH FROM KHACH_HANG WHERE Email = @input" :
                        "SELECT MaKH FROM KHACH_HANG WHERE SDT = @input";

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@input", input);

                    object result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        // GÁN GIÁ TRỊ VÀO BIẾN TOÀN CỤC (Để fix lỗi CS0103)
                        loggedInMaKH = result.ToString();

                        // 3. Nếu tìm thấy khách hàng thì mới tạo và gửi OTP
                        Random rd = new Random();
                        currentOTP = rd.Next(100000, 999999).ToString();

                        if (isGmail)
                        {
                            SendEmail(input, currentOTP);
                        }
                        else
                        {
                            // Giả lập SMS cho đồ án
                            MessageBox.Show($"[GIẢ LẬP SMS] Mã OTP gửi tới {input} là: {currentOTP}");
                            ShowAlert("Mã xác nhận đã gửi qua tin nhắn!", false);
                            stkOTP.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        ShowAlert("Thông tin này không tồn tại trên hệ thống!");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    ShowAlert("Lỗi kết nối: " + ex.Message);
                    return;
                }
            }
        }

        // 3. Xác nhận OTP
        private void btnVerify_Click(object sender, RoutedEventArgs e)
        {
            if (txtOTP.Text == currentOTP && !string.IsNullOrEmpty(currentOTP))
            {
                ShowAlert("Xác thực thành công! Hãy đặt mật khẩu mới.", false);
                stkNewPassword.Visibility = Visibility.Visible; // HIỆN Ô ĐỔI MẬT KHẨU
                btnVerify.IsEnabled = false; // Khóa nút để tránh nhấn lại
            }
            else
            {
                ShowAlert("Mã OTP không chính xác!");
            }
        }

        // 4. Nhấn nút Đổi mật khẩu
        private void btnUpdatePass_Click(object sender, RoutedEventArgs e)
        {
            string newPass = txtNewPass.Password; // Giả sử bạn dùng PasswordBox

            if (newPass.Length < 6)
            {
                ShowAlert("Mật khẩu mới phải có ít nhất 6 ký tự!");
                return;
            }

            // --- BƯỚC QUAN TRỌNG: MÃ HÓA MẬT KHẨU MỚI ---
            string hashedNewPass = RegisterWindow.HashPassword(newPass);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    // Cập nhật mật khẩu đã mã hóa vào DB
                    string sql = "UPDATE TAI_KHOAN SET MatKhauHash = @pass WHERE MaKH = @ma";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@pass", hashedNewPass); // Dùng chuỗi đã băm
                    cmd.Parameters.AddWithValue("@ma", loggedInMaKH); // MaKH bạn xác định được qua OTP

                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0)
                    {
                        MessageBox.Show("Đổi mật khẩu thành công! Hãy đăng nhập lại.");
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    ShowAlert("Lỗi DB: " + ex.Message);
                }
            }
        }

        // 5. Cập nhật vào SQL
        private void UpdatePasswordInDatabase(string info, string newPassword, bool isGmail)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string column = isGmail ? "Email" : "SDT";
                    // Tìm MaKH từ bảng KHACH_HANG thông qua Email/SDT rồi update bên TAI_KHOAN
                    string sql = $@"UPDATE TAI_KHOAN SET MatKhauHash = CAST(@pass AS varbinary) 
                                   WHERE MaKH IN (SELECT MaKH FROM KHACH_HANG WHERE {column} = @info)";

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@pass", newPassword);
                    cmd.Parameters.AddWithValue("@info", info);

                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0)
                    {
                        MessageBox.Show("Đổi mật khẩu thành công! Hãy đăng nhập lại.");
                        this.Close();
                    }
                    else
                    {
                        ShowAlert("Không tìm thấy tài khoản gắn với thông tin này!");
                    }
                }
                catch (Exception ex) { ShowAlert("Lỗi DB: " + ex.Message); }
            }
        }

        // 6. Hàm gửi Mail thực tế
        private void SendEmail(string targetEmail, string otpCode)
        {
            try
            {
                var fromAddress = new MailAddress("your-email@gmail.com", "The Champions Support");
                string fromPassword = "your-app-password"; // App Password 16 ký tự của Google

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var message = new MailMessage(fromAddress, new MailAddress(targetEmail))
                {
                    Subject = "Mã OTP khôi phục mật khẩu",
                    Body = $"Mã OTP của bạn là: {otpCode}. Vui lòng không cung cấp mã này cho bất kỳ ai!"
                })
                {
                    smtp.Send(message);
                }
                ShowAlert("Mã OTP đã gửi qua Gmail!", false);
                stkOTP.Visibility = Visibility.Visible; // HIỆN Ô NHẬP OTP SAU KHI GỬI THÀNH CÔNG
            }
            catch (Exception ex) { ShowAlert("Lỗi gửi mail: " + ex.Message); }
        }

        private void ShowAlert(string message, bool isError = true)
        {
            brdAlert.Background = isError ? (SolidColorBrush)new BrushConverter().ConvertFrom("#fab1a0") : (SolidColorBrush)new BrushConverter().ConvertFrom("#55efc4");
            txtAlertMessage.Text = message;
            brdAlert.Visibility = Visibility.Visible;

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, e) => { brdAlert.Visibility = Visibility.Collapsed; timer.Stop(); };
            timer.Start();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Đóng cửa sổ khôi phục mật khẩu
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e) { this.Close(); }
    }
}