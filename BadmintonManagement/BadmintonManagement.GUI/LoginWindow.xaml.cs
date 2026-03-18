using BadmintonManagement.BUS;
using System;
using System.Windows;
using System.Windows.Threading;

namespace BadmintonManagement.GUI
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        // Trong LoginWindow.xaml.cs
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Password;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                ShowAlert("Vui lòng nhập đầy đủ!");
                return;
            }

            try
            {
                TaiKhoanBUS bus = new TaiKhoanBUS();
                var result = bus.KiemTraDangNhap(user, pass);

                if (!string.IsNullOrEmpty(result.vaiTro)) // Kiểm tra vai trò thay vì chỉ maKH
                {
                    if (result.vaiTro == "Admin")
                    {
                        // Mở giao diện Admin (Có thể vẫn là MainWindow nhưng truyền thêm vai trò)
                        // Hoặc tạo mới AdminDashboard nếu bạn muốn tách biệt hoàn toàn
                        MainWindow main = new MainWindow(result.maKH, result.vaiTro);
                        main.Show();
                        this.Close();
                    }
                    else if (result.vaiTro == "KhachHang")
                    {
                        MessageBox.Show("Chào khách hàng! Giao diện dành riêng đang cập nhật.", "Thông báo");
                        // Có thể mở CustomerPortal ở đây
                    }
                    else
                    {
                        ShowAlert("Tài khoản chưa được phân quyền hoặc mật khẩu sai.");
                    }
                }
                else
                {
                    ShowAlert("Tài khoản hoặc mật khẩu không chính xác!");
                }
            }
            catch (Exception ex)
            {
                ShowAlert("Lỗi: " + ex.Message);
            }
        }

        private void btnGoToRegister_Click(object sender, RoutedEventArgs e)
        {
            new RegisterWindow().Show();
            this.Close();
        }

        private void linkForgotPass_Click(object sender, RoutedEventArgs e)
        {
            new ForgotPasswordWindow().Show();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        public void ShowAlert(string message, bool isError = true)
        {
            // Giữ nguyên code hiển thị alert của bạn
            brdAlert.Background = isError ?
                (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFrom("#fab1a0") :
                (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFrom("#55efc4");
            txtAlertMessage.Text = message;
            brdAlert.Visibility = Visibility.Visible;

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, ev) => { brdAlert.Visibility = Visibility.Collapsed; timer.Stop(); };
            timer.Start();
        }
    }
}