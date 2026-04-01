using QuanLySCL.BUS;
using QuanLySCL.Models;
using System.Collections.ObjectModel;

namespace QuanLySCL.GUI.ViewModels
{
    public class TimeSlotsAdminViewModel : BaseViewModel
    {
        private readonly AdminBUS _adminBus = new AdminBUS();

        private ObservableCollection<TimeSlot> _timeSlots;
        public ObservableCollection<TimeSlot> TimeSlots
        {
            get => _timeSlots;
            set => SetProperty(ref _timeSlots, value);
        }

        private TimeSlot _selectedSlot;
        public TimeSlot SelectedSlot
        {
            get => _selectedSlot;
            set => SetProperty(ref _selectedSlot, value);
        }

        public TimeSlotsAdminViewModel()
        {
            Load();
        }

        public void Load()
        {
            TimeSlots = _adminBus.GetAllTimeSlots();
        }
    }
}
