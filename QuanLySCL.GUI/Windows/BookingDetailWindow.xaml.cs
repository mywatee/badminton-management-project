using QuanLySCL.BUS;
using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace QuanLySCL.GUI.Windows
{
    public partial class BookingDetailWindow : Window
    {
        public class Vm
        {
            public Booking Booking { get; set; }
            public string InfoLine { get; set; }
            public ObservableCollection<BookingServiceDetail> Services { get; set; } = new ObservableCollection<BookingServiceDetail>();
        }

        public BookingDetailWindow(Booking booking)
        {
            InitializeComponent();
            if (booking == null) return;

            var serviceBus = new ServiceBUS();
            var services = serviceBus.GetServiceDetailsByBooking(booking.Id) ?? new ObservableCollection<BookingServiceDetail>();

            var vm = new Vm
            {
                Booking = booking,
                InfoLine = $"{booking.Court} | {booking.Date:dd/MM/yyyy} | {booking.Time}",
                Services = services
            };

            DataContext = vm;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
