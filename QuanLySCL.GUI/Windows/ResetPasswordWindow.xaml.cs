using QuanLySCL.BUS;
using System.Windows;

namespace QuanLySCL.GUI.Windows
{
    public partial class ResetPasswordWindow : Window
    {
        private readonly TaiKhoanBUS _bus = new TaiKhoanBUS();

        public string Username { get; set; }
        public string ErrorText { get; set; }

        public ResetPasswordWindow(string username)
        {
            InitializeComponent();
            Username = username ?? string.Empty;
            DataContext = this;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            ErrorText = null;

            string p1 = PasswordBox?.Password ?? string.Empty;
            string p2 = ConfirmPasswordBox?.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(p1) || p1.Length < 6)
            {
                ErrorText = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                Refresh();
                return;
            }

            if (p1 != p2)
            {
                ErrorText = "Mật khẩu nhập lại không khớp.";
                Refresh();
                return;
            }

            var res = _bus.ResetMatKhauTheoTenDangNhap(Username, p1);
            if (!res.ok)
            {
                ErrorText = res.msg ?? "Không thể reset mật khẩu.";
                Refresh();
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Refresh()
        {
            DataContext = null;
            DataContext = this;
        }
    }
}

