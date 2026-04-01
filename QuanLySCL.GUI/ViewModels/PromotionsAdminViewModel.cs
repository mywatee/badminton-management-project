using QuanLySCL.BUS;
using QuanLySCL.Models;
using System.Collections.ObjectModel;

namespace QuanLySCL.GUI.ViewModels
{
    public class PromotionsAdminViewModel : BaseViewModel
    {
        private readonly KhuyenMaiBUS _bus = new KhuyenMaiBUS();

        private ObservableCollection<Promotion> _promotions;
        public ObservableCollection<Promotion> Promotions
        {
            get => _promotions;
            set => SetProperty(ref _promotions, value);
        }

        private Promotion _selectedPromotion;
        public Promotion SelectedPromotion
        {
            get => _selectedPromotion;
            set => SetProperty(ref _selectedPromotion, value);
        }

        public PromotionsAdminViewModel()
        {
            LoadData();
        }

        public void LoadData()
        {
            Promotions = _bus.GetAllPromotions();
            SelectedPromotion = null;
        }

        public (bool ok, string error) SavePromotion(bool isNew, Promotion promo)
        {
            var result = _bus.CreateOrUpdatePromotion(isNew, promo);
            if (result.ok) LoadData();
            return result;
        }

        public (bool ok, string error) DeletePromotion(string maKM)
        {
            var result = _bus.DeletePromotion(maKM);
            if (result.ok) LoadData();
            return result;
        }
    }
}
