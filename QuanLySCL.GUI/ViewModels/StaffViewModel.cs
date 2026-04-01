using QuanLySCL.BUS;
using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace QuanLySCL.GUI.ViewModels
{
    public class StaffViewModel : BaseViewModel
    {
        private readonly StaffBUS _staffBus = new StaffBUS();
        private readonly TaiKhoanBUS _taiKhoanBus = new TaiKhoanBUS();

        private ObservableCollection<Staff> _allStaff = new ObservableCollection<Staff>();
        private ObservableCollection<Account> _allAccounts = new ObservableCollection<Account>();

        public ObservableCollection<StaffRowViewModel> StaffMembers { get; set; } = new ObservableCollection<StaffRowViewModel>();

        private StaffRowViewModel _selectedStaff;
        public StaffRowViewModel SelectedStaff
        {
            get => _selectedStaff;
            set => SetProperty(ref _selectedStaff, value);
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    ApplyFilter();
            }
        }

        private int _totalStaffCount;
        public int TotalStaffCount
        {
            get => _totalStaffCount;
            set => SetProperty(ref _totalStaffCount, value);
        }

        private int _activeStaffCount;
        public int ActiveStaffCount
        {
            get => _activeStaffCount;
            set => SetProperty(ref _activeStaffCount, value);
        }

        private int _staffAccountsCount;
        public int StaffAccountsCount
        {
            get => _staffAccountsCount;
            set => SetProperty(ref _staffAccountsCount, value);
        }

        private int _activeStaffAccountsCount;
        public int ActiveStaffAccountsCount
        {
            get => _activeStaffAccountsCount;
            set => SetProperty(ref _activeStaffAccountsCount, value);
        }

        private int _staffWithoutAccountCount;
        public int StaffWithoutAccountCount
        {
            get => _staffWithoutAccountCount;
            set => SetProperty(ref _staffWithoutAccountCount, value);
        }

        public StaffViewModel()
        {
            Load();
        }

        public void Load()
        {
            try
            {
                _allStaff = _staffBus.GetAllStaff() ?? new ObservableCollection<Staff>();
            }
            catch
            {
                _allStaff = new ObservableCollection<Staff>();
            }

            try
            {
                _allAccounts = _taiKhoanBus.GetAllAccounts() ?? new ObservableCollection<Account>();
            }
            catch
            {
                _allAccounts = new ObservableCollection<Account>();
            }

            ApplyFilter();
            RefreshStats();
        }

        private void ApplyFilter()
        {
            var rows = _allStaff
                .Select(s =>
                {
                    var acc = _allAccounts.FirstOrDefault(a => !string.IsNullOrWhiteSpace(a.StaffId) &&
                                                              string.Equals(a.StaffId.Trim(), s.Id?.Trim(), StringComparison.OrdinalIgnoreCase));
                    return new StaffRowViewModel(s, acc);
                })
                .AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string keyword = SearchText.Trim();
                rows = rows.Where(r =>
                    (r.Staff.Id?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Staff.Name?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Staff.Phone?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Staff.Role?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.AccountUsername?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            StaffMembers = new ObservableCollection<StaffRowViewModel>(rows.OrderBy(r => r.Staff.Name));
            OnPropertyChanged(nameof(StaffMembers));
        }

        private void RefreshStats()
        {
            TotalStaffCount = _allStaff.Count;
            ActiveStaffCount = _allStaff.Count(s => string.Equals(s.Status, "Active", StringComparison.OrdinalIgnoreCase));

            try
            {
                StaffAccountsCount = _allAccounts.Count(a => !string.IsNullOrWhiteSpace(a.StaffId));
                ActiveStaffAccountsCount = _allAccounts.Count(a => !string.IsNullOrWhiteSpace(a.StaffId) && a.IsActive);
            }
            catch
            {
                StaffAccountsCount = 0;
                ActiveStaffAccountsCount = 0;
            }

            StaffWithoutAccountCount = Math.Max(0, TotalStaffCount - StaffAccountsCount);
        }

        public class StaffRowViewModel
        {
            public Staff Staff { get; }
            public Account Account { get; }

            public string AccountUsername => Account?.Username ?? string.Empty;
            public string AccountRole => Account?.Role ?? string.Empty;
            public string AccountActiveText => Account == null ? string.Empty : (Account.IsActive ? "Yes" : "No");

            public StaffRowViewModel(Staff staff, Account account)
            {
                Staff = staff;
                Account = account;
            }
        }
    }
}
