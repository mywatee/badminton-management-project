using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuanLySCL.BUS;
using QuanLySCL.GUI.ViewModels;
using QuanLySCL.GUI.Windows;
using QuanLySCL.Models;
using System;
using System.Linq;

namespace QuanLySCL.GUI.Views
{
    public partial class DashboardView : Page
    {
        private readonly BookingBUS _bookingBus = new BookingBUS();

        public DashboardView(string role = "KhachHang", string customerId = null)
        {
            InitializeComponent();
            DataContext = new DashboardViewModel(role, customerId);
        }

        private void CourtCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe) return;
            if (fe.DataContext is not Court court) return;

            var viewModel = DataContext as DashboardViewModel;
            if (viewModel == null) return;

            // Admin/Staff can do anything. Customers can only book if status is Available.
            if (!viewModel.IsAdminOrStaff && court.Status != "Available")
            {
                MessageBox.Show("Bạn không có quyền thực hiện thao tác này.", "Lỗi quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (court.Status == "Available")
            {
                // Quick Book
                var win = new CreateBookingWindow(DateTime.Today, court.Id, null, viewModel.UserRole, viewModel.CustomerId)
                {
                    Owner = Window.GetWindow(this)
                };
                if (win.ShowDialog() == true)
                {
                    viewModel?.RefreshData();
                }
            }
            else if (court.Status == "In-use")
            {
                // Quick Check-out
                var bookingBus = new BookingBUS();
                var activeBooking = bookingBus.GetActiveBookingByCourt(court.Id);
                if (activeBooking != null)
                {
                    var win = new CheckOutWindow(activeBooking.Id)
                    {
                        Owner = Window.GetWindow(this)
                    };
                    if (win.ShowDialog() == true)
                    {
                        viewModel?.RefreshData();
                    }
                }
                else
                {
                    MessageBox.Show("Không tìm thấy thông tin đặt sân cho sân này. Vui lòng kiểm tra lại.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else if (court.Status == "Maintenance")
            {
                MessageBox.Show("Sân đang bảo trì, không thể thực hiện thao tác.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RankProgress_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
                vm.ToggleRankPercent();
        }

        private void CheckIn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not Booking booking) return;

            var viewModel = DataContext as DashboardViewModel;
            if (viewModel == null || !viewModel.IsAdminOrStaff)
            {
                MessageBox.Show("Bạn không có quyền thực hiện thao tác này.", "Lỗi quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Xác nhận nhận sân cho khách {booking.Customer}?", "Nhận sân", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var bookingBus = new BookingBUS();
                if (bookingBus.PerformCheckIn(booking.Id, out string error))
                {
                    MessageBox.Show("Khách đã nhận sân thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    (DataContext as DashboardViewModel)?.RefreshData();
                }
                else
                {
                    MessageBox.Show("Lỗi: " + error, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CheckOut_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not Booking booking) return;

            var viewModel = DataContext as DashboardViewModel;
            if (viewModel == null || !viewModel.IsAdminOrStaff)
            {
                MessageBox.Show("Bạn không có quyền thực hiện thao tác này.", "Lỗi quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var win = new CheckOutWindow(booking.Id)
            {
                Owner = Window.GetWindow(this)
            };
            if (win.ShowDialog() == true)
            {
                viewModel?.RefreshData();
            }
        }
        private void AddService_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not Court court) return;

            var viewModel = DataContext as DashboardViewModel;
            if (viewModel == null || !viewModel.IsAdminOrStaff)
            {
                MessageBox.Show("Bạn không có quyền thực hiện thao tác này.", "Lỗi quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Find the active booking for this court
            var booking = _bookingBus.GetActiveBookingByCourt(court.Id);
            if (booking == null)
            {
                MessageBox.Show("Không tìm thấy lịch đặt đang hoạt động cho sân này.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var serviceWin = new Windows.AddServiceWindow(booking.Id);
            serviceWin.Owner = Window.GetWindow(this);
            serviceWin.ShowDialog();
            
            // Refresh data after adding services
            viewModel.RefreshData();
        }

        private void ViewAllBookings_Click(object sender, RoutedEventArgs e)
        {
            var main = Application.Current?.MainWindow as MainWindow;
            if (main != null)
            {
                if (main.FindName("BookingNav") is System.Windows.Controls.RadioButton rb)
                    rb.IsChecked = true;

                var vm = DataContext as DashboardViewModel;
                main.MainFrame?.Navigate(new BookingView(BookingView.BookingViewSection.AllBookings, vm?.UserRole, vm?.CustomerId));
                return;
            }

            var viewModel = DataContext as DashboardViewModel;
            NavigationService?.Navigate(new BookingView(BookingView.BookingViewSection.AllBookings, viewModel?.UserRole, viewModel?.CustomerId));
        }

        private void ViewAllCourts_Click(object sender, RoutedEventArgs e)
        {
            var main = Application.Current?.MainWindow as MainWindow;
            if (main != null)
            {
                if (main.FindName("BookingNav") is System.Windows.Controls.RadioButton rb)
                    rb.IsChecked = true;

                var vm = DataContext as DashboardViewModel;
                main.MainFrame?.Navigate(new BookingView(BookingView.BookingViewSection.Schedule, vm?.UserRole, vm?.CustomerId));
                return;
            }

            var viewModel = DataContext as DashboardViewModel;
            NavigationService?.Navigate(new BookingView(BookingView.BookingViewSection.Schedule, viewModel?.UserRole, viewModel?.CustomerId));
        }

        private void ViewTodayBookings_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as DashboardViewModel;
            if (viewModel == null) return;

            var win = new QuanLySCL.GUI.Windows.TodayBookingsWindow(viewModel.TodayBookings)
            {
                Owner = Window.GetWindow(this)
            };
            if (win.ShowDialog() == true)
            {
                viewModel.LoadData();
            }
        }

        private void QuickSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (DataContext is DashboardViewModel vmEsc)
                    vmEsc.IsQuickSearchPopupOpen = false;
                return;
            }

            if (e.Key != Key.Enter) return;

            var vm = DataContext as DashboardViewModel;
            if (vm == null || !vm.IsAdminOrStaff) return;

            var candidates = (vm.QuickSearchMatches ?? new System.Collections.ObjectModel.ObservableCollection<Booking>())
                .Where(b => b != null && !string.IsNullOrWhiteSpace(b.Id))
                .ToList();

            if (candidates.Count == 0)
            {
                MessageBox.Show("Không tìm thấy lượt đặt phù hợp để thanh toán.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (candidates.Count == 1)
            {
                OpenCheckoutForBooking(candidates[0]);
                vm.IsQuickSearchPopupOpen = false;
                return;
            }

            // Multiple matches -> open suggestion list for user to choose.
            vm.IsQuickSearchPopupOpen = true;
            vm.SelectedQuickSearchMatch ??= candidates[0];
            QuickSearchList?.Focus();
            e.Handled = true;
        }

        private void QuickSearchList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (DataContext is DashboardViewModel vmEsc)
                    vmEsc.IsQuickSearchPopupOpen = false;
                QuickSearchBox?.Focus();
                return;
            }

            if (e.Key != Key.Enter) return;
            OpenCheckoutForSelectedQuickMatch();
            e.Handled = true;
        }

        private void QuickSearchList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenCheckoutForSelectedQuickMatch();
        }

        private void OpenCheckoutForSelectedQuickMatch()
        {
            var vm = DataContext as DashboardViewModel;
            if (vm == null || !vm.IsAdminOrStaff) return;

            var booking = vm.SelectedQuickSearchMatch;
            if (booking == null || string.IsNullOrWhiteSpace(booking.Id)) return;

            OpenCheckoutForBooking(booking);
            vm.IsQuickSearchPopupOpen = false;
            QuickSearchBox?.Focus();
        }

        private void OpenCheckoutForBooking(Booking booking)
        {
            if (booking == null || string.IsNullOrWhiteSpace(booking.Id)) return;

            var vm = DataContext as DashboardViewModel;
            if (vm == null || !vm.IsAdminOrStaff)
            {
                MessageBox.Show("Bạn không có quyền thực hiện thao tác này.", "Lỗi quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var win = new CheckOutWindow(booking.Id)
            {
                Owner = Window.GetWindow(this)
            };
            if (win.ShowDialog() == true)
            {
                vm.RefreshData();
            }
        }
    }
}
