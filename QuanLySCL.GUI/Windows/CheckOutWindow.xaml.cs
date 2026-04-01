using QuanLySCL.GUI.ViewModels;
using System.Windows;

namespace QuanLySCL.GUI.Windows
{
    public partial class CheckOutWindow : Window
    {
        private readonly CheckOutViewModel _viewModel;

        public CheckOutWindow(string bookingId)
        {
            InitializeComponent();
            _viewModel = new CheckOutViewModel(bookingId);
            DataContext = _viewModel;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.ConfirmCheckOut(out string error))
            {
                MessageBox.Show("Thanh toán thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Lỗi khi thanh toán: " + error, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
