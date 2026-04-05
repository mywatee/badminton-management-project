using QuanLySCL.BUS;
using QuanLySCL.GUI.ViewModels;
using QuanLySCL.GUI.Windows;
using System.Windows;
using System.Windows.Controls;

namespace QuanLySCL.GUI.Views
{
    public partial class StaffView : Page
    {
        private StaffViewModel Vm => DataContext as StaffViewModel;

        public StaffView()
        {
            InitializeComponent();
            DataContext = new StaffViewModel();
        }

        private void AddStaff_Click(object sender, RoutedEventArgs e)
        {
            var win = new UpsertStaffWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (win.ShowDialog() == true)
                Vm?.Load();
        }

        private void EditStaff_Click(object sender, RoutedEventArgs e)
        {
            if (Vm?.SelectedStaff == null)
            {
                MessageBox.Show("Vui lòng chọn 1 nhân viên để sửa.", "Thiếu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var win = new UpsertStaffWindow(Vm.SelectedStaff.Staff)
            {
                Owner = Window.GetWindow(this)
            };

            if (win.ShowDialog() == true)
                Vm.Load();
        }

        private void DeleteStaff_Click(object sender, RoutedEventArgs e)
        {
            if (Vm?.SelectedStaff == null)
            {
                MessageBox.Show("Vui lòng chọn 1 nhân viên để xóa.", "Thiếu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var staff = Vm.SelectedStaff.Staff;
            var confirm = MessageBox.Show($"Xóa nhân viên {staff.Id} - {staff.Name}?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var bus = new StaffBUS();
            var res = bus.DeleteStaff(staff.Id);
            if (!res.ok)
            {
                MessageBox.Show(res.error ?? "Không thể xóa nhân viên.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Vm.Load();
        }

        private void CreateAccount_Click(object sender, RoutedEventArgs e)
        {
            if (Vm?.SelectedStaff == null)
            {
                MessageBox.Show("Vui lòng chọn 1 nhân viên để tạo tài khoản.", "Thiếu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (Vm.SelectedStaff.Account != null)
            {
                MessageBox.Show("Nhân viên này đã có tài khoản.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var win = new CreateStaffAccountWindow(Vm.SelectedStaff.Staff)
            {
                Owner = Window.GetWindow(this)
            };

            if (win.ShowDialog() == true)
                Vm.Load();
        }

        private void ToggleAccountStatus_Click(object sender, RoutedEventArgs e)
        {
            if (Vm?.SelectedStaff == null)
            {
                MessageBox.Show("Vui lòng chọn 1 nhân viên.", "Thiếu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var account = Vm.SelectedStaff.Account;
            if (account == null || string.IsNullOrWhiteSpace(account.Username))
            {
                MessageBox.Show("Nhân viên này chưa có tài khoản.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            bool newStatus = !account.IsActive;
            string actionText = newStatus ? "mở khóa" : "khóa";
            var confirm = MessageBox.Show($"Bạn có chắc muốn {actionText} tài khoản '{account.Username}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            var bus = new TaiKhoanBUS();
            if (!bus.SetAccountStatus(account.Username, newStatus))
            {
                MessageBox.Show("Không thể cập nhật trạng thái tài khoản.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Vm.Load();
        }

        private void ResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (Vm?.SelectedStaff == null)
            {
                MessageBox.Show("Vui lòng chọn 1 nhân viên.", "Thiếu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var account = Vm.SelectedStaff.Account;
            if (account == null || string.IsNullOrWhiteSpace(account.Username))
            {
                MessageBox.Show("Nhân viên này chưa có tài khoản.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var win = new ResetPasswordWindow(account.Username)
            {
                Owner = Window.GetWindow(this)
            };

            if (win.ShowDialog() == true)
            {
                MessageBox.Show("Đã reset mật khẩu thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ManageAccount_Click(object sender, RoutedEventArgs e)
        {
            if (Vm?.SelectedStaff == null)
            {
                MessageBox.Show("Vui lòng chọn 1 nhân viên.", "Thiếu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var account = Vm.SelectedStaff.Account;
            if (account == null)
            {
                MessageBox.Show("Nhân viên này chưa có tài khoản.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var win = new ManageStaffAccountWindow(Vm.SelectedStaff.Staff, account)
            {
                Owner = Window.GetWindow(this)
            };

            if (win.ShowDialog() == true)
                Vm.Load();
        }
    }
}
