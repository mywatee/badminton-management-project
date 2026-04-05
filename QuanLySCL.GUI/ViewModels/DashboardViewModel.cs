using QuanLySCL.BUS;
using QuanLySCL.DAL;
using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace QuanLySCL.GUI.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private const int RecentBookingLimit = 8;
        private const int RecentBookingDays = 7;

        private ObservableCollection<Court> _courts = new ObservableCollection<Court>();
        public ObservableCollection<Court> Courts
        {
            get => _courts;
            set => SetProperty(ref _courts, value);
        }

        private ObservableCollection<Booking> _recentBookings = new ObservableCollection<Booking>();
        public ObservableCollection<Booking> RecentBookings
        {
            get => _recentBookings;
            set => SetProperty(ref _recentBookings, value);
        }

        private decimal _dailyRevenue;
        public decimal DailyRevenue
        {
            get => _dailyRevenue;
            set => SetProperty(ref _dailyRevenue, value);
        }

        private int _activeBookings;
        public int ActiveBookings
        {
            get => _activeBookings;
            set => SetProperty(ref _activeBookings, value);
        }

        private int _totalCustomers;
        public int TotalCustomers
        {
            get => _totalCustomers;
            set => SetProperty(ref _totalCustomers, value);
        }

        private decimal _monthlyGrowth;
        public decimal MonthlyGrowth
        {
            get => _monthlyGrowth;
            set => SetProperty(ref _monthlyGrowth, value);
        }

        private readonly CourtBUS _courtBus = new CourtBUS();
        private readonly BookingBUS _bookingBus = new BookingBUS();
        private readonly CustomerBUS _customerBus = new CustomerBUS();

        private DispatcherTimer _autoCompleteTimer = null!;

        private string _userRole = "KhachHang";
        public string UserRole
        {
            get => _userRole;
            set
            {
                if (SetProperty(ref _userRole, value))
                {
                    OnPropertyChanged(nameof(IsAdminOrStaff));
                    OnPropertyChanged(nameof(IsAdmin));
                    LoadData();
                }
            }
        }

        public bool IsAdminOrStaff =>
            string.Equals(UserRole, "Admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(UserRole, "NhanVien", StringComparison.OrdinalIgnoreCase);

        public bool IsAdmin =>
            string.Equals(UserRole, "Admin", StringComparison.OrdinalIgnoreCase);

        private ObservableCollection<Service> _lowStockServices = new ObservableCollection<Service>();
        public ObservableCollection<Service> LowStockServices
        {
            get => _lowStockServices;
            set => SetProperty(ref _lowStockServices, value);
        }

        private ObservableCollection<Booking> _todayBookings = new ObservableCollection<Booking>();
        private ObservableCollection<Booking> _allTodayBookings = new ObservableCollection<Booking>();
        public ObservableCollection<Booking> TodayBookings
        {
            get => _todayBookings;
            set => SetProperty(ref _todayBookings, value);
        }

        private ObservableCollection<Booking> _quickSearchMatches = new ObservableCollection<Booking>();
        public ObservableCollection<Booking> QuickSearchMatches
        {
            get => _quickSearchMatches;
            set => SetProperty(ref _quickSearchMatches, value);
        }

        private Booking? _selectedQuickSearchMatch;
        public Booking? SelectedQuickSearchMatch
        {
            get => _selectedQuickSearchMatch;
            set => SetProperty(ref _selectedQuickSearchMatch, value);
        }

        private bool _isQuickSearchPopupOpen;
        public bool IsQuickSearchPopupOpen
        {
            get => _isQuickSearchPopupOpen;
            set => SetProperty(ref _isQuickSearchPopupOpen, value);
        }

        private string _customerId;
        public string CustomerId
        {
            get => _customerId;
            set
            {
                if (SetProperty(ref _customerId, value))
                {
                    LoadData();
                }
            }
        }

        private string _quickSearchText = string.Empty;
        public string QuickSearchText
        {
            get => _quickSearchText;
            set
            {
                if (SetProperty(ref _quickSearchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(QuickSearchText))
            {
                // Restore full list from the cached today bookings (avoid re-query on every clear).
                TodayBookings = new ObservableCollection<Booking>(_allTodayBookings);
                QuickSearchMatches = new ObservableCollection<Booking>();
                SelectedQuickSearchMatch = null;
                IsQuickSearchPopupOpen = false;
                return;
            }

            string q = QuickSearchText.ToLower();
            var filtered = _allTodayBookings
                .Where(b =>
                    (b.Customer ?? string.Empty).ToLower().Contains(q) ||
                    (!string.IsNullOrWhiteSpace(b.Phone) && b.Phone.Contains(q)) ||
                    (b.Court ?? string.Empty).ToLower().Contains(q))
                .ToList();

            TodayBookings = new ObservableCollection<Booking>(filtered);
            QuickSearchMatches = new ObservableCollection<Booking>(filtered);

            if (QuickSearchMatches.Count > 1)
            {
                SelectedQuickSearchMatch = QuickSearchMatches[0];
                IsQuickSearchPopupOpen = true;
            }
            else
            {
                SelectedQuickSearchMatch = QuickSearchMatches.Count == 1 ? QuickSearchMatches[0] : null;
                IsQuickSearchPopupOpen = false;
            }
        }

        private string _welcomeMessage = "Chào mừng trở lại! Tình hình hoạt động hôm nay.";
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        private string _userRank = "Mới";
        public string UserRank
        {
            get => _userRank;
            set => SetProperty(ref _userRank, value);
        }

        private int _rankProgress;
        public int RankProgress
        {
            get => _rankProgress;
            set
            {
                if (SetProperty(ref _rankProgress, value))
                {
                    OnPropertyChanged(nameof(RankPercentText));
                    OnPropertyChanged(nameof(RankTooltipText));
                }
            }
        }

        private string _nextRank = string.Empty;
        public string NextRank
        {
            get => _nextRank;
            set
            {
                if (SetProperty(ref _nextRank, value))
                    OnPropertyChanged(nameof(RankTooltipText));
            }
        }

        private bool _isRankPercentVisible;
        public bool IsRankPercentVisible
        {
            get => _isRankPercentVisible;
            set => SetProperty(ref _isRankPercentVisible, value);
        }

        public string RankPercentText => $"{Math.Max(0, Math.Min(100, RankProgress))}%";

        private string _rankBenefitText = string.Empty;
        public string RankBenefitText
        {
            get => _rankBenefitText;
            set
            {
                if (SetProperty(ref _rankBenefitText, value))
                    OnPropertyChanged(nameof(RankTooltipText));
            }
        }

        private string _rankRequirementText = string.Empty;
        public string RankRequirementText
        {
            get => _rankRequirementText;
            set
            {
                if (SetProperty(ref _rankRequirementText, value))
                    OnPropertyChanged(nameof(RankTooltipText));
            }
        }

        public string RankTooltipText =>
            string.Join(
                "\n",
                new[]
                {
                    $"Hạng hiện tại: {UserRank}",
                    $"Tiến độ: {RankPercentText}",
                    string.IsNullOrWhiteSpace(NextRank) ? null : $"Mục tiêu: {NextRank}",
                    string.IsNullOrWhiteSpace(RankBenefitText) ? null : RankBenefitText,
                    string.IsNullOrWhiteSpace(RankRequirementText) ? null : RankRequirementText,
                    $"Lượt đặt: {TotalPersonalBookings}",
                    $"Tổng chi tiêu: {TotalPersonalSpent:N0}₫"
                }.Where(x => !string.IsNullOrWhiteSpace(x)));

        public void ToggleRankPercent() => IsRankPercentVisible = !IsRankPercentVisible;

        private void SetRankNotes(string rankEn)
        {
            rankEn = (rankEn ?? string.Empty).Trim();

            RankBenefitText = rankEn switch
            {
                "VIP" => "Ưu đãi: Giảm 15% tổng thanh toán.",
                "Gold" => "Ưu đãi: Giảm 10% tổng thanh toán.",
                "Silver" => "Ưu đãi: Giảm 5% tổng thanh toán.",
                _ => "Ưu đãi: Chưa áp dụng giảm giá."
            };

            RankRequirementText = rankEn switch
            {
                "VIP" => "Yêu cầu: Đã đạt hạng cao nhất.",
                "Gold" => "Yêu cầu lên VIP: >= 100 lượt đặt hoặc >= 10.000.000₫.",
                "Silver" => "Yêu cầu lên Vàng: >= 50 lượt đặt hoặc >= 5.000.000₫.",
                _ => "Yêu cầu lên Bạc: >= 30 lượt đặt."
            };
        }

        private int _totalPersonalBookings;
        public int TotalPersonalBookings
        {
            get => _totalPersonalBookings;
            set => SetProperty(ref _totalPersonalBookings, value);
        }

        private decimal _totalPersonalSpent;
        public decimal TotalPersonalSpent
        {
            get => _totalPersonalSpent;
            set => SetProperty(ref _totalPersonalSpent, value);
        }

        private ObservableCollection<Booking> _personalBookings = new ObservableCollection<Booking>();
        public ObservableCollection<Booking> PersonalBookings
        {
            get => _personalBookings;
            set => SetProperty(ref _personalBookings, value);
        }

        private readonly ServiceDAL _serviceDal = new ServiceDAL();

        public DashboardViewModel(string role = "KhachHang", string customerId = null)
        {
            UserRole = role ?? "KhachHang";
            CustomerId = customerId;
            LoadData();

            _autoCompleteTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _autoCompleteTimer.Tick += AutoCompleteTimer_Tick;
            _autoCompleteTimer.Start();
        }

        private void AutoCompleteTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                _bookingBus.AutoCompleteOverdueBookings();
                LoadData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Timer Tick Error: " + ex.Message);
            }
        }

        public void LoadData()
        {
            try
            {
                Courts = _courtBus.GetAllCourts();

                if (IsAdminOrStaff)
                {
                    LoadAdminStats();
                }
                else
                {
                    LoadPersonalData();
                }

                // Highlight my courts (common logic)
                if (!string.IsNullOrEmpty(CustomerId) && Courts != null)
                {
                    var myTodayBookings = TodayBookings?.Where(b => b.CustomerId == CustomerId && b.Status == "Checked-in").ToList();
                    foreach (var court in Courts)
                    {
                        court.IsMyBooking = myTodayBookings?.Any(b => b.CourtId == court.Id) ?? false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu Dashboard: " + ex.Message);

                Courts = new ObservableCollection<Court>();
                RecentBookings = new ObservableCollection<Booking>();
                LowStockServices = new ObservableCollection<Service>();
                TodayBookings = new ObservableCollection<Booking>();
                PersonalBookings = new ObservableCollection<Booking>();
            }
        }

        private void LoadAdminStats()
        {
            var allBookings = _bookingBus.GetAllBookings() ?? new ObservableCollection<Booking>();
            var minDate = DateTime.Today;

            // 1. Recent Bookings (Next/Recent 7 days)
            var filtered = allBookings
                .Where(b => b.Date >= minDate.AddDays(-1))
                .OrderByDescending(b => b.Date)
                .Take(RecentBookingLimit)
                .ToList();

            RecentBookings = new ObservableCollection<Booking>(filtered);

            // 2. Today's Bookings
            var todayList = allBookings
                .Where(b => b.Date.Date == minDate.Date && (b.Status == "Pending" || b.Status == "Checked-in"))
                .ToList();
            
            foreach (var b in todayList) b.IsMyBooking = (b.CustomerId == CustomerId);
            _allTodayBookings = new ObservableCollection<Booking>(todayList);
            TodayBookings = new ObservableCollection<Booking>(todayList);

            // Reset quick search state after refresh.
            if (string.IsNullOrWhiteSpace(QuickSearchText))
            {
                QuickSearchMatches = new ObservableCollection<Booking>();
                SelectedQuickSearchMatch = null;
                IsQuickSearchPopupOpen = false;
            }
            else
            {
                ApplyFilter();
            }

            foreach (var b in filtered) b.IsMyBooking = (b.CustomerId == CustomerId);
            RecentBookings = new ObservableCollection<Booking>(filtered);

            // 3. Stats
            var stats = _bookingBus.GetBookingStats();
            DailyRevenue = stats.dailyRevenue;
            ActiveBookings = stats.activeBookings;
            MonthlyGrowth = Math.Round(stats.monthlyGrowth, 1);

            var allCustomers = _customerBus.GetAllCustomers() ?? new ObservableCollection<Customer>();
            TotalCustomers = allCustomers.Count;

            // 4. Alerts
            LowStockServices = _serviceDal.GetLowStockServices(5);
        }

        private void LoadPersonalData()
        {
            if (string.IsNullOrEmpty(CustomerId))
            {
                WelcomeMessage = "Chưa nhận diện được mã khách hàng (KH001). Vui lòng thử đăng nhập lại.";
                return;
            }

            // 1. Load Personal Stats & Rank
            var customer = _customerBus.GetCustomerById(CustomerId);
            if (customer != null)
            {
                TotalPersonalBookings = customer.TotalBookings;
                TotalPersonalSpent = customer.TotalSpent;
                
                // Translate rank for UI
                string rankEn = _customerBus.GetCustomerRank(customer);
                UserRank = rankEn switch
                {
                    "VIP" => "VIP",
                    "Gold" => "Vàng",
                    "Silver" => "Bạc",
                    _ => "Thành viên"
                };

                // Rank progress for dashboard (thanh tiến độ mức hạng)
                UserRank = rankEn switch
                {
                    "VIP" => "VIP",
                    "Gold" => "Vàng",
                    "Silver" => "Bạc",
                    _ => "Thành viên"
                };

                RankProgress = Math.Max(0, Math.Min(100, customer.RankProgress));
                NextRank = customer.NextRank switch
                {
                    "VIP" => "VIP",
                    "Gold" => "Vàng",
                    "Silver" => "Bạc",
                    _ => string.Empty
                };

                SetRankNotes(rankEn);
            }

            // 2. Load Personal Booking History
            var personalList = _bookingBus.GetBookingsByCustomerId(CustomerId) ?? new ObservableCollection<Booking>();
            var filteredPersonal = personalList.OrderByDescending(b => b.Date).Take(10).ToList();
            foreach (var b in filteredPersonal) b.IsMyBooking = true;
            
            PersonalBookings = new ObservableCollection<Booking>(filteredPersonal);
        }

        public void RefreshData() => LoadData();
    }
}
