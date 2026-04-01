using QuanLySCL.BUS;
using QuanLySCL.GUI.ViewModels;
using QuanLySCL.GUI.Windows;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuanLySCL.GUI.Views
{
    public partial class ServicesView : Page
    {
        private ServicesViewModel Vm => DataContext as ServicesViewModel;

        public ServicesView()
        {
                InitializeComponent();
            DataContext = new ServicesViewModel();

            // Only Admin can see the management tab.
            string role = (Application.Current?.MainWindow as QuanLySCL.GUI.MainWindow)?.CurrentRole;
            bool isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
            if (!isAdmin && AdminManageTab != null)
                AdminManageTab.Visibility = Visibility.Collapsed;
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            Vm?.Load();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var win = new UpsertServiceWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (win.ShowDialog() == true)
                Vm?.Load();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (Vm?.SelectedService == null)
            {
                MessageBox.Show("Vui lòng chọn 1 dịch vụ để sửa.", "Thiếu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var win = new UpsertServiceWindow(Vm.SelectedService)
            {
                Owner = Window.GetWindow(this)
            };

            if (win.ShowDialog() == true)
                Vm.Load();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (Vm?.SelectedService == null)
            {
                MessageBox.Show("Vui lòng chọn 1 dịch vụ để xóa.", "Thiếu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var s = Vm.SelectedService;
            var confirm = MessageBox.Show(
                $"Xóa dịch vụ {s.Id} - {s.Name}?\nLưu ý: Có thể thất bại nếu dịch vụ đang được dùng trong hóa đơn/phiếu đặt.",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            var bus = new ServiceBUS();
            var res = bus.DeleteService(s.Id);
            if (!res.ok)
            {
                MessageBox.Show("Không thể xóa dịch vụ: " + (res.error ?? "Unknown"), "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Vm.Load();
        }

        private void ServiceSalesGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid grid) return;
            if (grid.SelectedItem is not QuanLySCL.Models.ServiceSaleInvoice inv) return;

            var win = new ServiceSaleDetailWindow(inv)
            {
                Owner = Window.GetWindow(this)
            };
            win.ShowDialog();
        }
    }
}

