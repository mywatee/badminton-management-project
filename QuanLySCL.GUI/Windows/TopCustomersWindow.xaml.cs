using QuanLySCL.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace QuanLySCL.GUI.Windows
{
    public partial class TopCustomersWindow : Window
    {
        public ObservableCollection<TopCustomerReport> Items { get; }

        public TopCustomersWindow(ObservableCollection<TopCustomerReport> items)
        {
            InitializeComponent();
            Items = items ?? new ObservableCollection<TopCustomerReport>();
            DataContext = this;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ViewCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe) return;
            string customerId = fe.Tag?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(customerId)) return;

            var win = new CustomerDetailsWindow(customerId.Trim())
            {
                Owner = this
            };
            win.ShowDialog();
        }
    }
}

