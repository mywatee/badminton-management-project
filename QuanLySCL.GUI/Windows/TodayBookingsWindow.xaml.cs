using System.Collections.Generic;
using System.Windows;
using QuanLySCL.Models;

namespace QuanLySCL.GUI.Windows
{
    public partial class TodayBookingsWindow : Window
    {
        public TodayBookingsWindow(IEnumerable<Booking> bookings)
        {
            InitializeComponent();
            
            // Xả dữ liệu List Lịch Đặt từ Dashboard vào nguồn dữ liệu màn hình này
            dgBookings.ItemsSource = bookings;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close(); // Đóng giao diện
        }
    }
}
