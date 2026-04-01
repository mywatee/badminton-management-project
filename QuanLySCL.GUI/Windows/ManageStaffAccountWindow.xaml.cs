using QuanLySCL.BUS;
using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace QuanLySCL.GUI.Windows
{
    public partial class ManageStaffAccountWindow : Window
    {
        private readonly TaiKhoanBUS _bus = new TaiKhoanBUS();
        private readonly Staff _staff;
        private readonly Account _account;

        public string StaffSummary { get; set; }
        public string OldUsername { get; set; }
        public string NewUsername { get; set; }

        public ObservableCollection<string> RoleOptions { get; set; } =
            new ObservableCollection<string> { "NhanVien", "Admin" };

        public string SelectedRole { get; set; } = "NhanVien";
        public bool AccountIsActive { get; set; }

        public string ErrorText { get; set; }

        public ManageStaffAccountWindow(Staff staff, Account account)
        {
            InitializeComponent();

            _staff = staff;
            _account = account;

            StaffSummary = staff == null ? string.Empty : $"{staff.Id} - {staff.Name}";

            OldUsername = account?.Username ?? string.Empty;
            NewUsername = OldUsername;
            SelectedRole = string.IsNullOrWhiteSpace(account?.Role) ? "NhanVien" : account.Role;
            AccountIsActive = account?.IsActive ?? true;

            DataContext = this;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText = null;

            if (_account == null || string.IsNullOrWhiteSpace(OldUsername))
            {
                ErrorText = "Thiếu thông tin tài khoản.";
                Refresh();
                return;
            }

            // 1) Rename username (optional)
            var rename = _bus.DoiTenDangNhap(OldUsername, NewUsername);
            if (!rename.ok)
            {
                ErrorText = rename.msg;
                Refresh();
                return;
            }

            string currentUsername = string.IsNullOrWhiteSpace(NewUsername) ? OldUsername : NewUsername.Trim();

            // 2) Update role
            var roleRes = _bus.CapNhatQuyenTheoTenDangNhap(currentUsername, SelectedRole);
            if (!roleRes.ok)
            {
                ErrorText = roleRes.msg;
                Refresh();
                return;
            }

            // 3) Update active
            if (!_bus.SetAccountStatus(currentUsername, AccountIsActive))
            {
                ErrorText = "Không thể cập nhật trạng thái kích hoạt.";
                Refresh();
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            ErrorText = null;

            if (_account == null || string.IsNullOrWhiteSpace(OldUsername))
            {
                ErrorText = "Thiếu thông tin tài khoản.";
                Refresh();
                return;
            }

            var confirm = MessageBox.Show($"Xóa tài khoản '{OldUsername}'?\nLưu ý: không xóa nhân viên.", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var del = _bus.XoaTaiKhoanTheoTenDangNhap(OldUsername);
            if (!del.ok)
            {
                ErrorText = del.msg;
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

