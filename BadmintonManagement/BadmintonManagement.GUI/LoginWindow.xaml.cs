using System;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using Microsoft.Data.SqlClient;

namespace BadmintonManagement.GUI
{
    public partial class LoginWindow : Window
    {
        string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=QLSCL;Integrated Security=True;TrustServerCertificate=True";

        public LoginWindow()
        {
            InitializeComponent();
        }

        // 1. Xử lý Đăng nhập
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtUsername.Text) || string.IsNullOrEmpty(txtPassword.Password))
            {
                ShowAlert("Vui lòng nhập đầy đủ thông tin!");
                return;
            }
            // Code login SQL của Huy Hoàng dán tiếp vào đây...
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