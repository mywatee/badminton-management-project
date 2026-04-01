using QuanLySCL.BUS;
using QuanLySCL.GUI.ViewModels;
using QuanLySCL.GUI.Windows;
using QuanLySCL.Models;
using System.Windows;
using System.Windows.Controls;

namespace QuanLySCL.GUI.Views
{
    public partial class PricingAdminView : Page
    {
        private PricingAdminViewModel Vm => DataContext as PricingAdminViewModel;

        public PricingAdminView()
        {
            InitializeComponent();
            DataContext = new PricingAdminViewModel();
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            Vm?.Load();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (Vm == null) return;

            var win = new UpsertPricingWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (win.ShowDialog() == true)
                Vm.Load();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var entry = (sender as Button)?.DataContext as CompactPriceEntry;
            if (entry == null) entry = Vm?.SelectedEntry;
            if (entry == null) return;

            // Pick which one to edit
            PriceEntry target = null;
            if (entry.IdLe != null && entry.IdFixed != null)
            {
                var result = MessageBox.Show("Sửa giá Lẻ? (Chọn No để sửa giá Cố định)", "Chọn loại giá", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes) target = CreateFromCompact(entry, "Lẻ");
                else if (result == MessageBoxResult.No) target = CreateFromCompact(entry, "Cố định");
            }
            else if (entry.IdLe != null) target = CreateFromCompact(entry, "Lẻ");
            else if (entry.IdFixed != null) target = CreateFromCompact(entry, "Cố định");

            if (target == null) return;

            var win = new UpsertPricingWindow(target)
            {
                Owner = Window.GetWindow(this)
            };

            if (win.ShowDialog() == true)
                Vm.Load();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var entry = (sender as Button)?.DataContext as CompactPriceEntry;
            if (entry == null) entry = Vm?.SelectedEntry;
            if (entry == null) return;

            string idToDelete = null;
            if (entry.IdLe != null && entry.IdFixed != null)
            {
                var result = MessageBox.Show("Xóa giá Lẻ? (Chọn No để xóa giá Cố định)", "Chọn loại giá", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes) idToDelete = entry.IdLe;
                else if (result == MessageBoxResult.No) idToDelete = entry.IdFixed;
            }
            else if (entry.IdLe != null) idToDelete = entry.IdLe;
            else if (entry.IdFixed != null) idToDelete = entry.IdFixed;

            if (idToDelete == null) return;

            var bus = new AdminBUS();
            if (bus.DeletePriceEntry(idToDelete, out string error))
                Vm.Load();
            else
                MessageBox.Show("Lỗi: " + error);
        }

        private PriceEntry CreateFromCompact(CompactPriceEntry c, string type)
        {
            return new PriceEntry
            {
                Id = type == "Lẻ" ? c.IdLe : c.IdFixed,
                CourtTypeId = c.CourtTypeId,
                CourtTypeName = c.CourtTypeName,
                SlotId = c.SlotId,
                SlotName = c.SlotName,
                BookingType = type,
                Price = type == "Lẻ" ? c.PriceLe : c.PriceFixed
            };
        }
    }
}
