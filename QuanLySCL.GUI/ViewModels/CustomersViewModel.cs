using QuanLySCL.BUS;
using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace QuanLySCL.GUI.ViewModels
{
    public class CustomersViewModel : BaseViewModel
    {
        private const int PageSize = 20;

        private readonly CustomerBUS _customerBus = new CustomerBUS();
        private readonly BookingBUS _bookingBus = new BookingBUS();

        private ObservableCollection<Customer> _allCustomers = new ObservableCollection<Customer>();

        public ObservableCollection<Customer> Customers { get; set; } = new ObservableCollection<Customer>();
        public ObservableCollection<Customer> VisibleCustomers { get; set; } = new ObservableCollection<Customer>();

        private int _displayCount = PageSize;

        private bool _canLoadMore;
        public bool CanLoadMore
        {
            get => _canLoadMore;
            set => SetProperty(ref _canLoadMore, value);
        }

        private int _vipCustomers;
        public int VipCustomers
        {
            get => _vipCustomers;
            set => SetProperty(ref _vipCustomers, value);
        }

        private int _newCustomersThisMonth;
        public int NewCustomersThisMonth
        {
            get => _newCustomersThisMonth;
            set => SetProperty(ref _newCustomersThisMonth, value);
        }

        private int _activeToday;
        public int ActiveToday
        {
            get => _activeToday;
            set => SetProperty(ref _activeToday, value);
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _displayCount = PageSize;
                    ApplyFilter();
                }
            }
        }

        public CustomersViewModel()
        {
            LoadData();
        }

        public void LoadMore()
        {
            _displayCount += PageSize;
            ApplyVisible();
        }

        private void LoadData()
        {
            try
            {
                _allCustomers = _customerBus.GetAllCustomers() ?? new ObservableCollection<Customer>();
                Customers = _allCustomers;
                OnPropertyChanged(nameof(Customers));

                VipCustomers = _allCustomers.Count(c => string.Equals(c.Status, "VIP", StringComparison.OrdinalIgnoreCase));

                var now = DateTime.Now;
                NewCustomersThisMonth = _allCustomers.Count(c => c.MemberSince.Year == now.Year && c.MemberSince.Month == now.Month);

                try
                {
                    ActiveToday = _bookingBus.CountActiveCustomersToday();
                }
                catch
                {
                    ActiveToday = 0;
                }

                ApplyFilter();
            }
            catch
            {
                _allCustomers = new ObservableCollection<Customer>();
                Customers = _allCustomers;
                OnPropertyChanged(nameof(Customers));
                VipCustomers = 0;
                NewCustomersThisMonth = 0;
                ActiveToday = 0;
                VisibleCustomers = new ObservableCollection<Customer>();
                OnPropertyChanged(nameof(VisibleCustomers));
                CanLoadMore = false;
            }
        }

        private void ApplyFilter()
        {
            var filtered = _allCustomers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string keyword = SearchText.Trim();
                filtered = filtered.Where(c =>
                    (!string.IsNullOrEmpty(c.Id) && c.Id.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.Name) && c.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.Phone) && c.Phone.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.Email) && c.Email.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
            }

            Customers = new ObservableCollection<Customer>(filtered);
            OnPropertyChanged(nameof(Customers));

            ApplyVisible();
        }

        private void ApplyVisible()
        {
            var visible = Customers.Take(_displayCount).ToList();
            VisibleCustomers = new ObservableCollection<Customer>(visible);
            OnPropertyChanged(nameof(VisibleCustomers));

            CanLoadMore = Customers.Count > VisibleCustomers.Count;
        }
    }
}
