using System;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using QuanLySCL.BUS;
using System.Threading.Tasks;
using System.Windows.Media;

namespace QuanLySCL.GUI.Windows
{
    public partial class ForgotPasswordWindow : Window
    {
        private readonly TaiKhoanBUS _bus = new TaiKhoanBUS();
        private string _maKHFound = "";
        private string _currentOTP = "";
        private bool _isEmailMethod = true; // Mặc định là Gmail

        public ForgotPasswordWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        // --- BƯỚC 1: GỬI OTP ---
        private async void btnSendOTP_Click(object sender, RoutedEventArgs e)
        {
            string input = txtInfo.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                ShowAlert("Vui lòng nhập thông tin!", true);
                return;
            }

            // Validate format
            if (_isEmailMethod && !Regex.IsMatch(input, @"^[\w-\.]+@gmail\.com$"))
            {
                ShowAlert("Định dạng Gmail không đúng!", true);
                return;
            }
            if (!_isEmailMethod && !Regex.IsMatch(input, @"^0[0-9]{9,10}$"))
            {
                ShowAlert("Số điện thoại không hợp lệ!", true);
                return;
            }

            // Tìm MaKH trong DB
            _maKHFound = _bus.LayMaKH(input, _isEmailMethod);

            if (string.IsNullOrEmpty(_maKHFound))
            {
                ShowAlert("Thông tin này chưa được đăng ký trong hệ thống!", true);
                return;
            }

            // Tạo OTP giả lập (6 số ngẫu nhiên)
            Random rnd = new Random();
            _currentOTP = rnd.Next(100000, 999999).ToString();

            // Gửi mail thật (REST API) hoặc giả lập SMS
            if (_isEmailMethod)
            {
                btnSendOTP.IsEnabled = false;
                txtInfo.IsEnabled = false;
                ShowAlert("Đang gửi email qua REST API... Vui lòng đợi.", false);

                var sendResult = await _bus.GuiOTPQuenMK(input, _currentOTP);
                
                if (!sendResult.success)
                {
                    ShowAlert(sendResult.errorMsg, true);
                    btnSendOTP.IsEnabled = true;
                    txtInfo.IsEnabled = true;
                    return;
                }
            }
            else
            {
                // Giả lập SMS vì chưa có API SMS
                MessageBox.Show($"[GIẢ LẬP SMS]\nGửi tới: {input}\nMã OTP của bạn là: {_currentOTP}", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            ShowAlert("Mã OTP đã được gửi thành công! Vui lòng kiểm tra email.", false);

            // Chuyển sang bước 2
            step1_Panel.Visibility = Visibility.Collapsed;
            step2_Panel.Visibility = Visibility.Visible;
        }

        // --- BƯỚC 3: XÁC MINH OTP ---
        private void btnVerifyOTP_Click(object sender, RoutedEventArgs e)
        {
            if (txtOTP.Text == _currentOTP && !string.IsNullOrEmpty(_currentOTP))
            {
                ShowAlert("Xác thực thành công!", false);
                step2_Panel.Visibility = Visibility.Collapsed;
                step3_Panel.Visibility = Visibility.Visible;
            }
            else
            {
                ShowAlert("Mã OTP không chính xác!", true);
            }
        }

        // --- BƯỚC 4: ĐỔI MẬT KHẨU ---
        private void btnUpdatePassword_Click(object sender, RoutedEventArgs e)
        {
            string newPass = txtNewPass.Password;
            if (string.IsNullOrEmpty(newPass) || newPass.Length < 6)
            {
                ShowAlert("Mật khẩu mới phải có ít nhất 6 ký tự!", true);
                return;
            }

            // Gọi BUS để đổi mật khẩu (BUS sẽ tự tạo Salt mới và mã hóa)
            bool success = _bus.DoiMatKhau(_maKHFound, newPass);

            if (success)
            {
                MessageBox.Show("Đổi mật khẩu thành công! Vui lòng đăng nhập lại.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                ShowAlert("Có lỗi xảy ra khi cập nhật mật khẩu.", true);
            }
        }

        private void ShowAlert(string msg, bool isError)
        {
            txtAlert.Text = msg;
            brdAlert.Background = isError
                ? (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#FEE2E2")
                : (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#D1FAE5");

            txtAlert.Foreground = isError
                ? (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#EF4444")
                : (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#059669");

            brdAlert.Visibility = Visibility.Visible;
        }
    }
}