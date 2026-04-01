using QuanLySCL.BUS;
using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace QuanLySCL.GUI.ViewModels
{
    public class CustomerDetailsViewModel : BaseViewModel
    {
        private const int PageSize = 10;

        private readonly CustomerBUS _customerBus = new CustomerBUS();
        private readonly BookingBUS _bookingBus = new BookingBUS();

        public string CustomerId { get; }

        private Customer _customer;
        public Customer Customer
        {
            get => _customer;
            set => SetProperty(ref _customer, value);
        }

        public ObservableCollection<Booking> Bookings { get; set; } = new ObservableCollection<Booking>();

        private ObservableCollection<Booking> _visibleBookings = new ObservableCollection<Booking>();
        public ObservableCollection<Booking> VisibleBookings
        {
            get => _visibleBookings;
            set => SetProperty(ref _visibleBookings, value);
        }

        private int _displayCount = PageSize;

        private bool _canLoadMore;
        public bool CanLoadMore
        {
            get => _canLoadMore;
            set => SetProperty(ref _canLoadMore, value);
        }

        private decimal _totalSpent;
        public decimal TotalSpent
        {
            get => _totalSpent;
            set => SetProperty(ref _totalSpent, value);
        }

        private int _totalBookings;
        public int TotalBookings
        {
            get => _totalBookings;
            set => SetProperty(ref _totalBookings, value);
        }

        public CustomerDetailsViewModel(string customerId)
        {
            CustomerId = customerId?.Trim() ?? string.Empty;
            Load();
        }

        public void Load()
        {
            Customer = _customerBus.GetCustomerById(CustomerId) ?? new Customer { Id = CustomerId };

            try
            {
                Bookings = _bookingBus.GetBookingsByCustomerId(CustomerId) ?? new ObservableCollection<Booking>();
            }
            catch
            {
                Bookings = new ObservableCollection<Booking>();
            }

            TotalBookings = Bookings.Count;
            TotalSpent = Bookings.Where(b => string.Equals(b.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                                .Sum(b => b.Amount);

            _displayCount = PageSize;
            ApplyVisible();
        }

        public void LoadMore()
        {
            _displayCount += PageSize;
            ApplyVisible();
        }

        private void ApplyVisible()
        {
            var ordered = Bookings.OrderByDescending(b => b.Date).ThenBy(b => b.Time).ToList();
            VisibleBookings = new ObservableCollection<Booking>(ordered.Take(_displayCount));
            CanLoadMore = VisibleBookings.Count < ordered.Count;
        }
    }
}

