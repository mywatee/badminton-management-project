using QuanLySCL.GUI.ViewModels;
using QuanLySCL.GUI.Windows;
using System.Windows;
using System.Windows.Controls;

namespace QuanLySCL.GUI.Views
{
    public partial class ReportsView : Page
    {
        public ReportsView()
        {
            InitializeComponent();
        }

        private void ViewTopCustomers_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ReportsViewModel vm) return;

            var win = new TopCustomersWindow(vm.TopCustomers)
            {
                Owner = Window.GetWindow(this)
            };
            win.ShowDialog();
        }
    }
}
