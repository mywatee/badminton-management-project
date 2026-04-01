using QuanLySCL.BUS;
using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace QuanLySCL.GUI.Windows
{
    public partial class UpsertServiceWindow : Window
    {
        private readonly ServiceBUS _bus = new ServiceBUS();
        private readonly bool _isEdit;
        private readonly Service _editing;

        public string HeaderText { get; set; }
        public string ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string Unit { get; set; }
        public string PriceText { get; set; }
        public string StockText { get; set; }

        public ObservableCollection<string> CategoryOptions { get; set; } =
            new ObservableCollection<string> { "Drinks", "Equipment" };

        public string SelectedCategory { get; set; } = "Drinks";

        public string ErrorText { get; set; }

        public UpsertServiceWindow(Service editing = null)
        {
            InitializeComponent();

            _editing = editing;
            _isEdit = editing != null;

            if (_isEdit)
            {
                HeaderText = "Sửa dịch vụ";
                ServiceId = editing.Id;
                ServiceName = editing.Name;
                Unit = editing.Unit;
                PriceText = editing.Price.ToString("0.##", CultureInfo.InvariantCulture);
                StockText = editing.Stock.ToString(CultureInfo.InvariantCulture);
                SelectedCategory = editing.Category == "Equipment" ? "Equipment" : "Drinks";
            }
            else
            {
                HeaderText = "Thêm dịch vụ";
                ServiceId = "(Tự sinh)";
                ServiceName = string.Empty;
                Unit = string.Empty;
                PriceText = "0";
                StockText = "0";
                SelectedCategory = "Drinks";
            }

            DataContext = this;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText = null;

            if (!decimal.TryParse(PriceText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal price))
            {
                ErrorText = "Giá không hợp lệ (gợi ý: 10000 hoặc 10000.5).";
                Refresh();
                return;
            }

            if (!int.TryParse(StockText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int stock) || stock < 0)
            {
                ErrorText = "Tồn kho không hợp lệ (>= 0).";
                Refresh();
                return;
            }

            if (!_isEdit)
            {
                var res = _bus.CreateServiceAutoId(SelectedCategory, ServiceName, Unit, price, stock);
                if (!res.ok)
                {
                    ErrorText = res.error ?? "Không thể thêm dịch vụ.";
                    Refresh();
                    return;
                }

                DialogResult = true;
                Close();
                return;
            }

            if (_editing == null)
            {
                ErrorText = "Không có dữ liệu để sửa.";
                Refresh();
                return;
            }

            var upd = _bus.UpdateService(_editing.Id, SelectedCategory, ServiceName, Unit, price, stock);
            if (!upd.ok)
            {
                ErrorText = upd.error ?? "Không thể cập nhật dịch vụ.";
                Refresh();
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Refresh()
        {
            DataContext = null;
            DataContext = this;
        }
    }
}
