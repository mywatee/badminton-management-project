using Microsoft.Data.SqlClient;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using BadmintonManagement.BUS;

namespace BadmintonManagement.GUI
{
    public partial class LoginWindow : Window
    {
        string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=QLSCL;Integrated Security=True;TrustServerCertificate=True";

        public LoginWindow()
        {
            InitializeComponent();
        }

        // HÀM MÃ HÓA SHA-256 (Phải giống hệt bên RegisterWindow)
        public static string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // 1. Xử lý Đăng nhập
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Password;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                ShowAlert("Vui lòng nhập đầy đủ tài khoản và mật khẩu!");
                return;
            }

            try
            {
                // GỌI BUS: Đây là nơi lỗi CS1061 xuất hiện nếu bước 1 chưa xong
                TaiKhoanBUS bus = new TaiKhoanBUS();
                var result = bus.KiemTraDangNhap(user, pass);

                if (result.maKH != null)
                {
                    MainWindow main = new MainWindow(result.maKH);
                    main.Show();
                    this.Close();
                }
                else
                {
                    ShowAlert("Tài khoản hoặc mật khẩu không chính xác!");
                }
            }
            catch (Exception ex)
            {
                ShowAlert("Lỗi hệ thống: " + ex.Message);
            }
        }

        // 2. Chuyển sang màn hình Đăng ký
        private void btnGoToRegister_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow reg = new RegisterWindow();
            reg.Show();
            this.Close();
        }

        // 3. Chuyển sang màn hình Quên mật khẩu
        private void linkForgotPass_Click(object sender, RoutedEventArgs e)
        {
            ForgotPasswordWindow forgot = new ForgotPasswordWindow();
            forgot.Show();
        }

        // 4. Thoát ứng dụng
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // 5. Hàm hiện thông báo "Xịn"
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
    }
}