using QuanLySCL.BUS;
using QuanLySCL.Models;
using QuanLySCL.GUI.Services;
using QuanLySCL.GUI.ViewModels;
using QuanLySCL.GUI.Views;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace QuanLySCL.GUI.ViewModels
{
    public class ServicesViewModel : BaseViewModel
    {
        private readonly ServiceBUS _serviceBus = new ServiceBUS();
        private readonly CustomerBUS _customerBus = new CustomerBUS();
        private readonly BookingBUS _bookingBus = new BookingBUS();

        private ObservableCollection<Service> _allServices = new ObservableCollection<Service>();
        private ObservableCollection<ServiceSaleInvoice> _allServiceSales = new ObservableCollection<ServiceSaleInvoice>();

        public ObservableCollection<Service> Services { get; private set; } = new ObservableCollection<Service>();
        public ObservableCollection<CartItem> CartItems { get; } = new ObservableCollection<CartItem>();
        public ObservableCollection<Customer> Customers { get; } = new ObservableCollection<Customer>();
        public ObservableCollection<Booking> ActiveBookings { get; } = new ObservableCollection<Booking>();

        public ObservableCollection<ServiceSaleInvoice> ServiceSales { get; private set; } = new ObservableCollection<ServiceSaleInvoice>();
        private int _currentSalesPage = 1;
        private const int SalesPageSize = 20;
        private bool _hasMoreSales = true;
        public bool HasMoreSales
        {
            get => _hasMoreSales;
            set => SetProperty(ref _hasMoreSales, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    ApplyFilter();
            }
        }

        private bool _isAddToBookingMode;
        public bool IsAddToBookingMode
        {
            get => _isAddToBookingMode;
            set
            {
                if (SetProperty(ref _isAddToBookingMode, value))
                {
                    OnPropertyChanged(nameof(CheckoutButtonText));
                    if (value) LoadActiveBookings();
                }
            }
        }

        private Booking? _selectedActiveBooking;
        public Booking? SelectedActiveBooking
        {
            get => _selectedActiveBooking;
            set => SetProperty(ref _selectedActiveBooking, value);
        }

        public string CheckoutButtonText => IsAddToBookingMode ? "Ghi nợ vào sân" : "Tiến hành thanh toán";

        private DateTime? _salesFromDate = DateTime.Today;
        public DateTime? SalesFromDate
        {
            get => _salesFromDate;
            set
            {
                if (SetProperty(ref _salesFromDate, value))
                    LoadServiceSales();
            }
        }

        private DateTime? _salesToDate = DateTime.Today;
        public DateTime? SalesToDate
        {
            get => _salesToDate;
            set
            {
                if (SetProperty(ref _salesToDate, value))
                    LoadServiceSales();
            }
        }

        private Customer? _salesSelectedCustomer;
        public Customer? SalesSelectedCustomer
        {
            get => _salesSelectedCustomer;
            set
            {
                if (SetProperty(ref _salesSelectedCustomer, value))
                    LoadServiceSales();
            }
        }

        private string _salesSearchText = string.Empty;
        public string SalesSearchText
        {
            get => _salesSearchText;
            set
            {
                if (SetProperty(ref _salesSearchText, value))
                    ApplySalesFilter();
            }
        }

        public decimal CartSubtotal => CartItems.Sum(i => i?.Total ?? 0);

        private string _promoCode = string.Empty;
        public string PromoCode
        {
            get => _promoCode;
            set => SetProperty(ref _promoCode, value);
        }

        private decimal _discountAmount;
        public decimal DiscountAmount
        {
            get => _discountAmount;
            private set
            {
                if (SetProperty(ref _discountAmount, value))
                    OnPropertyChanged(nameof(TotalPayable));
            }
        }

        public decimal TotalPayable => Math.Max(0, CartSubtotal - DiscountAmount);

        private string? _appliedPromotionId;
        public string AppliedPromotionId
        {
            get => _appliedPromotionId;
            private set => SetProperty(ref _appliedPromotionId, value);
        }

        public ObservableCollection<string> PaymentMethods { get; } =
            new ObservableCollection<string> { "Tiền mặt", "Chuyển khoản" };

        private string _selectedPaymentMethod = "Tiền mặt";
        public string SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set => SetProperty(ref _selectedPaymentMethod, value);
        }

        public RelayCommand<Service> AddToCartCommand { get; }
        public RelayCommand<CartItem> RemoveFromCartCommand { get; }
        public RelayCommand<CartItem> IncreaseQuantityCommand { get; }
        public RelayCommand<CartItem> DecreaseQuantityCommand { get; }
        public RelayCommand CheckoutCommand { get; }
        public RelayCommand ApplyPromoCommand { get; }
        public RelayCommand ReloadSalesCommand { get; }
        public RelayCommand LoadMoreSalesCommand { get; }
        public RelayCommand RefreshActiveBookingsCommand { get; }

        private Customer? _selectedCustomer;
        public Customer? SelectedCustomer
        {
            get => _selectedCustomer;
            set => SetProperty(ref _selectedCustomer, value);
        }

        public ObservableCollection<string> CategoryOptions { get; } =
            new ObservableCollection<string> { "All", "Drinks", "Equipment" };

        private string _selectedCategory = "All";
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                    ApplyFilter();
            }
        }

        private Service? _selectedService;
        public Service? SelectedService
        {
            get => _selectedService;
            set => SetProperty(ref _selectedService, value);
        }

        public ServicesViewModel()
        {
            AddToCartCommand = new RelayCommand<Service>(ExecuteAddToCart);
            RemoveFromCartCommand = new RelayCommand<CartItem>(ExecuteRemoveFromCart);
            IncreaseQuantityCommand = new RelayCommand<CartItem>(ExecuteIncreaseQuantity);
            DecreaseQuantityCommand = new RelayCommand<CartItem>(ExecuteDecreaseQuantity);
            CheckoutCommand = new RelayCommand(ExecuteCheckout, () => CartItems.Count > 0);
            ApplyPromoCommand = new RelayCommand(ExecuteApplyPromo);
            ReloadSalesCommand = new RelayCommand(() => LoadServiceSales(reset: true));
            LoadMoreSalesCommand = new RelayCommand(() => LoadServiceSales(reset: false), () => HasMoreSales);
            RefreshActiveBookingsCommand = new RelayCommand(LoadActiveBookings);

            CartItems.CollectionChanged += CartItems_CollectionChanged;

            Load();
            LoadServiceSales(reset: true);
            LoadActiveBookings();
        }

        private void CartItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var it in e.OldItems.OfType<CartItem>())
                    it.PropertyChanged -= CartItem_PropertyChanged;
            }
            if (e.NewItems != null)
            {
                foreach (var it in e.NewItems.OfType<CartItem>())
                    it.PropertyChanged += CartItem_PropertyChanged;
            }

            RecalculateTotals();
        }

        private void CartItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CartItem.Quantity) || e.PropertyName == nameof(CartItem.Total))
                RecalculateTotals();
        }

        public void RecalculateTotals()
        {
            decimal subtotal = CartSubtotal;
            if (DiscountAmount > subtotal)
                DiscountAmount = subtotal;

            OnPropertyChanged(nameof(CartSubtotal));
            OnPropertyChanged(nameof(TotalPayable));
            CheckoutCommand.RaiseCanExecuteChanged();
        }

        private void ExecuteAddToCart(Service? service)
        {
            if (service == null) return;

            var existing = CartItems.FirstOrDefault(i => i.ServiceId == service.Id);
            if (existing != null)
                existing.Quantity++;
            else
                CartItems.Add(new CartItem 
                { 
                    ServiceId = service.Id, 
                    ServiceName = service.Name,
                    Price = service.Price,
                    Unit = service.Unit,
                    Quantity = 1 
                });
        }

        private void ExecuteRemoveFromCart(CartItem? item)
        {
            if (item == null) return;
            CartItems.Remove(item);
        }

        private void ExecuteIncreaseQuantity(CartItem? item)
        {
            if (item == null) return;
            item.Quantity++;
        }

        private void ExecuteDecreaseQuantity(CartItem? item)
        {
            if (item == null) return;
            if (item.Quantity <= 1) return;
            item.Quantity--;
        }

        private void ExecuteCheckout()
        {
            if (CartItems.Count == 0) return;

            if (IsAddToBookingMode)
            {
                if (SelectedActiveBooking == null)
                {
                    MessageBox.Show("Vui lòng chọn sân đang sử dụng để ghi nợ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Add to Booking Tab mode
                int successCount = 0;
                foreach (var item in CartItems)
                {
                    var detail = new BookingServiceDetail
                    {
                        Id = "DV" + SelectedActiveBooking.Id.Substring(Math.Max(0, SelectedActiveBooking.Id.Length - 10)) + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper(),
                        BookingId = SelectedActiveBooking.Id,
                        ServiceId = item.ServiceId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price
                    };

                    if (_serviceBus.AddServiceToBooking(detail, out string? err))
                    {
                        // Update stock if not equipment (simple assumption for now, DAL handles it better normally)
                        // But here we call BUS which calls DAL. ServiceDAL.AddServiceToBooking DOES NOT update stock.
                        // We should probably call UpdateStock too.
                        _serviceBus.UpdateStock(item.ServiceId, -item.Quantity);
                        successCount++;
                    }
                }

                MessageBox.Show($"Đã thêm {successCount}/{CartItems.Count} mặt hàng vào tab của {SelectedActiveBooking.Court}.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                CartItems.Clear();
                RecalculateTotals();
                Load(); // reload stock
            }
            else
            {
                // Standalone POS sale mode
                if (SelectedPaymentMethod == "Chuyển khoản")
                {
                    string tempTxnRef = "POS" + DateTime.Now.Ticks.ToString().Substring(10);
                    var qrWindow = new VietQRWindow(TotalPayable, $"Thanh toan dich vu {tempTxnRef}");
                    bool? result = qrWindow.ShowDialog();

                    if (result != true)
                    {
                        MessageBox.Show("Thanh toán VietQR không thành công hoặc đã bị hủy.", "Thông báo thanh toán", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                string customerId = SelectedCustomer?.Id;
                var res = _serviceBus.CheckoutPosSale(customerId, CartItems, PromoCode, SelectedPaymentMethod);
                if (!res.ok)
                {
                    MessageBox.Show(res.error ?? "Không thể thanh toán.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var receipt = new StringBuilder();
                receipt.AppendLine("=== BIÊN LAI BÁN HÀNG ===");
                receipt.AppendLine($"Mã HD: {res.invoiceId}");
                receipt.AppendLine($"Ngày: {DateTime.Now:dd/MM/yyyy HH:mm}");
                receipt.AppendLine(SelectedCustomer != null
                    ? $"Khách hàng: {SelectedCustomer.Name} ({SelectedCustomer.Id})"
                    : "Khách hàng: Khách vãng lai");
                receipt.AppendLine("---------------------------");
                foreach (var item in CartItems)
                    receipt.AppendLine($"{item.ServiceName} x{item.Quantity} = {item.Total:N0}");
                receipt.AppendLine("---------------------------");
                if (DiscountAmount > 0)
                    receipt.AppendLine($"GIẢM: {DiscountAmount:N0}");
                receipt.AppendLine($"TỔNG CỘNG: {TotalPayable:N0}");
                receipt.AppendLine($"THANH TOÁN: {SelectedPaymentMethod}");

                MessageBox.Show(receipt.ToString(), "Thanh toán thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // --- NEW PRINT LOGIC ---
                var printVm = new ReceiptViewModel
                {
                    InvoiceId = res.invoiceId,
                    IssuedAt = DateTime.Now,
                    CustomerName = SelectedCustomer?.Name ?? "Khách vãng lai",
                    Items = CartItems.ToList(), // Convert to list to avoid collection issues
                    Subtotal = CartSubtotal,
                    Discount = DiscountAmount,
                    TotalPayable = TotalPayable
                };
                PrintService.PrintReceipt(printVm);
                // -----------------------

                CartItems.Clear();
                PromoCode = string.Empty;
                DiscountAmount = 0;
                AppliedPromotionId = null;

                Load(); // reload stock + service list
                LoadServiceSales(reset: true); // show new invoice in history
                RecalculateTotals();
            }
        }

        private void ExecuteApplyPromo()
        {
            string code = (PromoCode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                DiscountAmount = 0;
                AppliedPromotionId = null;
                return;
            }

            var res = _serviceBus.TryApplyPromotion(code, CartSubtotal);
            if (!res.ok)
            {
                DiscountAmount = 0;
                AppliedPromotionId = null;
                MessageBox.Show(res.error ?? "Mã khuyến mãi không hợp lệ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AppliedPromotionId = res.promotionId;
            DiscountAmount = res.discount;
            MessageBox.Show($"Đã áp dụng mã {res.promotionId}. Giảm {DiscountAmount:N0}.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void Load()
        {
            try
            {
                _allServices = _serviceBus.GetAllServices() ?? new ObservableCollection<Service>();

                var customersList = _customerBus.GetAllCustomers();
                Customers.Clear();
                if (customersList != null)
                {
                    foreach (var c in customersList) Customers.Add(c);
                }
            }
            catch
            {
                _allServices = new ObservableCollection<Service>();
            }

            ApplyFilter();
            RecalculateTotals();
        }

        private void LoadServiceSales(bool reset = true)
        {
            try
            {
                if (reset)
                {
                    _currentSalesPage = 1;
                    _allServiceSales.Clear();
                    HasMoreSales = true;
                }

                string customerId = SalesSelectedCustomer?.Id;
                int offset = (_currentSalesPage - 1) * SalesPageSize;
                
                var newInvoices = _serviceBus.GetServiceSaleInvoices(SalesFromDate, SalesToDate, customerId, SalesPageSize, offset);
                
                if (newInvoices == null || newInvoices.Count < SalesPageSize)
                    HasMoreSales = false;
                else
                    HasMoreSales = true;

                if (newInvoices != null)
                {
                    foreach (var inv in newInvoices)
                        _allServiceSales.Add(inv);
                    
                    _currentSalesPage++;
                }
            }
            catch
            {
                if (reset) _allServiceSales.Clear();
                HasMoreSales = false;
            }

            ApplySalesFilter();
            LoadMoreSalesCommand.RaiseCanExecuteChanged();
        }

        private void LoadActiveBookings()
        {
            ActiveBookings.Clear();
            var list = _bookingBus.GetActiveBookings();
            if (list != null)
            {
                foreach (var b in list) ActiveBookings.Add(b);
            }
        }

        private void ApplySalesFilter()
        {
            var rows = _allServiceSales.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SalesSearchText))
            {
                string keyword = SalesSearchText.Trim();
                rows = rows.Where(r =>
                    (r.InvoiceId?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.BookingId?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.CustomerName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.CustomerId?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.PaymentMethod?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            ServiceSales = new ObservableCollection<ServiceSaleInvoice>(rows);
            OnPropertyChanged(nameof(ServiceSales));
        }

        public void ApplyFilter()
        {
            var filtered = _allServices.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "All")
                filtered = filtered.Where(s => string.Equals(s.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string keyword = SearchText.Trim();
                filtered = filtered.Where(s => s.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            Services = new ObservableCollection<Service>(filtered);
            OnPropertyChanged(nameof(Services));
        }
    }
}


