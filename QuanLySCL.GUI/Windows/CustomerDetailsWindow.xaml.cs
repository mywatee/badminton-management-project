using QuanLySCL.GUI.ViewModels;
using System.Windows;

namespace QuanLySCL.GUI.Windows
{
    public partial class CustomerDetailsWindow : Window
    {
        private readonly CustomerDetailsViewModel _vm;

        public CustomerDetailsWindow(string customerId)
        {
            InitializeComponent();
            _vm = new CustomerDetailsViewModel(customerId);
            DataContext = _vm;
        }

        private void LoadMore_Click(object sender, RoutedEventArgs e)
        {
            _vm.LoadMore();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

