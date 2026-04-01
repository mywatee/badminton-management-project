using QuanLySCL.BUS;
using QuanLySCL.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace QuanLySCL.GUI.Windows
{
    public partial class CreateStaffAccountWindow : Window
    {
        private readonly TaiKhoanBUS _bus = new TaiKhoanBUS();
        private readonly Staff _staff;

        public string StaffSummary { get; set; }
        public string Username { get; set; }

        public ObservableCollection<string> RoleOptions { get; set; } =
            new ObservableCollection<string> { "NhanVien", "Admin" };

        public string SelectedRole { get; set; } = "NhanVien";
        public bool AccountIsActive { get; set; } = true;

        public string ErrorText { get; set; }

        public CreateStaffAccountWindow(Staff staff)
        {
            InitializeComponent();

            _staff = staff;
            StaffSummary = staff == null ? string.Empty : $"{staff.Id} - {staff.Name}";
            Username = string.Empty;

            DataContext = this;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            ErrorText = null;

            if (_staff == null)
            {
                ErrorText = "Thiếu thông tin nhân viên.";
                Refresh();
                return;
            }

            string password = PasswordBox?.Password ?? string.Empty;
            var res = _bus.TaoTaiKhoanNhanVien(_staff.Id, Username, password, SelectedRole, AccountIsActive);
            if (!res.ok)
            {
                ErrorText = res.msg ?? "Không thể tạo tài khoản.";
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
