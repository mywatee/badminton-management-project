using QuanLySCL.BUS;
using QuanLySCL.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QuanLySCL.GUI.Windows
{
    public partial class UpsertPricingWindow : Window
    {
        private readonly PriceEntry _existingEntry;
        private readonly bool _isEdit;

        public UpsertPricingWindow(PriceEntry entry = null)
        {
            InitializeComponent();
            _existingEntry = entry;
            _isEdit = entry != null;

            LoadData();

            if (_isEdit)
            {
                CbCourtType.SelectedValue = entry.CourtTypeId;
                CbTimeSlot.SelectedValue = entry.SlotId;
                
                // Set BookingType combo
                foreach (ComboBoxItem item in CbBookingType.Items)
                {
                    if (item.Tag?.ToString() == entry.BookingType)
                    {
                        CbBookingType.SelectedItem = item;
                        break;
                    }
                }

                TxtPrice.Text = entry.Price.ToString("F0");
                Title = "Sửa cấu hình Giá";
            }
            else
            {
                Title = "Thêm cấu hình Giá mới";
                CbBookingType.SelectedIndex = 0;
            }
        }

        private void LoadData()
        {
            CbCourtType.ItemsSource = new CourtBUS().GetCourtTypes();
            CbTimeSlot.ItemsSource = new AdminBUS().GetAllTimeSlots();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (CbCourtType.SelectedValue == null || CbTimeSlot.SelectedValue == null || CbBookingType.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn đầy đủ Loại sân, Ca giờ và Hình thức thuê.", "Thiếu thông tin");
                return;
            }

            if (!decimal.TryParse(TxtPrice.Text.Trim(), out decimal price))
            {
                MessageBox.Show("Vui lòng nhập đơn giá hợp lệ.", "Lỗi nhập liệu");
                return;
            }

            var entry = new PriceEntry
            {
                Id = _existingEntry?.Id,
                CourtTypeId = CbCourtType.SelectedValue.ToString(),
                SlotId = CbTimeSlot.SelectedValue.ToString(),
                BookingType = (CbBookingType.SelectedItem as ComboBoxItem)?.Tag?.ToString(),
                Price = price
            };

            var bus = new AdminBUS();
            bool ok;
            string error;

            if (_isEdit)
                ok = bus.UpdatePriceEntry(entry, out error);
            else
                ok = bus.AddPriceEntry(entry, out error);

            if (ok)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Lỗi: " + error, "Lỗi lưu dữ liệu");
            }
        }
    }
}
