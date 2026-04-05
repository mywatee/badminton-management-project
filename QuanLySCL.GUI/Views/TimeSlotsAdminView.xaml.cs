using QuanLySCL.BUS;
using QuanLySCL.GUI.ViewModels;
using QuanLySCL.GUI.Windows;
using QuanLySCL.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace QuanLySCL.GUI.Views
{
    public partial class TimeSlotsAdminView : Page
    {
        private TimeSlotsAdminViewModel Vm => DataContext as TimeSlotsAdminViewModel;

        public TimeSlotsAdminView()
        {
            InitializeComponent();
            DataContext = new TimeSlotsAdminViewModel();
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            Vm?.Load();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (Vm == null) return;

            var win = new UpsertTimeSlotWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (win.ShowDialog() == true)
                Vm.Load();
        }

        private void QuickGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (Vm == null) return;

            var win = new QuickGenerateTimeSlotsWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (win.ShowDialog() == true)
                Vm.Load();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (Vm == null) return;
            if (Vm.SelectedSlot == null)
            {
                MessageBox.Show("Vui lòng chọn 1 ca để sửa.", "Thiếu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var win = new UpsertTimeSlotWindow(Vm.SelectedSlot)
            {
                Owner = Window.GetWindow(this)
            };

            if (win.ShowDialog() == true)
                Vm.Load();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (Vm == null) return;
            if (Vm.SelectedSlot == null)
            {
                MessageBox.Show("Vui lòng chọn 1 ca để xóa.", "Thiếu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var slot = Vm.SelectedSlot;
            var confirm = MessageBox.Show(
                $"Xóa {slot.Name} ({slot.DisplayLabel})?\nLưu ý: sẽ thất bại nếu ca đang được sử dụng trong bảng giá hoặc lịch đặt.",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            var bus = new AdminBUS();
            if (!bus.DeleteTimeSlot(slot.Id, out string error))
            {
                MessageBox.Show("Không thể xóa: " + (error ?? "Lỗi không xác định"), "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Vm.Load();
        }
    }
}
