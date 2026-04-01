using System;
using System.Windows;
using System.Windows.Controls;
using QuanLySCL.BUS;
using System.Threading;
using System.Threading.Tasks;

namespace QuanLySCL.GUI.Windows
{
    public partial class LoginWindow : Window
    {
        public string Username { get; private set; }
        public string Role { get; private set; }
        public string CustomerId { get; private set; }

        private CancellationTokenSource _alertCts;

        public LoginWindow() => InitializeComponent();

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => Application.Current?.Shutdown();

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string u = txtUser.Text.Trim();
            string p = txtPassword.Password;

            if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p))
            {
                ShowAlert("Vui lòng nhập đầy đủ thông tin!");
                return;
            }

            var bus = new TaiKhoanBUS();
            var result = bus.DangNhap(u, p);

            if (string.Equals(result.vaiTro, "LOCKED", StringComparison.OrdinalIgnoreCase))
            {
                ShowAlert("Tài khoản đang bị khóa. Vui lòng liên hệ quản trị viên để được hỗ trợ.");
                txtPassword.Clear();
                return;
            }

            if (!string.IsNullOrEmpty(result.vaiTro))
            {
                Username = u;
                Role = result.vaiTro;
                CustomerId = result.maKH;
                DialogResult = true;
                Close();
                return;
            }

            ShowAlert("Sai tên đăng nhập hoặc mật khẩu!");
            txtPassword.Clear();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            var reg = new RegisterWindow();
            if (reg.ShowDialog() == true)
            {
                txtUser.Text = reg.LastRegisteredUser;
                txtPassword.Focus();
            }
        }

        private void linkForgotPass_Click(object sender, RoutedEventArgs e)
        {
            var forgotWindow = new ForgotPasswordWindow();
            forgotWindow.ShowDialog();
        }

        private void ShowAlert(string msg)
        {
            if (txtAlert != null && brdAlert != null)
            {
                txtAlert.Text = msg;
                brdAlert.Visibility = Visibility.Visible;

                _alertCts?.Cancel();
                _alertCts = new CancellationTokenSource();
                var token = _alertCts.Token;

                _ = HideAlertAfterDelayAsync(token);
            }
        }

        private async Task HideAlertAfterDelayAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested) return;

            if (!Dispatcher.HasShutdownStarted)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    if (brdAlert != null)
                        brdAlert.Visibility = Visibility.Collapsed;
                });
            }
        }
    }
}
