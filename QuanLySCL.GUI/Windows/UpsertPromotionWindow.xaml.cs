using QuanLySCL.Models;
using System;
using System.Windows;

namespace QuanLySCL.GUI.Windows
{
    public partial class UpsertPromotionWindow : Window
    {
        public Promotion EditedPromo { get; private set; }
        public bool IsNew { get; private set; }

        public UpsertPromotionWindow(Promotion promo = null)
        {
            InitializeComponent();
            IsNew = promo == null;

            if (promo != null)
            {
                txtTitle.Text = "SỬA KHUYẾN MÃI";
                txtMaKM.Text = promo.MaKM;
                txtMaKM.IsReadOnly = true; 
                txtTenKM.Text = promo.TenKM;
                cmbKieu.Text = promo.Kieu;
                txtGiaTri.Text = promo.GiaTri.ToString("G0");
                txtDonToiThieu.Text = promo.DonToiThieu?.ToString("G0");
                dpNgayBD.SelectedDate = promo.NgayBD;
                dpNgayKT.SelectedDate = promo.NgayKT;
                chkTrangThai.IsChecked = promo.TrangThai;
            }
        }

        private void ShowError(string msg)
        {
            txtError.Text = msg;
            txtError.Visibility = Visibility.Visible;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMaKM.Text)) { ShowError("Mã KM không được để trống."); return; }
            if (string.IsNullOrWhiteSpace(txtTenKM.Text)) { ShowError("Tên chương trình không được để trống."); return; }
            if (string.IsNullOrWhiteSpace(cmbKieu.Text)) { ShowError("Loại giảm không được để trống."); return; }
            if (!decimal.TryParse(txtGiaTri.Text, out decimal giaTri)) { ShowError("Mức giảm sai định dạng số."); return; }
            
            decimal? donToiThieu = null;
            if (!string.IsNullOrWhiteSpace(txtDonToiThieu.Text))
            {
                if (!decimal.TryParse(txtDonToiThieu.Text, out decimal dt)) { ShowError("Đơn tối thiểu sai định dạng số."); return; }
                donToiThieu = dt;
            }

            EditedPromo = new Promotion
            {
                MaKM = txtMaKM.Text.Trim().ToUpper(),
                TenKM = txtTenKM.Text.Trim(),
                Kieu = cmbKieu.Text,
                GiaTri = giaTri,
                DonToiThieu = donToiThieu,
                NgayBD = dpNgayBD.SelectedDate,
                NgayKT = dpNgayKT.SelectedDate,
                TrangThai = chkTrangThai.IsChecked ?? true
            };

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
