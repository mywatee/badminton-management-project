using System;
using System.Collections.ObjectModel;
using System.Linq;
using QuanLySCL.BUS;
using QuanLySCL.Models;
using LiveCharts;
using LiveCharts.Wpf;
using System.Windows.Input;
using System.Collections.Generic;

namespace QuanLySCL.GUI.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private readonly ReportingBUS _reportingBus = new ReportingBUS();

        private string _selectedTimeRange = "Theo tháng";
        public string SelectedTimeRange
        {
            get => _selectedTimeRange;
            set
            {
                if (SetProperty(ref _selectedTimeRange, value))
                    LoadData();
            }
        }

        public ObservableCollection<string> TimeRanges { get; } = new ObservableCollection<string> 
        { 
            "Theo ngày", "Theo tuần", "Theo tháng", "Theo năm" 
        };

        private DateTime? _fromDate = DateTime.Today.AddDays(-7);
        public DateTime? FromDate
        {
            get => _fromDate;
            set
            {
                if (SetProperty(ref _fromDate, value) && string.Equals(SelectedTimeRange, "Custom", StringComparison.OrdinalIgnoreCase))
                    LoadData();
            }
        }

        private DateTime? _toDate = DateTime.Today;
        public DateTime? ToDate
        {
            get => _toDate;
            set
            {
                if (SetProperty(ref _toDate, value) && string.Equals(SelectedTimeRange, "Custom", StringComparison.OrdinalIgnoreCase))
                    LoadData();
            }
        }

        private ReportSummary _summary = new ReportSummary();
        public ReportSummary Summary
        {
            get => _summary;
            set => SetProperty(ref _summary, value);
        }

        private SeriesCollection _revenueSeries;
        public SeriesCollection RevenueSeries
        {
            get => _revenueSeries;
            set => SetProperty(ref _revenueSeries, value);
        }

        private List<string> _revenueLabels = new List<string>();
        public List<string> RevenueLabels
        {
            get => _revenueLabels;
            set => SetProperty(ref _revenueLabels, value);
        }

        private SeriesCollection _categorySeries = new SeriesCollection();
        public SeriesCollection CategorySeries
        {
            get => _categorySeries;
            set => SetProperty(ref _categorySeries, value);
        }

        private SeriesCollection _categorySeriesKPI = new SeriesCollection();
        public SeriesCollection CategorySeriesKPI
        {
            get => _categorySeriesKPI;
            set => SetProperty(ref _categorySeriesKPI, value);
        }

        private ObservableCollection<TopCustomerReport> _topCustomers = new ObservableCollection<TopCustomerReport>();
        public ObservableCollection<TopCustomerReport> TopCustomers
        {
            get => _topCustomers;
            set => SetProperty(ref _topCustomers, value);
        }

        private ObservableCollection<RevenueByCourt> _courtRevenue = new ObservableCollection<RevenueByCourt>();
        public ObservableCollection<RevenueByCourt> CourtRevenue
        {
            get => _courtRevenue;
            set => SetProperty(ref _courtRevenue, value);
        }

        private ObservableCollection<RevenueByService> _serviceRevenue = new ObservableCollection<RevenueByService>();
        public ObservableCollection<RevenueByService> ServiceRevenue
        {
            get => _serviceRevenue;
            set => SetProperty(ref _serviceRevenue, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ExportCommand { get; }
        public Func<double, string> Formatter { get; } = x => x.ToString("N0");

        public ReportsViewModel()
        {
            RevenueSeries = new SeriesCollection();
            CategorySeries = new SeriesCollection();
            CategorySeriesKPI = new SeriesCollection();

            if (!TimeRanges.Contains("Custom"))
                TimeRanges.Add("Custom");

            RefreshCommand = new RelayCommand(_ => LoadData());
            ExportCommand = new RelayCommand(_ => ExportReport());
            
            // Safe initial load after UI has chance to initialize
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => LoadData()), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public void LoadData()
        {
            try
            {
                bool custom = string.Equals(SelectedTimeRange, "Custom", StringComparison.OrdinalIgnoreCase);
                DateTime from = (FromDate ?? DateTime.Today).Date;
                DateTime to = (ToDate ?? from).Date;
                if (to < from) (from, to) = (to, from);

                // Basic Summary
                Summary = custom ? _reportingBus.GetSummaryByDateRange(from, to) : _reportingBus.GetSummary(SelectedTimeRange);

                // Revenue Trends (Line Chart)
                var monthlyRevenue = custom
                    ? (_reportingBus.GetRevenueTrendsByDateRange(from, to) ?? new List<RevenueByMonth>())
                    : (_reportingBus.GetMonthlyRevenue(SelectedTimeRange) ?? new List<RevenueByMonth>());
                var revenueValues = new ChartValues<double>();
                revenueValues.AddRange(monthlyRevenue.Select(x => (double)x.Revenue));

                RevenueSeries.Clear();
                RevenueSeries.Add(new LineSeries
                {
                    Title = "Doanh thu",
                    Values = revenueValues,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 10,
                    StrokeThickness = 3
                });
                RevenueLabels = monthlyRevenue.Select(x => x.Month).ToList();

                // Revenue By Category (Pie Chart)
                var categoryRevenue = custom
                    ? (_reportingBus.GetCategoryRevenueByDateRange(from, to) ?? new List<RevenueByCategory>())
                    : (_reportingBus.GetCategoryRevenue(SelectedTimeRange) ?? new List<RevenueByCategory>());
                
                CategorySeries.Clear();
                CategorySeriesKPI.Clear();

                foreach (var cat in categoryRevenue)
                {
                    double value = (double)cat.Revenue;
                    
                    CategorySeries.Add(new PieSeries
                    {
                        Title = cat.Category,
                        Values = new ChartValues<double> { value },
                        DataLabels = true
                    });

                    CategorySeriesKPI.Add(new PieSeries
                    {
                        Title = cat.Category,
                        Values = new ChartValues<double> { value },
                        DataLabels = false
                    });
                }

                // Top Customers
                TopCustomers = new ObservableCollection<TopCustomerReport>(
                    custom
                        ? (_reportingBus.GetTopCustomersByDateRange(from, to, top: 20) ?? new List<TopCustomerReport>())
                        : (_reportingBus.GetTopCustomersWithId(20, SelectedTimeRange) ?? new List<TopCustomerReport>()));

                CourtRevenue = new ObservableCollection<RevenueByCourt>(
                    custom
                        ? (_reportingBus.GetCourtRevenueByDateRange(from, to) ?? new List<RevenueByCourt>())
                        : (_reportingBus.GetCourtRevenue(SelectedTimeRange) ?? new List<RevenueByCourt>()));

                ServiceRevenue = new ObservableCollection<RevenueByService>(
                    custom
                        ? (_reportingBus.GetServiceRevenueByDateRange(from, to) ?? new List<RevenueByService>())
                        : (_reportingBus.GetServiceRevenue(SelectedTimeRange) ?? new List<RevenueByService>()));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ReportsViewModel Error: " + ex.Message);
            }
        }

        private void ExportReport()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd}.csv",
                    DefaultExt = ".csv",
                    Filter = "CSV documents (.csv)|*.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    var lines = new List<string>();
                    lines.Add("BÁO CÁO TỔNG QUAN DOANH THU");
                    lines.Add("Tổng Doanh Thu,Tổng Số Đặt Sân,Doanh Thu TB/Ngày,Tỷ Lệ Tăng Trưởng");
                    lines.Add($"{Summary.TotalRevenue},{Summary.TotalBookings},{Summary.AvgRevenuePerDay},{Summary.RevenueGrowth}%");
                    lines.Add("");
                    lines.Add("TOP KHÁCH HÀNG");
                    lines.Add("Tên Khách Hàng,Số Lượng Đặt,Tổng Tiền Thuê");
                    foreach (var c in TopCustomers)
                    {
                        lines.Add($"{c.Name},{c.TotalBookings},{c.TotalSpent}");
                    }

                    // Sử dụng UTF8 với BOM để Excel hỗ trợ hiển thị Tiếng Việt
                    System.IO.File.WriteAllLines(dialog.FileName, lines, new System.Text.UTF8Encoding(true));
                    System.Windows.MessageBox.Show("Xuất báo cáo CSV thành công!", "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi khi xuất file: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
