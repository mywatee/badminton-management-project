using QuanLySCL.GUI.ViewModels;
using QuanLySCL.GUI.Windows;
using QuanLySCL.Models;
using System.Windows;
using System.Windows.Controls;

namespace QuanLySCL.GUI.Views
{
    public partial class PromotionsAdminView : Page
    {
        private readonly PromotionsAdminViewModel _viewModel;

        public PromotionsAdminView()
        {
            InitializeComponent();
            _viewModel = new PromotionsAdminViewModel();
            DataContext = _viewModel;
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadData();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new UpsertPromotionWindow { Owner = Window.GetWindow(this) };
            if (wnd.ShowDialog() == true && wnd.EditedPromo != null)
            {
                var res = _viewModel.SavePromotion(true, wnd.EditedPromo);
                if (!res.ok)
                    MessageBox.Show(res.error, "Lỗi thêm mới", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("Thêm khuyến mãi thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Promotion promo)
            {
                _viewModel.SelectedPromotion = promo;
                var wnd = new UpsertPromotionWindow(promo) { Owner = Window.GetWindow(this) };
                if (wnd.ShowDialog() == true && wnd.EditedPromo != null)
                {
                    var res = _viewModel.SavePromotion(false, wnd.EditedPromo);
                    if (!res.ok)
                        MessageBox.Show(res.error, "Lỗi cập nhật", MessageBoxButton.OK, MessageBoxImage.Error);
                    else
                        MessageBox.Show("Cập nhật khuyến mãi thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Promotion promo)
            {
                _viewModel.SelectedPromotion = promo;
                var dialog = MessageBox.Show($"Bạn có chắc chắn muốn xóa mã {promo.MaKM} không?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (dialog == MessageBoxResult.Yes)
                {
                    var result = _viewModel.DeletePromotion(promo.MaKM);
                    if (!result.ok)
                        MessageBox.Show(result.error, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
                    else
                        MessageBox.Show("Đã xóa hoặc vô hiệu hóa thành công", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}
