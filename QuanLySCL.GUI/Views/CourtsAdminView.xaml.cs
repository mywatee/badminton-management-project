using QuanLySCL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using QuanLySCL.Models;
using QuanLySCL.BUS;
using QuanLySCL.GUI.Windows;

namespace QuanLySCL.GUI.Views
{
    /// <summary>
    /// Interaction logic for CourtsAdminView.xaml
    /// </summary>
    public partial class CourtsAdminView : Page
    {
        private readonly CourtsAdminViewModel _viewModel;
        public CourtsAdminView()
        {
            InitializeComponent();
            _viewModel = new CourtsAdminViewModel();
            DataContext = _viewModel;
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Load();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var window = new UpsertCourtWindow(_viewModel.CourtTypes);
            if (window.ShowDialog() == true)
            {
                _viewModel.Load();
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedCourt == null)
            {
                MessageBox.Show("Vui lòng chọn sân cần sửa.");
                return;
            }
            var window = new UpsertCourtWindow(_viewModel.CourtTypes, _viewModel.SelectedCourt);
            if (window.ShowDialog() == true)
            {
                _viewModel.Load();
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedCourt == null)
            {
                MessageBox.Show("Vui lòng chọn sân cần xóa.");
                return;
            }

            var result = MessageBox.Show("Bạn có chắc chắn muốn xóa sân này?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var bus = new CourtBUS();
                if (bus.DeleteCourt(_viewModel.SelectedCourt.Id, out string error))
                {
                    _viewModel.Load();
                }
                else
                {
                    MessageBox.Show(error);
                }
            }
        }
    }
}
