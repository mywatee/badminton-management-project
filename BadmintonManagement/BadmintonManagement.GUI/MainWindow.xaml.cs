using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BadmintonManagement.GUI
{
    public partial class MainWindow : Window
    {
        // Nhớ kiểm tra lại Data Source cho đúng với máy bạn (.\SQLEXPRESS hoặc localhost\SQLEXPRESS)
        string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=QLSCL;Integrated Security=True;TrustServerCertificate=True";

        public MainWindow()
        {
            InitializeComponent();
            StartClock();
            LoadAllData(); // Gọi hàm tổng hợp để load tất cả các cột
        }

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

        // Hàm này sẽ gọi tất cả các hàm con để đổ dữ liệu vào Kanban
        private void LoadAllData()
        {
            LoadAvailableCourts();  // Cột 1
            LoadActiveCourts();     // Cột 2
            LoadMaintenanceCourts(); // Cột 3
            LoadWaitingList();      // Cột 4
        }

        // CỘT 1: Sân trống & Lịch đặt trước
        private void LoadAvailableCourts()
        {
            List<object> availableCourts = new List<object>();
            int totalCourts = 0; // Biến để đếm tổng số sân

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // 1. Đếm tổng số sân đang có trong bảng SAN
                    string sqlCount = "SELECT COUNT(*) FROM SAN";
                    SqlCommand cmdCount = new SqlCommand(sqlCount, conn);
                    totalCourts = (int)cmdCount.ExecuteScalar();

                    // 2. Lấy danh sách sân trống thực sự
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

                    icSanSan.ItemsSource = availableCourts;

                    // 3. Cập nhật con số thực tế lên giao diện (Ví dụ: 5/8)
                    txtStatusCount.Text = $" {availableCourts.Count}/{totalCourts}";
                }
                catch (Exception ex) { MessageBox.Show("Lỗi đếm sân: " + ex.Message); }
            }
        }

        // CỘT 2: Đang thi đấu (Lấy thông tin khách và sân)
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
                                ProgressValue = 65 // Huy Hoàng có thể tính toán % dựa trên giờ bắt đầu/kết thúc sau này
                            });
                        }
                    }
                    icDangSuDung.ItemsSource = activeList;
                }
                catch (Exception ex) { MessageBox.Show("Lỗi Cột 2: " + ex.Message); }
            }
        }

        // CỘT 3: Thanh toán / Bảo trì
        private void LoadMaintenanceCourts()
        {
            List<object> mainList = new List<object>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "SELECT TenSan FROM SAN WHERE TrangThai = N'Bảo trì'";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            mainList.Add(new { TenSan = r["TenSan"].ToString() });
                        }
                    }
                    icBaoTri.ItemsSource = mainList;
                }
                catch (Exception ex) { MessageBox.Show("Lỗi Cột 3: " + ex.Message); }
            }
        }

        // CỘT 4: Lịch đặt sắp tới
        private void LoadWaitingList()
        {
            List<object> waitingList = new List<object>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    // Lấy tất cả các ca đặt của ngày hôm nay để làm Nhật ký (Recent Activity)
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
                            string trangThai = r["TrangThai"].ToString();
                            waitingList.Add(new
                            {
                                TenSan = r["TenSan"].ToString(),
                                // Hiển thị thêm Trạng thái để cột Nhật ký rõ ràng hơn
                                ThongTinCho = $"{r["HoTen"]} ({r["GioBatDau"]}) - {trangThai}"
                            });
                        }
                    }
                    icKhachDoi.ItemsSource = waitingList;
                }
                catch (Exception ex) { MessageBox.Show("Lỗi Cột Nhật ký: " + ex.Message); }
            }
        }

        private void btnCourt_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Đang chuyển sang màn hình Sơ đồ sân...", "Thông báo");
        }

        private void btnNewBooking_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Mở Form đặt lịch cho khách hàng mới", "Hệ thống");
        }
    }
}