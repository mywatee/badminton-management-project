using QuanLySCL.BUS;
using QuanLySCL.Models;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Windows.Input;

namespace QuanLySCL.GUI.ViewModels
{
    public class AdminPanelViewModel : BaseViewModel
    {
        private readonly TaiKhoanBUS _taiKhoanBus = new TaiKhoanBUS();
        private readonly StaffBUS _staffBus = new StaffBUS();
        private readonly CustomerBUS _customerBus = new CustomerBUS();

        private ObservableCollection<Account> _allAccounts = new ObservableCollection<Account>();


        public ObservableCollection<AccountRowViewModel> Accounts { get; set; } = new ObservableCollection<AccountRowViewModel>();

        private int _visibleCount = 15;
        public int VisibleCount
        {
            get => _visibleCount;
            set => SetProperty(ref _visibleCount, value);
        }

        private bool _isSeeMoreVisible;
        public bool IsSeeMoreVisible
        {
            get => _isSeeMoreVisible;
            set => SetProperty(ref _isSeeMoreVisible, value);
        }

        public ICommand SeeMoreCommand { get; }

        private AccountRowViewModel _selectedAccount;
        public AccountRowViewModel SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                if (SetProperty(ref _selectedAccount, value))
                    LoadSelectedAccountDetails();
            }
        }

        public ObservableCollection<string> RoleFilters { get; } =
            new ObservableCollection<string> { "All", "Admin", "NhanVien", "KhachHang" };

        private string _selectedRoleFilter = "All";
        public string SelectedRoleFilter
        {
            get => _selectedRoleFilter;
            set
            {
                if (SetProperty(ref _selectedRoleFilter, value))
                {
                    VisibleCount = 15;
                    ApplyFilter();
                }
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    VisibleCount = 15;
                    ApplyFilter();
                }
            }
        }

        public AdminPanelViewModel()
        {
            SeeMoreCommand = new RelayCommand(_ =>
            {
                VisibleCount += 15;
                ApplyFilter();
            });
            Load();
        }

        private bool _hasSelectedAccount;
        public bool HasSelectedAccount
        {
            get => _hasSelectedAccount;
            private set => SetProperty(ref _hasSelectedAccount, value);
        }

        private string _detailEmptyHint = "Chọn 1 tài khoản trong danh sách để xem chi tiết.";
        public string DetailEmptyHint
        {
            get => _detailEmptyHint;
            set => SetProperty(ref _detailEmptyHint, value);
        }

        private string _detailUsername = string.Empty;
        public string DetailUsername
        {
            get => _detailUsername;
            set => SetProperty(ref _detailUsername, value);
        }

        private string _detailRole = string.Empty;
        public string DetailRole
        {
            get => _detailRole;
            set => SetProperty(ref _detailRole, value);
        }

        private string _detailActive = string.Empty;
        public string DetailActive
        {
            get => _detailActive;
            set => SetProperty(ref _detailActive, value);
        }

        private string _detailLinkedId = string.Empty;
        public string DetailLinkedId
        {
            get => _detailLinkedId;
            set => SetProperty(ref _detailLinkedId, value);
        }

        private string _detailOwnerType = string.Empty;
        public string DetailOwnerType
        {
            get => _detailOwnerType;
            set => SetProperty(ref _detailOwnerType, value);
        }

        private string _detailFullName = string.Empty;
        public string DetailFullName
        {
            get => _detailFullName;
            set => SetProperty(ref _detailFullName, value);
        }

        private string _detailPhone = string.Empty;
        public string DetailPhone
        {
            get => _detailPhone;
            set => SetProperty(ref _detailPhone, value);
        }

        private string _detailEmail = string.Empty;
        public string DetailEmail
        {
            get => _detailEmail;
            set => SetProperty(ref _detailEmail, value);
        }

        private string _detailPosition = string.Empty;
        public string DetailPosition
        {
            get => _detailPosition;
            set => SetProperty(ref _detailPosition, value);
        }

        public void Load()
        {
            string keepUsername = SelectedAccount?.AccountUsername;
            try
            {
                _allAccounts = _taiKhoanBus.GetAllAccounts() ?? new ObservableCollection<Account>();
            }
            catch
            {
                _allAccounts = new ObservableCollection<Account>();
            }

            ApplyFilter();

            if (!string.IsNullOrWhiteSpace(keepUsername))
            {
                var match = Accounts.FirstOrDefault(a => string.Equals(a.AccountUsername, keepUsername, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    SelectedAccount = match;
            }
        }

        private void ApplyFilter()
        {
            var rows = _allAccounts
                .Select(a => new AccountRowViewModel(a))
                .AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SelectedRoleFilter) && !string.Equals(SelectedRoleFilter, "All", StringComparison.OrdinalIgnoreCase))
                rows = rows.Where(r => string.Equals(r.AccountRole, SelectedRoleFilter, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string keyword = SearchText.Trim();
                rows = rows.Where(r =>
                    (r.AccountUsername?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.AccountRole?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.DisplayId?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            var sorted = rows.OrderBy(r => r.AccountRole).ThenBy(r => r.AccountUsername).ToList();

            IsSeeMoreVisible = sorted.Count > VisibleCount;

            Accounts = new ObservableCollection<AccountRowViewModel>(sorted.Take(VisibleCount));
            OnPropertyChanged(nameof(Accounts));

            if (_selectedAccount != null)
            {
                string keepUsername = _selectedAccount.AccountUsername;
                var match = Accounts.FirstOrDefault(a => string.Equals(a.AccountUsername, keepUsername, StringComparison.OrdinalIgnoreCase));
                SelectedAccount = match;
            }
        }

        private void LoadSelectedAccountDetails()
        {
            var account = SelectedAccount?.Account;
            HasSelectedAccount = account != null;

            if (account == null)
            {
                DetailUsername = string.Empty;
                DetailRole = string.Empty;
                DetailActive = string.Empty;
                DetailLinkedId = string.Empty;
                DetailOwnerType = string.Empty;
                DetailFullName = string.Empty;
                DetailPhone = string.Empty;
                DetailEmail = string.Empty;
                DetailPosition = string.Empty;
                return;
            }

            DetailUsername = account.Username ?? string.Empty;
            DetailRole = account.Role ?? string.Empty;
            DetailActive = account.IsActive ? "Yes" : "No";
            DetailLinkedId = !string.IsNullOrWhiteSpace(account.StaffId) ? account.StaffId : (account.CustomerId ?? string.Empty);
            DetailOwnerType = !string.IsNullOrWhiteSpace(account.StaffId) ? "Nhân viên" : (!string.IsNullOrWhiteSpace(account.CustomerId) ? "Khách hàng" : string.Empty);

            DetailFullName = string.Empty;
            DetailPhone = string.Empty;
            DetailEmail = string.Empty;
            DetailPosition = string.Empty;

            try
            {
                if (!string.IsNullOrWhiteSpace(account.StaffId))
                {
                    var staff = _staffBus.GetStaffById(account.StaffId);
                    if (staff != null)
                    {
                        DetailFullName = staff.Name ?? string.Empty;
                        DetailPhone = staff.Phone ?? string.Empty;
                        DetailPosition = staff.Role ?? string.Empty;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(account.CustomerId))
                {
                    var customer = _customerBus.GetCustomerById(account.CustomerId);
                    if (customer != null)
                    {
                        DetailFullName = customer.Name ?? string.Empty;
                        DetailPhone = customer.Phone ?? string.Empty;
                        DetailEmail = customer.Email ?? string.Empty;
                    }
                }
            }
            catch
            {
                // Keep UI stable. Details are optional.
            }
        }

        public class AccountRowViewModel
        {
            public Account Account { get; }

            public string AccountUsername => Account?.Username ?? string.Empty;
            public string AccountRole => Account?.Role ?? string.Empty;
            public string ActiveText => Account == null ? string.Empty : (Account.IsActive ? "Yes" : "No");
            public string DisplayId => !string.IsNullOrWhiteSpace(Account?.StaffId) ? Account.StaffId : (Account?.CustomerId ?? string.Empty);

            public AccountRowViewModel(Account account)
            {
                Account = account;
            }
        }
    }
}
