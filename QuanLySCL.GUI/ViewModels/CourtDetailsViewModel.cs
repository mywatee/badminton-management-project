using QuanLySCL.BUS;
using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace QuanLySCL.GUI.ViewModels
{
    public class CourtDetailsViewModel : BaseViewModel
    {
        private readonly BookingBUS _bookingBus = new BookingBUS();

        public Court Court { get; }

        private DateTime _selectedDate;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value.Date))
                    Load();
            }
        }

        public ObservableCollection<CourtScheduleItem> Bookings { get; set; } = new ObservableCollection<CourtScheduleItem>();

        public CourtDetailsViewModel(Court court)
        {
            Court = court;
            SelectedDate = DateTime.Today;
            Load();
        }

        public void Load()
        {
            if (Court == null)
            {
                Bookings = new ObservableCollection<CourtScheduleItem>();
                OnPropertyChanged(nameof(Bookings));
                return;
            }

            try
            {
                var all = _bookingBus.GetScheduleBookings(SelectedDate.Date, SelectedDate.Date);
                var filtered = all
                    .Where(b => string.Equals(b.CourtId, Court.Id, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(b => b.StartTime)
                    .ToList();

                Bookings = new ObservableCollection<CourtScheduleItem>(filtered);
                OnPropertyChanged(nameof(Bookings));
            }
            catch
            {
                Bookings = new ObservableCollection<CourtScheduleItem>();
                OnPropertyChanged(nameof(Bookings));
            }
        }
    }
}

