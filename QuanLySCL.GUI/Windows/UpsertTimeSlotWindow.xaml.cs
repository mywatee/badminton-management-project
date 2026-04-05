using QuanLySCL.BUS;
using QuanLySCL.Models;
using System;
using System.Windows;

namespace QuanLySCL.GUI.Windows
{
    public partial class UpsertTimeSlotWindow : Window
    {
        private readonly TimeSlot _existingSlot;
        private readonly bool _isEdit;

        public UpsertTimeSlotWindow(TimeSlot slot = null)
        {
            InitializeComponent();
            _existingSlot = slot;
            _isEdit = slot != null;

            if (_isEdit)
            {
                TxtId.Text = slot.Id;
                TxtId.IsEnabled = false;
                TxtName.Text = slot.Name;
                TxtStart.Text = slot.StartTime.ToString(@"hh\:mm");
                TxtEnd.Text = slot.EndTime.ToString(@"hh\:mm");
                ChkVang.IsChecked = slot.LaKhungGioVang;
                Title = "Sửa Ca giờ";
            }
            else
            {
                Title = "Thêm Ca giờ mới";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string id = TxtId.Text.Trim();
            string name = TxtName.Text.Trim();
            string startStr = TxtStart.Text.Trim();
            string endStr = TxtEnd.Text.Trim();

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Mã và Tên ca.", "Thiếu thông tin");
                return;
            }

            if (!TimeSpan.TryParse(startStr, out TimeSpan start) || !TimeSpan.TryParse(endStr, out TimeSpan end))
            {
                MessageBox.Show("Định dạng giờ không đúng (hh:mm).", "Lỗi định dạng");
                return;
            }

            var slot = new TimeSlot
            {
                Id = id,
                Name = name,
                StartTime = start,
                EndTime = end
            };

            var bus = new AdminBUS();
            bool ok;
            string error;

            if (_isEdit)
                ok = bus.UpdateTimeSlot(slot, out error);
            else
                ok = bus.AddTimeSlot(slot, out error);

            if (ok)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(error ?? "Lỗi lưu dữ liệu.", "Lỗi lưu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
