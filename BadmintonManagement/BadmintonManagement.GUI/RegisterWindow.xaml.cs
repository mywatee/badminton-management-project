using BadmintonManagement.BUS;
using Microsoft.Data.SqlClient;
using System;
using System.Security.Cryptography;
using System.Text;
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

        // HÀM MÃ HÓA SHA-256
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
            // Lấy dữ liệu từ UI
            string fullName = txtFullName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string email = txtEmail.Text.Trim();
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Password;
            string confirmPass = txtConfirmPassword.Password;

            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(phone) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                ShowAlert("Vui lòng nhập đầy đủ tất cả các trường!");
                return;
            }

            if (!Regex.IsMatch(user, @"^\S{5,}$"))
            {
                ShowAlert("Tên đăng nhập phải ít nhất 5 ký tự và không có khoảng trắng!");
                return;
            }

            if (!Regex.IsMatch(phone, @"^0[0-9]{9,10}$"))
            {
                ShowAlert("Số điện thoại phải bắt đầu bằng số 0 và có 10-11 chữ số!");
                return;
            }

            if (!Regex.IsMatch(email, @"^[\w-\.]+@gmail\.com$"))
            {
                ShowAlert("Vui lòng nhập đúng địa chỉ Gmail (ví dụ: user@gmail.com)!");
                return;
            }

            if (pass.Length < 6)
            {
                ShowAlert("Mật khẩu phải có ít nhất 6 ký tự!");
                return;
            }

            if (pass != confirmPass)
            {
                ShowAlert("Mật khẩu xác nhận không khớp!");
                return;
            }

            TaiKhoanBUS bus = new TaiKhoanBUS();
            string result = bus.DangKyMoi(fullName, phone, email, user, pass);

            if (result.StartsWith("SUCCESS:"))
            {
                string maVuaTao = result.Split(':')[1];
                SuccessDialog dialog = new SuccessDialog(maVuaTao);
                dialog.ShowDialog();
                btnBackToLogin_Click(null, null);
            }
            else
            {
                ShowAlert(result); // Hiện lỗi trùng lặp hoặc lỗi DB từ BUS trả về
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