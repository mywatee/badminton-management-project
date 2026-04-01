using QuanLySCL.BUS;
using QuanLySCL.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QuanLySCL.GUI.ViewModels
{
    public class CreateBookingViewModel : BaseViewModel
    {
        private readonly CourtBUS _courtBus = new CourtBUS();
        private readonly BookingBUS _bookingBus = new BookingBUS();
        private readonly CustomerBUS _customerBus = new CustomerBUS();
        
        public string UserRole { get; set; } = "Admin";
        public string CurrentCustomerId { get; set; }
        
        private bool _isAdminOrStaff = true;
        public bool IsAdminOrStaff
        {
            get => _isAdminOrStaff;
            set => SetProperty(ref _isAdminOrStaff, value);
        }

        public ObservableCollection<Court> Courts { get; set; } = new ObservableCollection<Court>();
        public ObservableCollection<TimeSlot> TimeSlots { get; set; } = new ObservableCollection<TimeSlot>();
        public ObservableCollection<Customer> Customers { get; set; } = new ObservableCollection<Customer>();
        public ObservableCollection<ServiceSelectionViewModel> AvailableServices { get; set; } = new ObservableCollection<ServiceSelectionViewModel>();

        private DateTime _usageDate = DateTime.Today;
        public DateTime UsageDate
        {
            get => _usageDate;
            set
            {
                if (SetProperty(ref _usageDate, value))
                    RecalcPrice();
            }
        }

        private Court _selectedCourt;
        public Court SelectedCourt
        {
            get => _selectedCourt;
            set
            {
                if (SetProperty(ref _selectedCourt, value))
                    RecalcPrice();
            }
        }

        private TimeSlot _selectedSlot;
        public TimeSlot SelectedSlot
        {
            get => _selectedSlot;
            set
            {
                if (SetProperty(ref _selectedSlot, value))
                    RecalcPrice();
            }
        }

        private Customer _selectedCustomer;
        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set => SetProperty(ref _selectedCustomer, value);
        }

        private string _customerName;
        public string CustomerName
        {
            get => _customerName;
            set => SetProperty(ref _customerName, value);
        }

        private string _customerPhone;
        public string CustomerPhone
        {
            get => _customerPhone;
            set => SetProperty(ref _customerPhone, value);
        }

        private string _customerEmail;
        public string CustomerEmail
        {
            get => _customerEmail;
            set => SetProperty(ref _customerEmail, value);
        }

        private string _bookingTypeVN = "Lẻ";
        public string BookingTypeVN
        {
            get => _bookingTypeVN;
            set
            {
                if (SetProperty(ref _bookingTypeVN, value))
                    RecalcPrice();
            }
        }

        private decimal _price;
        public decimal Price
        {
            get => _price;
            set
            {
                if (SetProperty(ref _price, value))
                    OnPropertyChanged(nameof(TotalPrice));
            }
        }

        private int _numberOfWeeks = 1;
        public int NumberOfWeeks
        {
            get => _numberOfWeeks;
            set
            {
                if (value < 1) value = 1;
                if (SetProperty(ref _numberOfWeeks, value))
                {
                    RecalcPrice();
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        private string _errorText;
        public string ErrorText
        {
            get => _errorText;
            set => SetProperty(ref _errorText, value);
        }

        public decimal TotalPrice => (Price * NumberOfWeeks) + AvailableServices.Sum(s => s.TotalPrice);

        public CreateBookingViewModel(DateTime? defaultDate = null, string defaultCourtId = null, string defaultSlotId = null, string role = "Admin", string customerId = null)
        {
            UserRole = role;
            CurrentCustomerId = customerId;
            IsAdminOrStaff = (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) || 
                              string.Equals(role, "NhanVien", StringComparison.OrdinalIgnoreCase));
            
            UsageDate = defaultDate?.Date ?? DateTime.Today;
            LoadData(defaultCourtId, defaultSlotId);
        }

        private void LoadData(string defaultCourtId = null, string defaultSlotId = null)
        {
            try
            {
                Courts = _courtBus.GetAllCourts();
                TimeSlots = _bookingBus.GetAllTimeSlots();

                if (IsAdminOrStaff)
                {
                    Customers = _customerBus.GetAllCustomers();
                }

                if (!string.IsNullOrEmpty(defaultCourtId))
                    SelectedCourt = Courts.FirstOrDefault(c => c.Id == defaultCourtId);
                else
                    SelectedCourt = Courts.FirstOrDefault();

                if (!string.IsNullOrEmpty(defaultSlotId))
                    SelectedSlot = TimeSlots.FirstOrDefault(s => s.Id == defaultSlotId);
                else
                    SelectedSlot = TimeSlots.OrderBy(s => s.StartTime).FirstOrDefault();

                if (IsAdminOrStaff)
                {
                    SelectedCustomer = Customers.FirstOrDefault();
                }
                else if (!string.IsNullOrEmpty(CurrentCustomerId))
                {
                    // For customers, pre-fill their own data
                    var customer = _customerBus.GetCustomerById(CurrentCustomerId);
                    if (customer != null)
                    {
                        CustomerName = customer.Name;
                        CustomerPhone = customer.Phone;
                        CustomerEmail = customer.Email;
                        // Use a dummy selected customer object to pass the validation if needed, 
                        // or ResolveCustomer will handle it via ID.
                        SelectedCustomer = customer;
                    }
                }

                var services = new ServiceBUS().GetAllServices();
                AvailableServices = new ObservableCollection<ServiceSelectionViewModel>(
                    services.Select(s => new ServiceSelectionViewModel(s, () => OnPropertyChanged(nameof(TotalPrice))))
                );
            }
            catch (Exception ex)
            {
                ErrorText = "Không tải được dữ liệu: " + ex.Message;
            }

            RecalcPrice();
        }

        private void RecalcPrice()
        {
            if (SelectedCourt == null || SelectedSlot == null)
            {
                Price = 0;
                return;
            }

            try
            {
                string effectiveType = BookingTypeVN;
                if (effectiveType == "Cố định" && NumberOfWeeks < 4)
                {
                    effectiveType = "Lẻ"; // Hiển thị giá Lẻ nếu số buổi chưa đủ điều kiện
                }
                Price = _bookingBus.GetPriceForCourtSlot(SelectedCourt.Id, SelectedSlot.Id, effectiveType);
            }
            catch
            {
                Price = 0;
            }
        }

        public (bool ok, string customerId, string error) ResolveCustomer()
        {
            if (SelectedCustomer != null && !string.IsNullOrWhiteSpace(SelectedCustomer.Id))
                return (true, SelectedCustomer.Id, null);

            return _customerBus.CreateCustomer(CustomerName, CustomerPhone, CustomerEmail);
        }

        public (bool ok, string bookingId, string error) CreateBooking()
        {
            ErrorText = null;

            if (UsageDate.Date < DateTime.Today) return (false, null, "Không thể đặt sân trong quá khứ.");
            if (SelectedCourt == null) return (false, null, "Vui lòng chọn sân.");
            if (SelectedCourt.Status == "Maintenance") return (false, null, "Sân đang bảo trì, không thể đặt.");
            if (SelectedSlot == null) return (false, null, "Vui lòng chọn ca giờ.");
            
            // Business Logic Rule: Fixed bookings must span at least 4 weeks to get the discounted price.
            if (BookingTypeVN == "Cố định" && NumberOfWeeks < 4) 
            {
                return (false, null, "Để hưởng mức giá Cố định, bạn phải đặt lịch duy trì liên tục ít nhất 4 tuần (4 buổi). Vui lòng đổi sang loại đặt 'Lẻ' hoặc tăng số buổi lên.");
            }

            // 1. Prepare list of dates
            List<DateTime> sessionDates = new List<DateTime>();
            for (int i = 0; i < NumberOfWeeks; i++)
            {
                sessionDates.Add(UsageDate.Date.AddDays(i * 7));
            }

            // 2. Check availability for ALL dates first
            foreach (var date in sessionDates)
            {
                if (!_bookingBus.IsCourtSlotFree(SelectedCourt.Id, SelectedSlot.Id, date))
                {
                    return (false, null, $"Ngày {date:dd/MM/yyyy} đã có người đặt khung giờ này. Vui lòng chọn lịch khác.");
                }
            }

            var customerRes = ResolveCustomer();
            if (!customerRes.ok) return (false, null, customerRes.error);

            // 3. Create bookings
            string firstBookingId = null;
            for (int i = 0; i < sessionDates.Count; i++)
            {
                var date = sessionDates[i];
                
                // Only include services in the first session (as per plan)
                var currentServices = (i == 0) 
                    ? AvailableServices.Where(s => s.Quantity > 0).Select(s => (s.Service.Id, s.Quantity, s.Service.Price)).ToList()
                    : new List<(string, int, decimal)>();

                bool ok = _bookingBus.CreateBooking(
                    customerRes.customerId,
                    SelectedCourt.Id,
                    SelectedSlot.Id,
                    date,
                    BookingTypeVN,
                    currentServices,
                    out string currentId,
                    out string error);

                if (!ok) return (false, null, $"Lỗi khi tạo lịch cho ngày {date:dd/MM/yyyy}: {error}");
                if (i == 0) firstBookingId = currentId;
            }

            return (true, firstBookingId, null);
        }
    }

    public class ServiceSelectionViewModel : BaseViewModel
    {
        private Action _onChanged;
        public Service Service { get; }
        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    OnPropertyChanged(nameof(TotalPrice));
                    _onChanged?.Invoke();
                }
            }
        }
        public decimal TotalPrice => Service.Price * Quantity;

        public ServiceSelectionViewModel(Service service, Action onChanged)
        {
            Service = service;
            _onChanged = onChanged;
        }
    }
}
