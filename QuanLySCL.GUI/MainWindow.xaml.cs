using QuanLySCL.GUI.Views;
using System;
using System.Windows;
using System.Windows.Controls;
using QuanLySCL.GUI.Security;
using QuanLySCL.GUI.Windows;

namespace QuanLySCL.GUI
{
    public partial class MainWindow : Window
    {
        public string CurrentUsername { get; set; }
        public string CurrentRole { get; set; }
        public string CustomerId { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(string username, string role, string customerId = null) : this()
        {
            CurrentUsername = username;
            CurrentRole = role;
            CustomerId = customerId;
            UpdateUserInfo();
            ApplyRoleBasedUI();
            NavigateDefaultForRole();
        }

        private void UpdateUserInfo()
        {
            var txtName = FindName("txtUserName") as TextBlock;
            var txtRole = FindName("txtUserRole") as TextBlock;
            var txtInitials = FindName("txtInitials") as TextBlock;

            if (txtName != null) txtName.Text = !string.IsNullOrEmpty(CurrentUsername) ? CurrentUsername : "User";

            if (txtRole != null)
            {
                txtRole.Text = CurrentRole switch
                {
                    "Admin" => "Quản trị viên",
                    "NhanVien" => "Nhân viên",
                    "KhachHang" => "Khách hàng",
                    _ => "User"
                };
            }

            if (txtInitials != null)
            {
                string name = !string.IsNullOrEmpty(CurrentUsername) ? CurrentUsername : "US";
                txtInitials.Text = name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();
            }
        }

        private void ApplyRoleBasedUI()
        {
            bool isAdmin = string.Equals(CurrentRole, "Admin", StringComparison.OrdinalIgnoreCase);
            bool isStaff = string.Equals(CurrentRole, "NhanVien", StringComparison.OrdinalIgnoreCase);
            bool isCustomer = string.Equals(CurrentRole, "KhachHang", StringComparison.OrdinalIgnoreCase);
            
            if (AdminSectionTitle != null) AdminSectionTitle.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (AdminSection != null) AdminSection.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (AdminPanelNav != null) AdminPanelNav.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            if (isStaff)
            {
                if (FindName("StaffNav") is RadioButton staff) staff.Visibility = Visibility.Collapsed;
                if (FindName("ReportsNav") is RadioButton reports) reports.Visibility = Visibility.Collapsed;
            }

            if (isCustomer)
            {
                if (FindName("ServicesNav") is RadioButton services) services.Visibility = Visibility.Collapsed;
                if (FindName("CustomersNav") is RadioButton customers) customers.Visibility = Visibility.Collapsed;
                if (FindName("StaffNav") is RadioButton staff) staff.Visibility = Visibility.Collapsed;
                if (FindName("ReportsNav") is RadioButton reports) reports.Visibility = Visibility.Collapsed;
            }
        }

        private void NavigateDefaultForRole()
        {
            if (MainFrame == null) return;

            if (string.Equals(CurrentRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                if (AdminPanelNav != null) AdminPanelNav.IsChecked = true;
                MainFrame.Navigate(new AdminPanelView());
                return;
            }

            if (string.Equals(CurrentRole, "KhachHang", StringComparison.OrdinalIgnoreCase))
            {
                if (FindName("BookingNav") is RadioButton bookingNav) bookingNav.IsChecked = true;
                MainFrame.Navigate(new BookingView(CurrentRole, CustomerId));
                return;
            }

            if (FindName("DashboardNav") is RadioButton dashNav) dashNav.IsChecked = true;
            MainFrame.Navigate(new DashboardView(CurrentRole, CustomerId));
        }

        private void AdminToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (AdminSubMenu == null || AdminPanelNav == null) return;
            
            bool isExpanded = AdminPanelNav.IsChecked == true;
            AdminSubMenu.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NavigationButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton button || MainFrame == null) return;
            string tag = button.Tag?.ToString();

            // Note: We don't collapse AdminSubMenu here anymore, let the user toggle it manually
            // unless we want to AUTO-collapse when clicking another main item. 
            // In most dash apps, it stays open unless manually collapsed.

            if (!RolePolicy.CanNavigate(CurrentRole, tag))
            {
                MessageBox.Show("Bạn không có quyền truy cập chức năng này.", "Không đủ quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigateDefaultForRole();
                return;
            }

            switch (tag)
            {
                case "Dashboard":
                    MainFrame.Navigate(new DashboardView(CurrentRole, CustomerId));
                    break;
                case "Booking":
                    MainFrame.Navigate(new BookingView(CurrentRole, CustomerId));
                    break;
                case "Services":
                    MainFrame.Navigate(new ServicesView());
                    break;
                case "Customers":
                    MainFrame.Navigate(new CustomersView());
                    break;
                case "Staff":
                    MainFrame.Navigate(new StaffView());
                    break;
                case "Reports":
                    MainFrame.Navigate(new ReportsView());
                    break;
                
                // Admin Sub-Items
                case "AdminAccounts":
                    MainFrame.Navigate(new AdminPanelView());
                    break;
                case "AdminCourts":
                    MainFrame.Navigate(new CourtsAdminView());
                    break;
                case "AdminTimeSlots":
                    MainFrame.Navigate(new TimeSlotsAdminView());
                    break;
                case "AdminPricing":
                    MainFrame.Navigate(new PricingAdminView());
                    break;
                case "AdminPromotions":
                    MainFrame.Navigate(new PromotionsAdminView());
                    break;
            }
        }
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                // Sử dụng đầy đủ namespace để tránh nhầm lẫn với System.Windows
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                Hide();

                var loginWin = new QuanLySCL.GUI.Windows.LoginWindow { Owner = this };
                bool? loginResult = loginWin.ShowDialog();

                if (loginResult == true)
                {
                    var newMain = new MainWindow(loginWin.Username, loginWin.Role, loginWin.CustomerId);
                    Application.Current.MainWindow = newMain;
                    Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    newMain.Show();
                    Close();
                    return;
                }

                if (Application.Current != null)
                {
                    Application.Current.Shutdown();
                }
                else
                {
                    // Fallback if Application.Current is already null
                    Environment.Exit(0);
                }
            }
        }
    }
}
