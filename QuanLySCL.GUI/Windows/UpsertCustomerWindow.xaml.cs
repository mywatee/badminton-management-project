using QuanLySCL.BUS;
using System.Text.RegularExpressions;
using System.Windows;

namespace QuanLySCL.GUI.Windows
{
    public partial class UpsertCustomerWindow : Window
    {
        private readonly CustomerBUS _customerBus = new CustomerBUS();
        private readonly TaiKhoanBUS _taiKhoanBus = new TaiKhoanBUS();

        public string CreatedCustomerId { get; private set; }

        public UpsertCustomerWindow()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = (txtName.Text ?? string.Empty).Trim();
            string phone = (txtPhone.Text ?? string.Empty).Trim();
            string email = (txtEmail.Text ?? string.Empty).Trim();
            bool createAccount = chkCreateAccount.IsChecked == true;

            string username = (txtUsername?.Text ?? string.Empty).Trim();
            string password = txtPassword?.Password ?? string.Empty;
            string confirmPassword = txtConfirmPassword?.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
            {
                ShowError("Vui lòng nhập họ tên và số điện thoại.");
                return;
            }

            if (!Regex.IsMatch(phone, @"^0[0-9]{9,10}$"))
            {
                ShowError("Số điện thoại không hợp lệ (phải bắt đầu bằng 0 và có 10-11 số).");
                return;
            }

            // Email is optional unless we create a brand new account+customer in one transaction.
            if (!string.IsNullOrWhiteSpace(email) && !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ShowError("Email không hợp lệ.");
                return;
            }

            if (createAccount)
            {
                if (!Regex.IsMatch(username, @"^\S{5,}$"))
                {
                    ShowError("Tên đăng nhập phải >= 5 ký tự và không chứa khoảng trắng.");
                    return;
                }

                if (password.Length < 6)
                {
                    ShowError("Mật khẩu phải >= 6 ký tự.");
                    return;
                }

                if (!string.Equals(password, confirmPassword))
                {
                    ShowError("Xác nhận mật khẩu không khớp.");
                    return;
                }
            }

            // If customer already exists (same phone), just create the account for that customer (if requested).
            var existing = _customerBus.GetCustomerByPhone(phone);
            if (existing != null && !string.IsNullOrWhiteSpace(existing.Id))
            {
                if (createAccount)
                {
                    var acct = _taiKhoanBus.TaoTaiKhoanKhachHang(existing.Id, username, password, true);
                    if (!acct.ok)
                    {
                        ShowError(acct.msg);
                        return;
                    }
                }

                CreatedCustomerId = existing.Id;
                DialogResult = true;
                Close();
                return;
            }

            if (createAccount)
            {
                // Create customer + account in ONE transaction to avoid dangling customer when account creation fails.
                if (string.IsNullOrWhiteSpace(email))
                {
                    ShowError("Vui lòng nhập email khi tạo tài khoản đăng nhập.");
                    return;
                }

                var reg = _taiKhoanBus.DangKy(name, phone, email, username, password);
                if (!reg.ok)
                {
                    ShowError(reg.msg);
                    return;
                }

                CreatedCustomerId = reg.msg;
                DialogResult = true;
                Close();
                return;
            }

            // Create customer profile only.
            var res = _customerBus.CreateCustomer(name, phone, email);
            if (!res.ok || string.IsNullOrWhiteSpace(res.customerId))
            {
                ShowError(res.error ?? "Không thể tạo khách hàng.");
                return;
            }

            CreatedCustomerId = res.customerId;
            DialogResult = true;
            Close();
        }

        private void ShowError(string message)
        {
            txtError.Text = message ?? string.Empty;
            brdError.Visibility = Visibility.Visible;
        }
    }
}
