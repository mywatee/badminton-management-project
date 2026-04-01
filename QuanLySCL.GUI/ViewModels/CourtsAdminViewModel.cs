using QuanLySCL.BUS;
using QuanLySCL.Models;
using System.Collections.ObjectModel;

namespace QuanLySCL.GUI.ViewModels
{
    public class CourtsAdminViewModel : BaseViewModel
    {
        private readonly CourtBUS _courtBus = new CourtBUS();

        public ObservableCollection<Court> Courts { get; set; } = new ObservableCollection<Court>();
        public ObservableCollection<CourtType> CourtTypes { get; set; } = new ObservableCollection<CourtType>();

        private Court _selectedCourt;
        public Court SelectedCourt
        {
            get => _selectedCourt;
            set => SetProperty(ref _selectedCourt, value);
        }

        public CourtsAdminViewModel()
        {
            Load();
        }

        public void Load()
        {
            try
            {
                Courts = _courtBus.GetAllCourts();
                CourtTypes = _courtBus.GetCourtTypes();
            }
            catch
            {
                Courts = new ObservableCollection<Court>();
                CourtTypes = new ObservableCollection<CourtType>();
            }

            OnPropertyChanged(nameof(Courts));
            OnPropertyChanged(nameof(CourtTypes));
        }
    }
}

