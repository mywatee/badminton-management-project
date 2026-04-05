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
            // If QR payment method is selected, show the QR window first
            if (_viewModel.PaymentMethod == "Chuyển khoản (QR)")
            {
                var qrWin = new QuanLySCL.GUI.Views.VietQRWindow(_viewModel.TotalAmount, $"Thanh toan {_viewModel.Booking.Court} - {_viewModel.Booking.Customer}");
                qrWin.Owner = this;
                qrWin.ShowDialog();

                if (qrWin.DialogResult != true)
                {
                    // User cancelled or staff didn't confirm receiving money
                    return;
                }
            }

            if (_viewModel.ConfirmCheckOut(out string error))
            {
                MessageBox.Show("Thanh toán và trả sân thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
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
