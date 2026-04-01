using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using QuanLySCL.BUS;
using QuanLySCL.DAL;

namespace QuanLySCL.GUI.Windows
{
    public partial class RegisterWindow : Window
    {
        private readonly TaiKhoanBUS _bus = new TaiKhoanBUS();
        private string _currentOTP = "";

        public string LastRegisteredUser { get; private set; } // Để truyền lại cho LoginWindow

        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                this.DragMove();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnBackToForm_Click(object sender, RoutedEventArgs e)
        {
            stkOTP.Visibility = Visibility.Collapsed;
            stkForm.Visibility = Visibility.Visible;
            brdAlert.Visibility = Visibility.Collapsed;
        }

        private async void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            // 1. Lấy dữ liệu
            string fullName = txtFullName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string email = txtEmail.Text.Trim();
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Password;
            string confirmPass = txtConfirmPass.Password;

            // 2. Validate cơ bản
            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(phone) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                ShowAlert("Vui lòng nhập đầy đủ thông tin!", true);
                return;
            }

            if (!Regex.IsMatch(user, @"^\S{5,}$"))
            {
                ShowAlert("Tên đăng nhập phải ít nhất 5 ký tự, không khoảng trắng!", true);
                return;
            }

            if (!Regex.IsMatch(phone, @"^0[0-9]{9,10}$"))
            {
                ShowAlert("Số điện thoại phải bắt đầu bằng 0 và có 10-11 số!", true);
                return;
            }

            if (!Regex.IsMatch(email, @"^[\w-\.]+@gmail\.com$"))
            {
                ShowAlert("Vui lòng nhập đúng địa chỉ Gmail!", true);
                return;
            }

            if (pass.Length < 6)
            {
                ShowAlert("Mật khẩu phải có ít nhất 6 ký tự!", true);
                return;
            }

            if (pass != confirmPass)
            {
                ShowAlert("Mật khẩu xác nhận không khớp!", true);
                return;
            }

            // 3. Kiểm tra trung (không đăng ký ngay)
            var check = _bus.KiemTraHopLeDangKy(user, phone, email);
            if (!check.ok)
            {
                ShowAlert(check.msg, true);
                return;
            }

            // 4. Tạo và gửi OTP qua REST API
            Random rnd = new Random();
            _currentOTP = rnd.Next(100000, 999999).ToString();

            btnRegister.IsEnabled = false;
            ShowAlert("Đang gửi mã OTP qua Brevo REST API...", false);

            var sendResult = await _bus.GuiOTPDangKy(email, _currentOTP);
            btnRegister.IsEnabled = true;

            if (sendResult.success)
            {
                ShowAlert("Mã OTP đã được gửi! Vui lòng kiểm tra email của bạn.", false);
                stkForm.Visibility = Visibility.Collapsed;
                stkOTP.Visibility = Visibility.Visible;
            }
            else
            {
                ShowAlert(sendResult.errorMsg, true);
            }
        }

        private void btnVerifyOTP_Click(object sender, RoutedEventArgs e)
        {
            if (txtOTP.Text.Trim() != _currentOTP || string.IsNullOrEmpty(_currentOTP))
            {
                ShowAlert("Mã OTP không chính xác!", true);
                return;
            }

            // OTP đúng -> Tiến hành đăng ký thật
            string fullName = txtFullName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string email = txtEmail.Text.Trim();
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Password;

            var result = _bus.DangKy(fullName, phone, email, user, pass);

            if (result.ok)
            {
                string maKH = result.msg;
                var successDlg = new SuccessDialog(maKH);
                if (successDlg.ShowDialog() == true)
                {
                    LastRegisteredUser = user;
                    this.DialogResult = true;
                    this.Close();
                }
            }
            else
            {
                ShowAlert(result.msg, true);
            }
        }

        private void btnGoToLogin_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void ShowAlert(string msg, bool isError)
        {
            txtAlert.Text = msg;
            brdAlert.Background = isError
                ? (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#FEE2E2")
                : (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#D1FAE5");

            var txtColor = isError ? "#EF4444" : "#059669";
            txtAlert.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom(txtColor);

            brdAlert.Visibility = Visibility.Visible;
        }
    }
}