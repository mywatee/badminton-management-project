using QuanLySCL.BUS;
using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace QuanLySCL.GUI.ViewModels
{
    public class ServicePickItem : BaseViewModel
    {
        private readonly Action _onChanged;

        public ServicePickItem(Service service, Action onChanged)
        {
            Service = service ?? new Service();
            _onChanged = onChanged ?? (() => { });
        }

        public Service Service { get; }

        public string Id => Service.Id;
        public string Name => Service.Name;
        public string Category => Service.Category;
        public string Unit => Service.Unit;
        public decimal Price => Service.Price;
        public int Stock => Service.Stock;
        public bool IsOutOfStock => Service.IsOutOfStock;
        public bool IsLowStock => Service.IsLowStock;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    if (_isSelected && _quantity < 1)
                        Quantity = 1;

                    OnPropertyChanged(nameof(LineTotal));
                    _onChanged();
                }
            }
        }

        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set
            {
                int next = value < 1 ? 1 : value;
                if (SetProperty(ref _quantity, next))
                {
                    OnPropertyChanged(nameof(LineTotal));
                    _onChanged();
                }
            }
        }

        public decimal LineTotal => IsSelected ? Price * Quantity : 0m;
    }

    public class AddServiceViewModel : BaseViewModel
    {
        private readonly ServiceBUS _serviceBus = new ServiceBUS();
        private readonly string _bookingId;

        public ObservableCollection<Service> Services { get; private set; }

        private ObservableCollection<ServicePickItem> _allServiceItems = new ObservableCollection<ServicePickItem>();

        private ServicePickItem? _activeItem;
        public ServicePickItem? ActiveItem
        {
            get => _activeItem;
            set
            {
                if (SetProperty(ref _activeItem, value))
                    OnPropertyChanged(nameof(HasActiveItem));
            }
        }

        public bool HasActiveItem => ActiveItem != null;

        public decimal TotalPrice => _allServiceItems?.Sum(i => i.LineTotal) ?? 0m;
        public int SelectedCount => _allServiceItems?.Count(i => i.IsSelected) ?? 0;

        private string _categoryFilter = "All";
        public string CategoryFilter
        {
            get => _categoryFilter;
            set
            {
                if (SetProperty(ref _categoryFilter, value))
                    FilterServices();
            }
        }

        private ObservableCollection<ServicePickItem> _filteredServices = new ObservableCollection<ServicePickItem>();
        public ObservableCollection<ServicePickItem> FilteredServices
        {
            get => _filteredServices;
            set => SetProperty(ref _filteredServices, value);
        }

        private readonly RelayCommand _addCommand;
        public ICommand AddCommand => _addCommand;
        public ICommand FilterCommand { get; }
        public ICommand IncreaseCommand { get; }
        public ICommand DecreaseCommand { get; }

        public AddServiceViewModel(string bookingId)
        {
            _bookingId = bookingId;
            Services = _serviceBus.GetAllServices();

            // Deduplicate by Name to fix the duplication issue reported by user
            var distinctServices = (Services ?? new ObservableCollection<Service>())
                .Where(s => s != null)
                .GroupBy(s => s.Name)
                .Select(g => g.First())
                .ToList();

            _allServiceItems = new ObservableCollection<ServicePickItem>(
                distinctServices.Select(s => new ServicePickItem(s, OnSelectionChanged)));

            FilterServices(); // populate initial list
            ActiveItem = FilteredServices.FirstOrDefault();

            _addCommand = new RelayCommand(ExecuteAdd, () => SelectedCount > 0);
            FilterCommand = new RelayCommand((param) => CategoryFilter = param?.ToString() ?? "All");
            IncreaseCommand = new RelayCommand(() =>
            {
                if (ActiveItem != null) ActiveItem.Quantity++;
            });
            DecreaseCommand = new RelayCommand(() =>
            {
                if (ActiveItem != null && ActiveItem.Quantity > 1) ActiveItem.Quantity--;
            });
        }

        private void OnSelectionChanged()
        {
            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(SelectedCount));
            _addCommand.RaiseCanExecuteChanged();
        }

        private void FilterServices()
        {
            var query = _allServiceItems.AsEnumerable();
            if (CategoryFilter != "All")
                query = query.Where(s => s.Category == CategoryFilter);

            FilteredServices = new ObservableCollection<ServicePickItem>(query.OrderBy(s => s.Name));

            if (ActiveItem == null || !FilteredServices.Contains(ActiveItem))
                ActiveItem = FilteredServices.FirstOrDefault();
        }

        private void ExecuteAdd()
        {
            var selected = _allServiceItems
                .Where(i => i.IsSelected && i.Quantity > 0)
                .ToList();

            if (selected.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất 1 dịch vụ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var errors = new List<string>();
            foreach (var item in selected)
            {
                var detail = new BookingServiceDetail(_bookingId, item.Service, item.Quantity);
                if (!_serviceBus.AddServiceToBooking(detail, out string? error))
                {
                    errors.Add($"- {item.Name}: {error}");
                }
            }

            if (errors.Count > 0)
            {
                MessageBox.Show(
                    "Không thể thêm một số dịch vụ:\n" + string.Join("\n", errors),
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            var lines = selected.Select(s => $"- {s.Quantity} {s.Unit} {s.Name}").ToList();
            MessageBox.Show(
                "Đã thêm dịch vụ:\n" + string.Join("\n", lines),
                "Thành công",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            RequestClose?.Invoke(this, true);
        }

        public delegate void RequestCloseEventHandler(object sender, bool dialogResult);
        public event RequestCloseEventHandler RequestClose;
    }
}
