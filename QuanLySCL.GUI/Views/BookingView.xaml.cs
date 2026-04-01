using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using QuanLySCL.GUI.ViewModels;
using QuanLySCL.GUI.Windows;

namespace QuanLySCL.GUI.Views
{
    public partial class BookingView : Page
    {
        public enum BookingViewSection
        {
            Schedule,
            AllBookings
        }

        private readonly BookingViewModel _viewModel;
        private BookingViewSection? _pendingScroll;

        public BookingView(string role = "Admin", string customerId = null)
        {
            InitializeComponent();
            _viewModel = new BookingViewModel(role, customerId);
            DataContext = _viewModel;
        }

        public BookingView(BookingViewSection section, string role = "Admin", string customerId = null) : this(role, customerId)
        {
            _pendingScroll = section;
            Loaded += BookingView_Loaded;
        }

        private void BookingView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_pendingScroll == null) return;

            var section = _pendingScroll.Value;
            _pendingScroll = null;
            Loaded -= BookingView_Loaded;

            Dispatcher.BeginInvoke(() =>
            {
                if (section == BookingViewSection.AllBookings)
                    AllBookingsSection?.BringIntoView();
                else
                    ScheduleSection?.BringIntoView();
            }, DispatcherPriority.Loaded);
        }

        private void PreviousWeek_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.MoveWeek(-1);
        }

        private void NextWeek_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.MoveWeek(1);
        }

        private void Today_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.GoToToday();
        }

        private void WeekDay_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is System.DateTime date)
            {
                _viewModel.SelectedDate = date.Date;
            }
        }

        private void CreateBooking_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new CreateBookingWindow(_viewModel.SelectedDate ?? System.DateTime.Today, null, null, _viewModel.UserRole, _viewModel.CustomerId)
            {
                Owner = Window.GetWindow(this)
            };

            bool? result = wnd.ShowDialog();
            if (result == true)
            {
                if (wnd.CreatedUsageDate.HasValue)
                    _viewModel.SelectedDate = wnd.CreatedUsageDate.Value.Date;
                _viewModel.ReloadFromDatabase();
            }
        }

        private void ScheduleCell_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is ScheduleCellViewModel cell)
            {
                if (cell.StatusKey == "Available")
                {
                    var wnd = new CreateBookingWindow(_viewModel.SelectedDate ?? System.DateTime.Today, cell.CourtId, cell.SlotId, _viewModel.UserRole, _viewModel.CustomerId)
                    {
                        Owner = Window.GetWindow(this)
                    };

                    bool? result = wnd.ShowDialog();
                    if (result == true)
                    {
                        if (wnd.CreatedUsageDate.HasValue)
                            _viewModel.SelectedDate = wnd.CreatedUsageDate.Value.Date;
                        _viewModel.ReloadFromDatabase();
                    }
                }
                else if (cell.StatusKey == "Booked" && cell.BookingStatus == "Pending")
                {
                    var result = MessageBox.Show(
                        $"Bạn có chắc chắn muốn hủy lịch sân {cell.CourtName} vào lúc {cell.SlotName} (Mã: {cell.BookingId}) không?", 
                        "Xác nhận hủy", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (!_viewModel.CancelBooking(cell.BookingId, out string error))
                        {
                            MessageBox.Show("Lỗi khi hủy sân: " + error, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show("Hủy lịch đặt sân thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
        }

        private void LoadMoreBookings_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadMoreBookings();
        }

        private void CancelBooking_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is QuanLySCL.Models.Booking booking)
            {
                if (booking.Status != "Pending")
                {
                    MessageBox.Show("Chỉ có thể hủy những lịch đặt đang ở trạng thái 'Pending'.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Bạn có chắc chắn muốn hủy lịch sân {booking.Court} vào lúc {booking.Time} (Mã: {booking.Id}) không?", "Xác nhận hủy", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    if (!_viewModel.CancelBooking(booking.Id, out string error))
                    {
                        MessageBox.Show("Lỗi khi hủy sân: " + error, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show("Hủy lịch đặt sân thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }
    }
}
