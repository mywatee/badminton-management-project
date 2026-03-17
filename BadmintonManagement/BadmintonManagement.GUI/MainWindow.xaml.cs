using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Threading;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BadmintonManagement.GUI
{
    public partial class MainWindow : Window
    {
        string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=QLSCL;Integrated Security=True;TrustServerCertificate=True";
        private string loggedInMaKH;
        private string loggedInVaiTro; // Biến mới

        // SỬA CONSTRUCTOR NHẬN THÊM THAM SỐ 'vaiTro'
        public MainWindow(string maKH, string vaiTro)
        {
            InitializeComponent();
            this.loggedInMaKH = maKH;
            this.loggedInVaiTro = vaiTro; // Gán giá trị

            StartClock();
            LoadAllData();
            ShowUserInfo();

            // Test nhanh xem có phải Admin không
            if (vaiTro == "Admin")
            {
                MessageBox.Show("Đăng nhập thành công với quyền ADMIN!", "Thông báo");
                // Bạn có thể thêm logic ẩn/hiện nút ở đây
            }
        }

        public MainWindow() { InitializeComponent(); }

        private void ShowUserInfo()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "SELECT HoTen FROM KHACH_HANG WHERE MaKH = @ma";
                    // Nếu là Admin (MaKH null), có thể cần query bảng NHAN_VIEN, tạm thời giữ nguyên
                    if (loggedInMaKH != null)
                    {
                        SqlCommand cmd = new SqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@ma", loggedInMaKH);
                        object result = cmd.ExecuteScalar();
                        if (result != null && txtUserDisplayName != null)
                        {
                            txtUserDisplayName.Text = result.ToString();
                        }
                    }
                    else if (txtUserDisplayName != null)
                    {
                        txtUserDisplayName.Text = "Admin";
                    }
                }
                catch { }
            }
        }

        private void ApplyPermissions()
        {
            // Giả sử bạn có các nút Menu tên là: btnDatSan, btnDichVu, btnKhachHang

            // Nếu là Khách Hàng (KhachHang)
            if (loggedInVaiTro == "KhachHang")
            {
                // Chỉ cho phép xem Dashboard và Đặt sân
                // btnDatSan.IsEnabled = true; 
                // btnDichVu.IsEnabled = false; // Vô hiệu hóa hoặc ẩn
                // btnKhachHang.Visibility = Visibility.Collapsed; // Ẩn hoàn toàn
            }
            // Nếu là Admin hoặc Nhân viên
            else if (loggedInVaiTro == "Admin" || loggedInVaiTro == "NhanVien")
            {
                // Cho phép truy cập tất cả
                // btnDatSan.IsEnabled = true;
                // btnDichVu.IsEnabled = true;
                // btnKhachHang.Visibility = Visibility.Visible;

                // Đổi màu sidebar hoặc thông báo đặc biệt nếu muốn
            }
        }

        // 1. Đồng hồ hiển thị trên Dashboard
        private void StartClock()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) =>
            {
                txtBigTime.Text = DateTime.Now.ToString("HH:mm");
                txtDate.Text = DateTime.Now.ToString("dd/MM/yyyy");
            };
            timer.Start();
        }

        // 2. Hàm tổng hợp để load tất cả dữ liệu
        private void LoadAllData()
        {
            LoadAvailableCourts();   // Cột 1: Sân trống
            LoadActiveCourts();      // Cột 2: Đang thi đấu
            LoadMaintenanceCourts(); // Cột 3: Bảo trì
            LoadWaitingList();       // Cột 4: Nhật ký đặt sân
        }

        // CỘT 1: Sân trống & Cập nhật con số tổng quát
        private void LoadAvailableCourts()
        {
            List<object> availableCourts = new List<object>();
            int totalCourts = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    // Đếm tổng số sân
                    SqlCommand cmdCount = new SqlCommand("SELECT COUNT(*) FROM SAN", conn);
                    totalCourts = (int)cmdCount.ExecuteScalar();

                    // Lấy danh sách sân trống thực sự (không có ai đang nhận sân)
                    string sql = @"SELECT TenSan FROM SAN 
                                   WHERE TrangThai = N'Sẵn sàng' 
                                   AND MaSan NOT IN (
                                       SELECT MaSan FROM CT_DAT_SAN CT 
                                       JOIN DAT_SAN DS ON CT.MaPhieuDat = DS.MaPhieuDat 
                                       WHERE DS.TrangThai = N'Nhận sân'
                                   )";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            availableCourts.Add(new { TenSan = r["TenSan"].ToString() });
                        }
                    }

                    if (icSanSan != null) icSanSan.ItemsSource = availableCourts;
                    if (txtStatusCount != null) txtStatusCount.Text = $"{availableCourts.Count}/{totalCourts}";
                }
                catch (Exception ex) { MessageBox.Show("Lỗi Cột 1: " + ex.Message); }
            }
        }

        // CỘT 2: Đang thi đấu
        private void LoadActiveCourts()
        {
            List<object> activeList = new List<object>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string sql = @"SELECT S.TenSan, K.HoTen 
                                 FROM CT_DAT_SAN CT
                                 JOIN DAT_SAN DS ON CT.MaPhieuDat = DS.MaPhieuDat
                                 JOIN SAN S ON CT.MaSan = S.MaSan
                                 JOIN KHACH_HANG K ON DS.MaKH = K.MaKH
                                 WHERE DS.TrangThai = N'Nhận sân'";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            activeList.Add(new
                            {
                                TenSan = r["TenSan"].ToString(),
                                TenKhach = r["HoTen"].ToString(),
                                ThoiGianConLai = "Đang đá",
                                ProgressValue = 65
                            });
                        }
                    }
                    if (icDangSuDung != null) icDangSuDung.ItemsSource = activeList;
                }
                catch (Exception ex) { MessageBox.Show("Lỗi Cột 2: " + ex.Message); }
            }
        }

        // CỘT 3: Bảo trì
        private void LoadMaintenanceCourts()
        {
            List<object> mainList = new List<object>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT TenSan FROM SAN WHERE TrangThai = N'Bảo trì'", conn);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            mainList.Add(new { TenSan = r["TenSan"].ToString() });
                        }
                    }
                    if (icBaoTri != null) icBaoTri.ItemsSource = mainList;
                }
                catch (Exception ex) { MessageBox.Show("Lỗi Cột 3: " + ex.Message); }
            }
        }

        // CỘT 4: Nhật ký đặt sân hôm nay
        private void LoadWaitingList()
        {
            List<object> waitingList = new List<object>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string sql = @"SELECT S.TenSan, K.HoTen, CG.GioBatDau, DS.TrangThai 
                                 FROM CT_DAT_SAN CT
                                 JOIN DAT_SAN DS ON CT.MaPhieuDat = DS.MaPhieuDat
                                 JOIN SAN S ON CT.MaSan = S.MaSan
                                 JOIN KHACH_HANG K ON DS.MaKH = K.MaKH
                                 JOIN CA_GIO CG ON CT.MaCa = CG.MaCa
                                 WHERE CT.NgaySuDung = CAST(GETDATE() AS DATE)
                                 ORDER BY CG.GioBatDau ASC";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            waitingList.Add(new
                            {
                                TenSan = r["TenSan"].ToString(),
                                ThongTinCho = $"{r["HoTen"]} ({r["GioBatDau"]}) - {r["TrangThai"]}"
                            });
                        }
                    }
                    if (icKhachDoi != null) icKhachDoi.ItemsSource = waitingList;
                }
                catch (Exception ex) { MessageBox.Show("Lỗi Cột 4: " + ex.Message); }
            }
        }

        // CÁC SỰ KIỆN NÚT BẤM
        private void btnCourt_Click(object sender, RoutedEventArgs e)
        {
            LoadAllData(); // Nhấn vào Sơ đồ sân để làm mới dữ liệu
        }

        private void btnNewBooking_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Mở Form đặt sân mới...", "Thông báo");
        }
    }
}