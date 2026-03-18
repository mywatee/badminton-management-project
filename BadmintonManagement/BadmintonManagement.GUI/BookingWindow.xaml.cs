using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;

namespace BadmintonManagement.GUI
{
    public partial class BookingWindow : Window
    {
        string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=QLSCL;Integrated Security=True;TrustServerCertificate=True";

        public BookingWindow()
        {
            InitializeComponent();
            LoadDataToComboboxes();
        }

        // 1. Load dữ liệu vào các ComboBox
        private void LoadDataToComboboxes()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // 1. Load Khách hàng
                    string sqlKH = "SELECT MaKH, HoTen, SDT FROM KHACH_HANG ORDER BY HoTen";
                    SqlDataAdapter daKH = new SqlDataAdapter(sqlKH, conn);
                    DataTable dtKH = new DataTable();
                    daKH.Fill(dtKH);

                    // SỬA LỖI Ở ĐÂY: Gán .DefaultView của DataTable
                    cbKhachHang.ItemsSource = dtKH.DefaultView;
                    cbKhachHang.DisplayMemberPath = "HoTen";
                    cbKhachHang.SelectedValuePath = "MaKH";

                    // 2. Load Sân
                    string sqlSan = "SELECT MaSan, TenSan FROM SAN WHERE TrangThai = N'Sẵn sàng'";
                    SqlDataAdapter daSan = new SqlDataAdapter(sqlSan, conn);
                    DataTable dtSan = new DataTable();
                    daSan.Fill(dtSan);

                    cbSan.ItemsSource = dtSan.DefaultView; // Dùng DefaultView
                    cbSan.DisplayMemberPath = "TenSan";
                    cbSan.SelectedValuePath = "MaSan";

                    // 3. Load Ca giờ
                    string sqlCa = "SELECT MaCa, TenCa FROM CA_GIO ORDER BY GioBatDau";
                    SqlDataAdapter daCa = new SqlDataAdapter(sqlCa, conn);
                    DataTable dtCa = new DataTable();
                    daCa.Fill(dtCa);

                    cbCaGio.ItemsSource = dtCa.DefaultView; // Dùng DefaultView
                    cbCaGio.DisplayMemberPath = "TenCa";
                    cbCaGio.SelectedValuePath = "MaCa";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
                }
            }
        }

        // 2. Sự kiện khi bấm nút Đặt Sân
        private void btnDatSan_Click(object sender, RoutedEventArgs e)
        {
            // Validate input
            if (cbKhachHang.SelectedItem == null || cbSan.SelectedItem == null || cbCaGio.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn đầy đủ Khách hàng, Sân và Ca giờ!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string maKH = ((dynamic)cbKhachHang.SelectedItem).MaKH;
            string maSan = (cbSan.SelectedItem as System.Data.DataRowView)?.Row["MaSan"].ToString();
            string maCa = (cbCaGio.SelectedItem as System.Data.DataRowView)?.Row["MaCa"].ToString();
            DateTime ngaySuDung = dpNgaySuDung.SelectedDate.Value;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    SqlTransaction trans = conn.BeginTransaction();

                    // BƯỚC 1: KIỂM TRA TRÙNG LỊCH
                    string checkSql = @"SELECT COUNT(*) FROM CT_DAT_SAN CT
                                        JOIN DAT_SAN DS ON CT.MaPhieuDat = DS.MaPhieuDat
                                        WHERE CT.MaSan = @maSan AND CT.MaCa = @maCa AND CT.NgaySuDung = @ngay
                                        AND DS.TrangThai <> N'Hủy'";

                    SqlCommand cmdCheck = new SqlCommand(checkSql, conn, trans);
                    cmdCheck.Parameters.AddWithValue("@maSan", maSan);
                    cmdCheck.Parameters.AddWithValue("@maCa", maCa);
                    cmdCheck.Parameters.AddWithValue("@ngay", ngaySuDung.Date);

                    int count = (int)cmdCheck.ExecuteScalar();
                    if (count > 0)
                    {
                        trans.Rollback();
                        MessageBox.Show("Sân này đã được đặt vào ca này rồi! Vui lòng chọn sân hoặc ca khác.", "Trùng lịch", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // BƯỚC 2: TẠO MÃ PHIẾU ĐẶT (PD + NgàyGiờPhút)
                    string maPhieuDat = "PD" + DateTime.Now.ToString("yyyyMMddHHmmss");

                    // BƯỚC 3: INSERT VÀO BẢNG DAT_SAN
                    string sqlDatSan = @"INSERT INTO DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai) 
                                         VALUES (@maPhieu, @maKH, NULL, GETDATE(), N'Lẻ', N'Chờ')";
                    SqlCommand cmdDatSan = new SqlCommand(sqlDatSan, conn, trans);
                    cmdDatSan.Parameters.AddWithValue("@maPhieu", maPhieuDat);
                    cmdDatSan.Parameters.AddWithValue("@maKH", maKH);
                    cmdDatSan.ExecuteNonQuery();

                    // BƯỚC 4: INSERT VÀO BẢNG CT_DAT_SAN
                    // Lấy đơn giá từ bảng BANG_GIA (nếu có) hoặc để tạm 0
                    decimal giaLuutru = 0;
                    // Nếu có bảng BANG_GIA, bạn query ở đây: SELECT DonGia FROM BANG_GIA WHERE MaLoaiSan = ... AND MaCa = ...

                    string sqlCTDS = @"INSERT INTO CT_DAT_SAN (MaCTDS, MaPhieuDat, MaSan, MaCa, NgaySuDung, GiaLuuTru) 
                                       VALUES (@maCTDS, @maPhieu, @maSan, @maCa, @ngay, @gia)";
                    SqlCommand cmdCTDS = new SqlCommand(sqlCTDS, conn, trans);
                    cmdCTDS.Parameters.AddWithValue("@maCTDS", maPhieuDat + "_01"); // Mã chi tiết
                    cmdCTDS.Parameters.AddWithValue("@maPhieu", maPhieuDat);
                    cmdCTDS.Parameters.AddWithValue("@maSan", maSan);
                    cmdCTDS.Parameters.AddWithValue("@maCa", maCa);
                    cmdCTDS.Parameters.AddWithValue("@ngay", ngaySuDung.Date);
                    cmdCTDS.Parameters.AddWithValue("@gia", giaLuutru);
                    cmdCTDS.ExecuteNonQuery();

                    trans.Commit();

                    DialogResult = true; // Báo thành công cho form cha
                    MessageBox.Show("Đặt sân thành công!\nMã phiếu: " + maPhieuDat, "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi đặt sân: " + ex.Message);
                }
            }
        }

        private void btnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}