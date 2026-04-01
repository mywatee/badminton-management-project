using QuanLySCL.BUS;
using QuanLySCL.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace QuanLySCL.GUI.ViewModels
{
    public class AddServiceViewModel : BaseViewModel
    {
        private readonly ServiceBUS _serviceBus = new ServiceBUS();
        private readonly string _bookingId;

        public ObservableCollection<Service> Services { get; private set; }
        
        private Service _selectedService;
        public Service SelectedService
        {
            get => _selectedService;
            set
            {
                if (SetProperty(ref _selectedService, value))
                    OnPropertyChanged(nameof(TotalPrice));
            }
        }

        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value < 1 ? 1 : value))
                    OnPropertyChanged(nameof(TotalPrice));
            }
        }

        public decimal TotalPrice => (SelectedService?.Price ?? 0) * Quantity;

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

        private ObservableCollection<Service> _filteredServices;
        public ObservableCollection<Service> FilteredServices
        {
            get => _filteredServices;
            set => SetProperty(ref _filteredServices, value);
        }

        public ICommand AddCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand IncreaseCommand { get; }
        public ICommand DecreaseCommand { get; }

        public AddServiceViewModel(string bookingId)
        {
            _bookingId = bookingId;
            Services = _serviceBus.GetAllServices();
            FilterServices(); // Use the filter logic to populate initial list
            SelectedService = FilteredServices.FirstOrDefault();

            AddCommand = new RelayCommand(ExecuteAdd);
            FilterCommand = new RelayCommand((param) => CategoryFilter = param?.ToString() ?? "All");
            IncreaseCommand = new RelayCommand(() => Quantity++);
            DecreaseCommand = new RelayCommand(() => { if (Quantity > 1) Quantity--; });
        }

        private void FilterServices()
        {
            var query = Services.AsEnumerable();
            if (CategoryFilter != "All")
                query = query.Where(s => s.Category == CategoryFilter);
            
            // Deduplicate by Name to fix the duplication issue reported by user
            var distinct = query.GroupBy(s => s.Name).Select(g => g.First()).ToList();
            FilteredServices = new ObservableCollection<Service>(distinct);
        }

        private void ExecuteAdd()
        {
            if (SelectedService == null)
            {
                MessageBox.Show("Vui lòng chọn dịch vụ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var detail = new BookingServiceDetail(_bookingId, SelectedService, Quantity);
            if (_serviceBus.AddServiceToBooking(detail, out string error))
            {
                // Close window logic handled in code-behind or via messenger if needed
                MessageBox.Show($"Đã thêm {Quantity} {SelectedService.Unit} {SelectedService.Name}.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                // We'll use a property or event to signal closing
                RequestClose?.Invoke(this, true);
            }
            else
            {
                MessageBox.Show($"Lỗi: {error}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public delegate void RequestCloseEventHandler(object sender, bool dialogResult);
        public event RequestCloseEventHandler RequestClose;
    }
}
