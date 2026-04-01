using QuanLySCL.GUI.ViewModels;
using System;
using System.Windows;

namespace QuanLySCL.GUI.Windows
{
    public partial class CreateBookingWindow : Window
    {
        private readonly CreateBookingViewModel _viewModel;
        public DateTime? CreatedUsageDate { get; private set; }

        public CreateBookingWindow(DateTime? defaultDate = null, string defaultCourtId = null, string defaultSlotId = null, string role = "Admin", string customerId = null)
        {
            InitializeComponent();
            _viewModel = new CreateBookingViewModel(defaultDate, defaultCourtId, defaultSlotId, role, customerId);
            DataContext = _viewModel;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var res = _viewModel.CreateBooking();
            if (!res.ok)
            {
                _viewModel.ErrorText = res.error;
                return;
            }

            CreatedUsageDate = _viewModel.UsageDate.Date;
            DialogResult = true;
            Close();
        }
    }
}
