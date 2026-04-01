using QuanLySCL.BUS;
using QuanLySCL.GUI.ViewModels;
using QuanLySCL.GUI.Windows;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuanLySCL.GUI.Views
{
    public partial class AdminPanelView : Page
    {
        private readonly AdminPanelViewModel _viewModel;

        public AdminPanelView()
        {
            InitializeComponent();
            _viewModel = new AdminPanelViewModel();
            DataContext = _viewModel;
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Load();
        }

        private void ToggleAccountStatus_Click(object sender, RoutedEventArgs e)
        {
            var selected = _viewModel.SelectedAccount?.Account;
            if (selected == null || string.IsNullOrWhiteSpace(selected.Username))
            {
                MessageBox.Show("Vui lòng chọn 1 tài khoản.", "Thiếu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            bool newStatus = !selected.IsActive;
            string actionText = newStatus ? "mở khóa" : "khóa";
            var confirm = MessageBox.Show(
                $"Bạn có chắc muốn {actionText} tài khoản '{selected.Username}'?",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            var bus = new TaiKhoanBUS();
            if (!bus.SetAccountStatus(selected.Username, newStatus))
            {
                MessageBox.Show("Không thể cập nhật trạng thái tài khoản.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _viewModel.Load();
        }

        private void ResetPassword_Click(object sender, RoutedEventArgs e)
        {
            var selected = _viewModel.SelectedAccount?.Account;
            if (selected == null || string.IsNullOrWhiteSpace(selected.Username))
            {
                MessageBox.Show("Vui lòng chọn 1 tài khoản.", "Thiếu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var win = new ResetPasswordWindow(selected.Username)
            {
                Owner = Window.GetWindow(this)
            };

            if (win.ShowDialog() == true)
                MessageBox.Show("Đã reset mật khẩu thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditAccount_Click(object sender, RoutedEventArgs e)
        {
            OpenEditForSelected();
        }

        private void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            var selected = _viewModel.SelectedAccount?.Account;
            if (selected == null || string.IsNullOrWhiteSpace(selected.Username))
            {
                MessageBox.Show("Vui lòng chọn 1 tài khoản.", "Thiếu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string ownerText =
                !string.IsNullOrWhiteSpace(selected.StaffId) ? $"nhân viên {selected.StaffId}" :
                (!string.IsNullOrWhiteSpace(selected.CustomerId) ? $"khách hàng {selected.CustomerId}" : "người dùng");

            var confirm = MessageBox.Show(
                $"Bạn có chắc muốn xóa tài khoản '{selected.Username}' ({ownerText})?\n\nLưu ý: thao tác này không thể hoàn tác.",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var bus = new TaiKhoanBUS();
            var res = bus.XoaTaiKhoanTheoTenDangNhap(selected.Username);
            if (!res.ok)
            {
                MessageBox.Show(string.IsNullOrWhiteSpace(res.msg) ? "Không thể xóa tài khoản." : res.msg, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show("Đã xóa tài khoản.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            _viewModel.Load();
        }

        private void AccountsGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenEditForSelected();
        }

        private void AccountsGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                OpenEditForSelected();
            }
        }

        private void OpenEditForSelected()
        {
            var selected = _viewModel.SelectedAccount?.Account;
            if (selected == null || string.IsNullOrWhiteSpace(selected.Username))
            {
                MessageBox.Show("Vui lòng chọn 1 tài khoản.", "Thiếu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Only allow editing staff accounts here to avoid duplicating customer management.
            if (string.IsNullOrWhiteSpace(selected.StaffId))
            {
                MessageBox.Show(
                    "Chỉ hỗ trợ sửa tài khoản nhân viên ở màn này. Tài khoản khách hàng chỉ cho phép Khóa/Mở và Reset mật khẩu.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var staffBus = new StaffBUS();
            var staff = staffBus.GetStaffById(selected.StaffId) ?? new QuanLySCL.Models.Staff { Id = selected.StaffId, Name = string.Empty };

            var win = new ManageStaffAccountWindow(staff, selected)
            {
                Owner = Window.GetWindow(this)
            };

            if (win.ShowDialog() == true)
                _viewModel.Load();
        }
    }
}
