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
        public ObservableCollection<TimeSlot> StartTimeSlots { get; set; } = new ObservableCollection<TimeSlot>();
        public ObservableCollection<DurationOption> DurationOptions { get; set; } = new ObservableCollection<DurationOption>();
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

        private DurationOption _selectedDuration;
        public DurationOption SelectedDuration
        {
            get => _selectedDuration;
            set
            {
                if (SetProperty(ref _selectedDuration, value))
                    RecalcPrice();
            }
        }

        private string _durationErrorText;
        public string DurationErrorText
        {
            get => _durationErrorText;
            set => SetProperty(ref _durationErrorText, value);
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
                StartTimeSlots = new ObservableCollection<TimeSlot>(
                    (TimeSlots ?? new ObservableCollection<TimeSlot>())
                        .Where(s => s != null && s.EndTime > s.StartTime)
                        .OrderBy(s => s.StartTime)
                        .ThenBy(s => s.EndTime)
                        .GroupBy(s => s.StartTime)
                        .Select(g => g.First())
                        .OrderBy(s => s.StartTime)
                        .ToList());
                DurationOptions = BuildDurationOptions();

                if (IsAdminOrStaff)
                {
                    Customers = _customerBus.GetAllCustomers();

                    // Add walk-in option so staff can book without creating a customer record.
                    Customers.Insert(0, new Customer
                    {
                        Id = string.Empty,
                        Name = "Khách vãng lai",
                        Phone = string.Empty,
                        Email = string.Empty
                    });
                }

                if (!string.IsNullOrEmpty(defaultCourtId))
                    SelectedCourt = Courts.FirstOrDefault(c => c.Id == defaultCourtId);
                else
                    SelectedCourt = Courts.FirstOrDefault();

                if (!string.IsNullOrEmpty(defaultSlotId))
                {
                    var byId = (TimeSlots ?? new ObservableCollection<TimeSlot>()).FirstOrDefault(s => s.Id == defaultSlotId);
                    if (byId != null)
                    {
                        SelectedSlot = (StartTimeSlots ?? new ObservableCollection<TimeSlot>())
                                           .FirstOrDefault(s => s.StartTime == byId.StartTime) ?? byId;
                    }
                    else
                    {
                        SelectedSlot = (StartTimeSlots ?? new ObservableCollection<TimeSlot>()).FirstOrDefault();
                    }
                }
                else
                {
                    SelectedSlot = (StartTimeSlots ?? new ObservableCollection<TimeSlot>()).FirstOrDefault()
                                   ?? (TimeSlots ?? new ObservableCollection<TimeSlot>()).OrderBy(s => s.StartTime).FirstOrDefault();
                }

                SelectedDuration = DurationOptions.FirstOrDefault(o => o.Minutes == 60) ??
                                  DurationOptions.FirstOrDefault(o => o.Minutes == 90) ??
                                  DurationOptions.FirstOrDefault();

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
                DurationOptions = BuildDurationOptions();
                SelectedDuration = DurationOptions.FirstOrDefault(o => o.Minutes == 60) ?? DurationOptions.FirstOrDefault();
                ErrorText = "Không tải được dữ liệu: " + ex.Message;
            }

            RecalcPrice();
        }

        private void RecalcPrice()
        {
            DurationErrorText = null;

            if (SelectedCourt == null || SelectedSlot == null || SelectedDuration == null)
            {
                Price = 0;
                return;
            }

            try
            {
                if (!TryGetSelectedSlots(out var selectedSlots, out string slotError))
                {
                    DurationErrorText = slotError;
                    Price = 0;
                    return;
                }

                string effectiveType = BookingTypeVN;
                if (effectiveType == "Cố định" && NumberOfWeeks < 4)
                {
                    effectiveType = "Lẻ"; // Hiển thị giá Lẻ nếu số buổi chưa đủ điều kiện
                }

                var slotIds = selectedSlots.Select(s => s.Id).ToList();
                Price = _bookingBus.GetTotalPriceForCourtSlots(SelectedCourt.Id, slotIds, effectiveType);
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

            // If staff chooses walk-in AND didn't provide quick-create fields, allow booking without a customer.
            if (string.IsNullOrWhiteSpace(CustomerName) &&
                string.IsNullOrWhiteSpace(CustomerPhone) &&
                string.IsNullOrWhiteSpace(CustomerEmail))
            {
                return (true, null, null);
            }

            return _customerBus.CreateCustomer(CustomerName, CustomerPhone, CustomerEmail);
        }

        public (bool ok, string bookingId, string error) CreateBooking()
        {
            ErrorText = null;
            DurationErrorText = null;

            if (UsageDate.Date < DateTime.Today) return (false, null, "Không thể đặt sân trong quá khứ.");
            if (SelectedCourt == null) return (false, null, "Vui lòng chọn sân.");
            if (SelectedCourt.Status == "Maintenance") return (false, null, "Sân đang bảo trì, không thể đặt.");
            if (SelectedSlot == null) return (false, null, "Vui lòng chọn ca giờ.");
            if (SelectedDuration == null) return (false, null, "Vui lòng chọn thời lượng.");
            
            // Business Logic Rule: Fixed bookings must span at least 4 weeks to get the discounted price.
            if (BookingTypeVN == "Cố định" && NumberOfWeeks < 4) 
            {
                return (false, null, "Để hưởng mức giá Cố định, bạn phải đặt lịch duy trì liên tục ít nhất 4 tuần (4 buổi). Vui lòng đổi sang loại đặt 'Lẻ' hoặc tăng số buổi lên.");
            }

            if (!TryGetSelectedSlots(out var requiredSlots, out string slotError))
            {
                DurationErrorText = slotError;
                return (false, null, slotError);
            }

            var requiredSlotIds = requiredSlots.Select(s => s.Id).ToList();

            // Rule: never allow booking a past start time on the current day (applies to all roles).
            if (UsageDate.Date == DateTime.Today)
            {
                var startTime = requiredSlots.FirstOrDefault()?.StartTime ?? TimeSpan.Zero;
                if (startTime < DateTime.Now.TimeOfDay)
                {
                    return (false, null, "Không thể đặt ca đã qua trong ngày hôm nay.");
                }
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
                var busy = _bookingBus.GetBusyCourtSlots(SelectedCourt.Id, requiredSlotIds, date);
                if (busy.Count > 0)
                {
                    // Try map to display labels
                    var busyLabels = requiredSlots
                        .Where(s => busy.Any(b => string.Equals(b, s.Id, StringComparison.OrdinalIgnoreCase)))
                        .Select(s => s.DisplayLabel)
                        .ToList();

                    string detail = busyLabels.Count > 0 ? string.Join(", ", busyLabels) : string.Join(", ", busy);
                    return (false, null, $"Ngày {date:dd/MM/yyyy} đã có người đặt các ca: {detail}. Vui lòng chọn lịch khác.");
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
                    requiredSlotIds,
                    date,
                    BookingTypeVN,
                    currentServices,
                    false,
                    out string currentId,
                    out string error);

                if (!ok) return (false, null, $"Lỗi khi tạo lịch cho ngày {date:dd/MM/yyyy}: {error}");
                if (i == 0) firstBookingId = currentId;
            }

            return (true, firstBookingId, null);
        }

        private bool TryGetSelectedSlots(out List<TimeSlot> slots, out string error)
        {
            slots = new List<TimeSlot>();
            error = null;

            if (SelectedSlot == null)
            {
                error = "Vui lòng chọn ca giờ.";
                return false;
            }

            int minutes = SelectedDuration?.Minutes ?? 0;
            if (minutes <= 0)
            {
                error = "Vui lòng chọn thời lượng hợp lệ.";
                return false;
            }

            if (SelectedSlot.EndTime <= SelectedSlot.StartTime)
            {
                error = "Ca giờ không hợp lệ (giờ kết thúc phải sau giờ bắt đầu).";
                return false;
            }

            TimeSpan targetEnd = SelectedSlot.StartTime.Add(TimeSpan.FromMinutes(minutes));
            if (targetEnd <= SelectedSlot.StartTime)
            {
                error = "Thời lượng không hợp lệ.";
                return false;
            }

            var timeSlotsOrdered = (TimeSlots ?? new ObservableCollection<TimeSlot>())
                .OrderBy(s => s.StartTime)
                .ThenBy(s => s.EndTime)
                .ToList();

            var byStart = timeSlotsOrdered
                .GroupBy(s => s.StartTime)
                .ToDictionary(g => g.Key, g => g.First());

            TimeSlot current = byStart.TryGetValue(SelectedSlot.StartTime, out var startSlot)
                ? startSlot
                : (timeSlotsOrdered.FirstOrDefault(s => s.Id == SelectedSlot.Id) ?? SelectedSlot);
            if (current.EndTime <= current.StartTime)
            {
                error = "Ca giờ không hợp lệ.";
                return false;
            }

            slots.Add(current);
            TimeSpan currentEnd = current.EndTime;

            while (currentEnd < targetEnd)
            {
                if (!byStart.TryGetValue(currentEnd, out var next))
                {
                    error = "Không thể ghép đủ thời lượng với ca giờ hiện có. " +
                            "Gợi ý: cấu hình CA_GIO theo bước 30 phút và liền kề nhau.";
                    return false;
                }

                if (next.EndTime <= next.StartTime)
                {
                    error = $"Ca {next.DisplayLabel} không hợp lệ.";
                    return false;
                }

                slots.Add(next);
                currentEnd = next.EndTime;
            }

            if (currentEnd != targetEnd)
            {
                error = $"Thời lượng {SelectedDuration?.Label ?? (minutes + " phút")} không khớp với các ca giờ hiện có. " +
                        $"Kết thúc hiện tại: {currentEnd:hh\\:mm} (cần: {targetEnd:hh\\:mm}).";
                return false;
            }

            return true;
        }

        private static ObservableCollection<DurationOption> BuildDurationOptions()
        {
            var list = new ObservableCollection<DurationOption>();
            for (int minutes = 30; minutes <= 300; minutes += 30)
            {
                list.Add(DurationOption.FromMinutes(minutes));
            }
            return list;
        }
    }

    public class DurationOption
    {
        public int Minutes { get; set; }
        public string Label { get; set; }

        public static DurationOption FromMinutes(int minutes)
        {
            if (minutes <= 0) return new DurationOption { Minutes = 0, Label = "0 phút" };

            int hours = minutes / 60;
            int mins = minutes % 60;

            string label;
            if (hours == 0) label = $"{mins} phút";
            else if (mins == 0) label = $"{hours} giờ";
            else label = $"{hours} giờ {mins} phút";

            return new DurationOption { Minutes = minutes, Label = label };
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
                if (value < 0) value = 0;
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
