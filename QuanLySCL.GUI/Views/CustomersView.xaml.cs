using System.Windows;
using System.Windows.Controls;
using QuanLySCL.GUI.ViewModels;
using QuanLySCL.GUI.Windows;

namespace QuanLySCL.GUI.Views
{
    public partial class CustomersView : Page
    {
        public CustomersView()
        {
            InitializeComponent();
            DataContext = new CustomersViewModel();
        }

        private CustomersViewModel Vm => DataContext as CustomersViewModel;

        private void ViewCustomerDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe) return;
            string customerId = fe.Tag?.ToString();
            if (string.IsNullOrWhiteSpace(customerId))
            {
                MessageBox.Show("Không xác định được khách hàng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var win = new CustomerDetailsWindow(customerId.Trim())
            {
                Owner = Window.GetWindow(this)
            };
            win.ShowDialog();
        }

        private void LoadMore_Click(object sender, RoutedEventArgs e)
        {
            Vm?.LoadMore();
        }
    }
}
