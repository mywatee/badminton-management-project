using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BadmintonManagement.BUS; // Nhớ dùng tầng BUS
using System.Windows;
using BadmintonManagement.DTO;

namespace BadmintonManagement.GUI
{
    public partial class MainWindow : Window
    {
        // Khởi tạo tầng BUS
        private SanBUS _sanBUS = new SanBUS();

        public MainWindow()
        {
            InitializeComponent();
            // Load dữ liệu ngay khi mở ứng dụng
            LoadSanDoSan();
            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = System.TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => {
                txtBigTime.Text = System.DateTime.Now.ToString("HH:mm");
                txtDate.Text = System.DateTime.Now.ToString("dd/MM/yyyy");
            };
            timer.Start();
        }
        private void UpdateStatus()
        {
            int total = 10; // Giả sử tổng 10 sân
            int available = 8; // Lấy từ database
            txtStatusCount.Text = $" {available}/{total} sân trống";
        }

        // Đừng quên tạo hàm cho nút bấm mới thêm
        private void btnNewBooking_Click(object sender, RoutedEventArgs e)
        {
            // Mở Form đặt lịch ở đây Huy Hoàng nhé!
            MessageBox.Show("Mở form đặt sân mới cho khách...");
        }

        private void btnQuanLySan_Click(object sender, RoutedEventArgs e)
        {
            LoadSanDoSan();
        }
        private void btnCourt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dsSan = _sanBUS.LayTatCaSan();

                // Lọc dữ liệu theo trạng thái đã thiết kế trong Database
                icSanSan.ItemsSource = dsSan.Where(s => s.TrangThai == "Sẵn sàng").ToList();
                icBaoTri.ItemsSource = dsSan.Where(s => s.TrangThai == "Bảo trì").ToList();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }
        private void LoadSanDoSan()
        {
            try
            {
                // Lấy toàn bộ danh sách sân từ tầng BUS
                var dsSan = _sanBUS.LayTatCaSan();

                // 1. Đổ dữ liệu vào cột "SẴN SÀNG"
                icSanSan.ItemsSource = dsSan.Where(s => s.TrangThai == "Sẵn sàng").ToList();

                // 2. Đổ dữ liệu vào cột "BẢO TRÌ"
                icBaoTri.ItemsSource = dsSan.Where(s => s.TrangThai == "Bảo trì").ToList();

                // Lưu ý: Nếu bạn có thêm cột "Đang sử dụng", hãy làm tương tự:
                // icDangSuDung.ItemsSource = dsSan.Where(s => s.TrangThai == "Đang sử dụng").ToList();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
        }
    }
}