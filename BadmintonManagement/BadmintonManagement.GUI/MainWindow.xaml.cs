using System;
using System.Linq;
using System.Windows;
using BadmintonManagement.BUS;
using BadmintonManagement.DTO;

namespace BadmintonManagement.GUI
{
    public partial class MainWindow : Window
    {
        private SanBUS _sanBUS = new SanBUS();

        public MainWindow()
        {
            InitializeComponent();
            StartClock();
            LoadData(); // Load dữ liệu ngay khi khởi động 
        }

        private void StartClock()
        {
            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => {
                txtBigTime.Text = DateTime.Now.ToString("HH:mm");
                txtDate.Text = DateTime.Now.ToString("dd/MM/yyyy");
            };
            timer.Start();
        }

        private void LoadData()
        {
            try
            {
                var dsSan = _sanBUS.LayTatCaSan();

                // Kiểm tra null trước khi gán để tránh crash
                if (icSanSan != null)
                    icSanSan.ItemsSource = dsSan.Where(s => s.TrangThai == "Sẵn sàng").ToList();

                if (icBaoTri != null)
                    icBaoTri.ItemsSource = dsSan.Where(s => s.TrangThai == "Bảo trì").ToList();

                if (txtStatusCount != null)
                {
                    int total = dsSan.Count;
                    int available = dsSan.Count(s => s.TrangThai == "Sẵn sàng");
                    txtStatusCount.Text = $"Trống: {available}/{total}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
        }

        private void btnCourt_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void btnNewBooking_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Mở form đặt sân mới...");
        }
    }
}