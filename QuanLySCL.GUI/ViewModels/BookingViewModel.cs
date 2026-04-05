using QuanLySCL.BUS;
using QuanLySCL.BUS;
using QuanLySCL.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace QuanLySCL.GUI.ViewModels
{
    public class BookingViewModel : BaseViewModel
    {
        private const int BookingPageSize = 10;

        private readonly CourtBUS _courtBus = new CourtBUS();
        private readonly BookingBUS _bookingBus = new BookingBUS();

        private string _role;
        public string UserRole => _role;

        private string _customerId;
        public string CustomerId => _customerId;

        private ObservableCollection<Booking> _allBookings = new ObservableCollection<Booking>();
        private ObservableCollection<CourtScheduleItem> _scheduleBookings = new ObservableCollection<CourtScheduleItem>();

        public ObservableCollection<Booking> Bookings { get; set; } = new ObservableCollection<Booking>();
        public ObservableCollection<Booking> VisibleBookings { get; set; } = new ObservableCollection<Booking>();
        public ObservableCollection<Court> Courts { get; set; } = new ObservableCollection<Court>();
        public ObservableCollection<TimeSlot> TimeSlots { get; set; } = new ObservableCollection<TimeSlot>();

        // Compact schedule view (selected day in the current week)
        public ObservableCollection<WeekDayViewModel> WeekDays { get; set; } = new ObservableCollection<WeekDayViewModel>();
        public ObservableCollection<ScheduleRowViewModel> ScheduleRows { get; set; } = new ObservableCollection<ScheduleRowViewModel>();

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    ApplyBookingFilter();
            }
        }

        private int _bookingDisplayCount = BookingPageSize;

        private bool _canLoadMoreBookings;
        public bool CanLoadMoreBookings
        {
            get => _canLoadMoreBookings;
            set => SetProperty(ref _canLoadMoreBookings, value);
        }

        private DateTime? _selectedDate;
        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                    RefreshSchedule();
            }
        }

        private int _scheduleViewMinutes = 60;
        public int ScheduleViewMinutes
        {
            get => _scheduleViewMinutes;
            set
            {
                int normalized = (value == 30) ? 30 : 60;
                if (SetProperty(ref _scheduleViewMinutes, normalized))
                {
                    OnPropertyChanged(nameof(IsScheduleCompactView));
                    OnPropertyChanged(nameof(ScheduleCellPadding));
                    OnPropertyChanged(nameof(ScheduleRowMargin));
                    SaveSchedulePrefs();
                    RefreshSchedule();
                }
            }
        }

        public bool IsScheduleCompactView => ScheduleViewMinutes >= 60;
        public Thickness ScheduleCellPadding => IsScheduleCompactView ? new Thickness(10, 6, 10, 6) : new Thickness(12, 10, 12, 10);
        public Thickness ScheduleRowMargin => IsScheduleCompactView ? new Thickness(0, 0, 0, 6) : new Thickness(0, 0, 0, 10);

        private string _currentWeekRange;
        public string CurrentWeekRange
        {
            get => _currentWeekRange;
            set => SetProperty(ref _currentWeekRange, value);
        }

        public BookingViewModel(string role = "KhachHang", string customerId = null)
        {
            _role = string.IsNullOrEmpty(role) ? "KhachHang" : role;
            _customerId = customerId;
            LoadSchedulePrefs();
            LoadData();
        }

        public void LoadMoreBookings()
        {
            _bookingDisplayCount += BookingPageSize;
            ApplyVisibleBookings();
        }

        public void ResetBookingPaging()
        {
            _bookingDisplayCount = BookingPageSize;
            ApplyVisibleBookings();
        }

        public void ReloadFromDatabase()
        {
            DateTime keepDate = (SelectedDate ?? DateTime.Today).Date;

            try
            {
                Courts = _courtBus.GetAllCourts();
                TimeSlots = _bookingBus.GetAllTimeSlots();
                
                bool isCustomer = string.Equals(_role, "KhachHang", StringComparison.OrdinalIgnoreCase);
                
                if (isCustomer && !string.IsNullOrEmpty(_customerId))
                {
                    _allBookings = _bookingBus.GetBookingsByCustomerId(_customerId);
                }
                else
                {
                    _allBookings = _bookingBus.GetAllBookings();
                }
            }
            catch
            {
                UseSampleData();
            }

            OnPropertyChanged(nameof(Courts));
            OnPropertyChanged(nameof(TimeSlots));
            _bookingDisplayCount = BookingPageSize;
            ApplyBookingFilter();

            // Force refresh even when the date is unchanged
            _selectedDate = keepDate;
            OnPropertyChanged(nameof(SelectedDate));
            RefreshSchedule();
        }

        public void MoveWeek(int offset)
        {
            SelectedDate = (SelectedDate ?? DateTime.Today).AddDays(offset * 7);
        }

        public void GoToToday()
        {
            SelectedDate = DateTime.Today;
        }

        private void LoadData()
        {
            try
            {
                Courts = _courtBus.GetAllCourts();
                TimeSlots = _bookingBus.GetAllTimeSlots();
                
                bool isCustomer = string.Equals(_role, "KhachHang", StringComparison.OrdinalIgnoreCase);
                
                if (isCustomer && !string.IsNullOrEmpty(_customerId))
                {
                    _allBookings = _bookingBus.GetBookingsByCustomerId(_customerId);
                }
                else
                {
                    _allBookings = _bookingBus.GetAllBookings();
                }

                if (Courts.Count == 0 || TimeSlots.Count == 0)
                    UseSampleData();
            }
            catch
            {
                UseSampleData();
            }

            ApplyBookingFilter();
            SelectedDate = DateTime.Today;
        }

        private void RefreshSchedule()
        {
            if (!SelectedDate.HasValue || Courts.Count == 0 || TimeSlots.Count == 0)
            {
                WeekDays = new ObservableCollection<WeekDayViewModel>();
                ScheduleRows = new ObservableCollection<ScheduleRowViewModel>();
                OnPropertyChanged(nameof(WeekDays));
                OnPropertyChanged(nameof(ScheduleRows));
                return;
            }

            DateTime weekStart = GetStartOfWeek(SelectedDate.Value);
            DateTime weekEnd = weekStart.AddDays(6);
            CurrentWeekRange = $"{weekStart:dd/MM/yyyy} - {weekEnd:dd/MM/yyyy}";

            try
            {
                _scheduleBookings = _bookingBus.GetScheduleBookings(weekStart, weekEnd);
            }
            catch (Exception ex)
            {
                // Clear the list on error so we don't show old/sample data
                _scheduleBookings = new ObservableCollection<CourtScheduleItem>();
                // We could log ex here if we had a logger
            }

            BuildWeekDays(weekStart);
            BuildScheduleGridForSelectedDate();

            CurrentWeekRange = $"{weekStart:dd/MM/yyyy} - {weekEnd:dd/MM/yyyy} (Found: {_scheduleBookings.Count})";
            OnPropertyChanged(nameof(CurrentWeekRange));
        }

        private void BuildWeekDays(DateTime weekStart)
        {
            string[] dayNames = { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ nhật" };
            ObservableCollection<WeekDayViewModel> days = new ObservableCollection<WeekDayViewModel>();

            for (int i = 0; i < 7; i++)
            {
                DateTime date = weekStart.AddDays(i);
                days.Add(new WeekDayViewModel
                {
                    Date = date,
                    DayName = dayNames[i],
                    DayNumber = date.ToString("dd/MM"),
                    IsToday = date.Date == DateTime.Today,
                    IsSelectedDate = SelectedDate.HasValue && date.Date == SelectedDate.Value.Date
                });
            }

            WeekDays = days;
            OnPropertyChanged(nameof(WeekDays));
        }

        private void BuildScheduleGridForSelectedDate()
        {
            if (!SelectedDate.HasValue)
            {
                ScheduleRows = new ObservableCollection<ScheduleRowViewModel>();
                OnPropertyChanged(nameof(ScheduleRows));
                return;
            }

            DateTime date = SelectedDate.Value.Date;

            var lookup = _scheduleBookings
                .Where(b => b.Date.Date == date)
                .ToLookup(b => $"{b.SlotId?.Trim()}|{b.CourtId?.Trim()}".ToUpper(), b => b);

            ObservableCollection<ScheduleRowViewModel> rows = new ObservableCollection<ScheduleRowViewModel>();
            var courtsOrdered = Courts.OrderBy(c => c.Name).ToList();

            foreach (var window in BuildScheduleWindows())
            {
                TimeSlot slotForView = window.viewSlot;
                var slotIdsInWindow = window.slotIds;

                ScheduleRowViewModel row = new ScheduleRowViewModel { TimeLabel = slotForView.DisplayLabel };

                foreach (var court in courtsOrdered)
                {
                    var perSlotBookings = new List<(string slotId, CourtScheduleItem booking)>();
                    CourtScheduleItem booking = null;
                    foreach (var sid in slotIdsInWindow)
                    {
                        var b = lookup[$"{sid?.Trim()}|{court.Id?.Trim()}".ToUpper()].FirstOrDefault();
                        if (b != null && b.Status == "Cancelled") b = null;
                        perSlotBookings.Add((sid, b));
                        if (b == null) continue;

                        if (b.Status == "Checked-in")
                        {
                            booking = b;
                            break;
                        }

                        if (booking == null) booking = b;
                    }

                    string statusKey = GetWindowStatus(court, perSlotBookings);

                    string details;
                    if (statusKey == "Partial")
                    {
                        details = BuildWindowDetails(date, slotForView, court, perSlotBookings);
                    }
                    else
                    {
                        details = BuildDetails(date, slotForView, court, booking, _role);
                    }

                    row.Cells.Add(new ScheduleCellViewModel
                    {
                        CourtId = court.Id,
                        CourtName = court.Name,
                        SlotId = slotForView.Id,
                        SlotName = slotForView.DisplayLabel,
                        StatusKey = statusKey,
                        StatusText = GetCompactStatusText(statusKey),
                        BookingId = booking?.BookingId,
                        BookingStatus = booking?.Status,
                        Details = details
                    });
                }

                rows.Add(row);
            }

            ScheduleRows = rows;
            OnPropertyChanged(nameof(ScheduleRows));
        }

        private IEnumerable<(TimeSlot viewSlot, List<string> slotIds)> BuildScheduleWindows()
        {
            var timeSlotsOrdered = (TimeSlots ?? new ObservableCollection<TimeSlot>())
                .OrderBy(s => s.StartTime)
                .ThenBy(s => s.EndTime)
                .ToList();

            if (timeSlotsOrdered.Count == 0)
                yield break;

            if (ScheduleViewMinutes == 30)
            {
                foreach (var s in timeSlotsOrdered)
                    yield return (s, new List<string> { s.Id });
                yield break;
            }

            var byStart = timeSlotsOrdered
                .GroupBy(s => s.StartTime)
                .ToDictionary(g => g.Key, g => g.First());

            TimeSpan minStart = timeSlotsOrdered.Min(s => s.StartTime);
            TimeSpan maxEnd = timeSlotsOrdered.Max(s => s.EndTime);

            var duration = TimeSpan.FromMinutes(60);

            // Align to the hour for compact view.
            TimeSpan alignedStart = new TimeSpan(minStart.Hours, 0, 0);
            if (alignedStart > minStart) alignedStart = alignedStart.Add(TimeSpan.FromHours(-1));

            for (TimeSpan start = alignedStart; start < maxEnd; start = start.Add(duration))
            {
                if (!byStart.TryGetValue(start, out var first))
                    continue;

                var list = new List<TimeSlot> { first };
                TimeSpan end = first.EndTime;

                while (end < start.Add(duration))
                {
                    if (!byStart.TryGetValue(end, out var next))
                        break;
                    if (next.EndTime <= next.StartTime)
                        break;

                    list.Add(next);
                    end = next.EndTime;
                }

                if (list.Count == 0) continue;

                TimeSpan viewEnd = end;
                if (viewEnd > start.Add(duration))
                    viewEnd = start.Add(duration);

                var viewSlot = new TimeSlot
                {
                    Id = first.Id,
                    Name = $"{start:hh\\:mm} - {viewEnd:hh\\:mm}",
                    StartTime = start,
                    EndTime = viewEnd,
                    LaKhungGioVang = list.Any(s => s.LaKhungGioVang)
                };

                yield return (viewSlot, list.Select(s => s.Id).ToList());
            }
        }

        private static string GetWindowStatus(Court court, List<(string slotId, CourtScheduleItem booking)> perSlotBookings)
        {
            if (court?.Status == "Maintenance") return "Maintenance";

            if (perSlotBookings == null || perSlotBookings.Count == 0)
                return "Available";

            bool anyInUse = perSlotBookings.Any(x => x.booking != null && x.booking.Status == "Checked-in");
            if (anyInUse) return "InUse";

            bool anyBooked = perSlotBookings.Any(x => x.booking != null && x.booking.Status != "Cancelled");
            bool anyFree = perSlotBookings.Any(x => x.booking == null || x.booking.Status == "Cancelled");

            if (!anyBooked) return "Available";
            if (!anyFree) return "Booked";
            return "Partial";
        }

        private string BuildWindowDetails(DateTime date, TimeSlot viewSlot, Court court, List<(string slotId, CourtScheduleItem booking)> perSlotBookings)
        {
            var free = perSlotBookings.Where(x => x.booking == null || x.booking.Status == "Cancelled").Select(x => x.slotId).ToList();
            var busy = perSlotBookings.Where(x => x.booking != null && x.booking.Status != "Cancelled").Select(x => x.slotId).ToList();

            string freeStr = string.Join(", ", ResolveSlotLabels(free));
            string busyStr = string.Join(", ", ResolveSlotLabels(busy));

            return string.Join(
                "\n",
                new[]
                {
                    $"{court.Name} - {viewSlot.DisplayLabel}",
                    $"{date:dd/MM/yyyy}",
                    $"Có thể đặt 30p: {freeStr}",
                    $"Đã bận: {busyStr}",
                    "Gợi ý: bấm để xem chi tiết 30 phút."
                });
        }

        private IEnumerable<string> ResolveSlotLabels(IEnumerable<string> slotIds)
        {
            if (slotIds == null) return Array.Empty<string>();

            var idSet = new HashSet<string>(slotIds.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()), StringComparer.OrdinalIgnoreCase);
            if (idSet.Count == 0) return Array.Empty<string>();

            return (TimeSlots ?? new ObservableCollection<TimeSlot>())
                .Where(s => idSet.Contains(s.Id))
                .OrderBy(s => s.StartTime)
                .Select(s => s.DisplayLabel)
                .ToList();
        }

        private void LoadSchedulePrefs()
        {
            try
            {
                var prefs = Helpers.UiPreferencesStore.Load();
                if (prefs != null && (prefs.ScheduleViewMinutes == 30 || prefs.ScheduleViewMinutes == 60))
                    _scheduleViewMinutes = prefs.ScheduleViewMinutes;
            }
            catch { }
        }

        private void SaveSchedulePrefs()
        {
            try
            {
                Helpers.UiPreferencesStore.Save(new Helpers.UiPreferences { ScheduleViewMinutes = ScheduleViewMinutes });
            }
            catch { }
        }

        private void ApplyBookingFilter()
        {
            IEnumerable<Booking> filtered = _allBookings;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string keyword = SearchText.Trim();
                filtered = filtered.Where(booking =>
                    (booking.Id?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (booking.Customer?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (booking.Phone?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (booking.Court?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (booking.Status?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (booking.Type?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            Bookings = new ObservableCollection<Booking>(filtered.OrderByDescending(b => b.Date).ThenBy(b => b.Time));
            OnPropertyChanged(nameof(Bookings));

            _bookingDisplayCount = BookingPageSize;
            ApplyVisibleBookings();
        }

        private void ApplyVisibleBookings()
        {
            var take = Math.Max(BookingPageSize, _bookingDisplayCount);
            VisibleBookings = new ObservableCollection<Booking>(Bookings.Take(take));
            OnPropertyChanged(nameof(VisibleBookings));

            CanLoadMoreBookings = VisibleBookings.Count < Bookings.Count;
        }

        private void UseSampleData()
        {
            Courts = new ObservableCollection<Court>
            {
                new Court { Id = "S01", Name = "Court 1", Type = "Indoor", Status = "Available" },
                new Court { Id = "S02", Name = "Court 2", Type = "Indoor", Status = "Available" },
                new Court { Id = "S03", Name = "Court 3", Type = "Indoor", Status = "Maintenance" },
                new Court { Id = "S04", Name = "Court 4", Type = "Outdoor", Status = "Available" },
                new Court { Id = "S05", Name = "Court 5", Type = "Outdoor", Status = "Available" },
                new Court { Id = "S06", Name = "Court 6", Type = "Outdoor", Status = "Available" },
                new Court { Id = "S07", Name = "Court 7", Type = "Pro Mat", Status = "Available" },
                new Court { Id = "S08", Name = "Court 8", Type = "Pro Mat", Status = "Available" }
            };

            TimeSlots = new ObservableCollection<TimeSlot>
            {
                new TimeSlot { Id = "CA09", Name = "14:00 - 15:00", StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(15, 0, 0) },
                new TimeSlot { Id = "CA10", Name = "15:00 - 16:00", StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(16, 0, 0) },
                new TimeSlot { Id = "CA11", Name = "16:00 - 17:00", StartTime = new TimeSpan(16, 0, 0), EndTime = new TimeSpan(17, 0, 0) },
                new TimeSlot { Id = "CA12", Name = "17:00 - 18:00", StartTime = new TimeSpan(17, 0, 0), EndTime = new TimeSpan(18, 0, 0) },
                new TimeSlot { Id = "CA13", Name = "18:00 - 19:00", StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(19, 0, 0) },
                new TimeSlot { Id = "CA14", Name = "19:00 - 20:00", StartTime = new TimeSpan(19, 0, 0), EndTime = new TimeSpan(20, 0, 0) },
                new TimeSlot { Id = "CA15", Name = "20:00 - 21:00", StartTime = new TimeSpan(20, 0, 0), EndTime = new TimeSpan(21, 0, 0) },
                new TimeSlot { Id = "CA16", Name = "21:00 - 22:00", StartTime = new TimeSpan(21, 0, 0), EndTime = new TimeSpan(22, 0, 0) }
            };

            _allBookings = new ObservableCollection<Booking>();
            _scheduleBookings = BuildSampleScheduleBookings();
        }

        private ObservableCollection<CourtScheduleItem> BuildSampleScheduleBookings()
        {
            DateTime weekStart = GetStartOfWeek(SelectedDate ?? DateTime.Today);

            return new ObservableCollection<CourtScheduleItem>
            {
                new CourtScheduleItem
                {
                    BookingId = "BK001",
                    CourtId = "S02",
                    CourtName = "Court 2",
                    SlotId = "CA09",
                    StartTime = new TimeSpan(14, 0, 0),
                    EndTime = new TimeSpan(15, 0, 0),
                    Date = weekStart,
                    Customer = "Nguyễn Văn A",
                    Phone = "0901234567",
                    Type = "Casual",
                    Status = "Pending",
                    Amount = 150000
                },
                new CourtScheduleItem
                {
                    BookingId = "BK002",
                    CourtId = "S04",
                    CourtName = "Court 4",
                    SlotId = "CA10",
                    StartTime = new TimeSpan(15, 0, 0),
                    EndTime = new TimeSpan(16, 0, 0),
                    Date = weekStart.AddDays(1),
                    Customer = "Trần Thị B",
                    Phone = "0912345678",
                    Type = "Casual",
                    Status = "Checked-in",
                    Amount = 150000
                }
            };
        }

        private static DateTime GetStartOfWeek(DateTime date)
        {
            int diff = (7 + ((int)date.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
            return date.Date.AddDays(-diff);
        }

        private static string GetCompactStatus(Court court, CourtScheduleItem booking)
        {
            if (booking != null)
            {
                // Cancelled should free the slot.
                if (booking.Status == "Cancelled") return "Available";
                
                // If the booking is checked-in, it's currently in-use.
                if (booking.Status == "Checked-in") return "InUse";

                return "Booked";
            }

            return court.Status == "Maintenance" ? "Maintenance" : "Available";
        }

        private static string GetCompactStatusText(string statusKey)
        {
            return statusKey switch
            {
                "InUse" => "In-use",
                "Booked" => "Booked",
                "Maintenance" => "Maintenance",
                "Partial" => "30p",
                _ => "Available"
            };
        }

        private static string BuildDetails(DateTime date, TimeSlot slot, Court court, CourtScheduleItem booking, string role)
        {
            if (booking == null)
            {
                if (court.Status == "Maintenance")
                    return $"[{court.Id} / {slot.Id}]\n{court.Name}\n{date:dd/MM/yyyy} - {slot.DisplayLabel}\nTrạng thái: Bảo trì";

                return $"[{court.Id} / {slot.Id}]\n{court.Name}\n{date:dd/MM/yyyy} - {slot.DisplayLabel}\nTrạng thái: Trống";
            }

            if (string.Equals(role, "KhachHang", StringComparison.OrdinalIgnoreCase))
            {
                return $"{court.Name} - {slot.DisplayLabel}\n{date:dd/MM/yyyy}\nTrạng thái: Đã có người đặt";
            }

            return string.Join(
                "\n",
                new[]
                {
                    $"{court.Name} - {slot.DisplayLabel}",
                    $"{date:dd/MM/yyyy}",
                    $"Khách: {booking.Customer ?? "Chưa có"}",
                    $"SĐT: {booking.Phone ?? "Không có"}",
                    $"Loại đặt: {(booking.Type == "Fixed" ? "Cố định" : "Lẻ")}",
                    $"Trạng thái: {booking.Status}"
                });
        }

        private static bool TryGetBookingStartDateTime(Booking booking, out DateTime start)
        {
            start = default;
            if (booking == null) return false;

            string time = (booking.Time ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(time)) return false;

            string startPart = time.Split('-').FirstOrDefault()?.Trim() ?? string.Empty;
            if (!TimeSpan.TryParse(startPart, out TimeSpan startTime)) return false;

            start = booking.Date.Date.Add(startTime);
            return true;
        }

        public bool CancelBooking(string bookingId, out string error)
        {
            var booking = _bookingBus.GetBookingById(bookingId);
            if (booking == null)
            {
                error = "Không tìm thấy thông tin đặt sân.";
                return false;
            }

            // RBAC Check: Users can only cancel their own bookings, unless they are Admin/Staff.
            bool isAdminOrStaff = string.Equals(UserRole, "Admin", StringComparison.OrdinalIgnoreCase) || 
                                  string.Equals(UserRole, "NhanVien", StringComparison.OrdinalIgnoreCase);

            if (!isAdminOrStaff)
            {
                if (string.IsNullOrWhiteSpace(CustomerId) || booking.CustomerId != CustomerId)
                {
                    error = "Không đủ quyền. Bạn không phải là chủ sở hữu của lịch đặt sân này.";
                    return false;
                }

                // Rule (doc): không được hủy sân trong vòng 24 giờ trước giờ chơi.
                // Admin/Nhân viên được phép override.
                if (TryGetBookingStartDateTime(booking, out DateTime start) && DateTime.Now > start.AddHours(-24))
                {
                    error = "Không thể hủy sân trong vòng 24 giờ trước giờ chơi.";
                    return false;
                }
            }

            bool ok = _bookingBus.UpdateBookingStatus(bookingId, "Cancelled", out error);
            if (ok)
            {
                ReloadFromDatabase();
            }
            return ok;
        }

        public bool CheckInBooking(string bookingId, out string error)
        {
            bool ok = _bookingBus.PerformCheckIn(bookingId, out error);
            if (ok)
            {
                ReloadFromDatabase();
            }
            return ok;
        }
    }

    public class WeekDayViewModel
    {
        public DateTime Date { get; set; }
        public string DayName { get; set; }
        public string DayNumber { get; set; }
        public bool IsToday { get; set; }
        public bool IsSelectedDate { get; set; }
    }

    public class ScheduleRowViewModel
    {
        public string TimeLabel { get; set; }
        public ObservableCollection<ScheduleCellViewModel> Cells { get; set; } = new ObservableCollection<ScheduleCellViewModel>();
    }

    public class ScheduleCellViewModel
    {
        public string CourtId { get; set; }
        public string CourtName { get; set; }
        public string SlotId { get; set; }
        public string SlotName { get; set; }
        public string StatusKey { get; set; }
        public string StatusText { get; set; }
        public string BookingId { get; set; }
        public string BookingStatus { get; set; }
        public string Details { get; set; }
    }
}
