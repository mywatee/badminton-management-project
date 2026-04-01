using System.Collections.ObjectModel;
using System.Linq;
using QuanLySCL.BUS;
using QuanLySCL.Models;
using System.Collections.Generic;

namespace QuanLySCL.GUI.ViewModels
{
    public class PricingAdminViewModel : BaseViewModel
    {
        private readonly AdminBUS _adminBus = new AdminBUS();
        private readonly CourtBUS _courtBus = new CourtBUS();

        private ObservableCollection<CourtType> _courtTypes;
        public ObservableCollection<CourtType> CourtTypes
        {
            get => _courtTypes;
            set => SetProperty(ref _courtTypes, value);
        }

        private CourtType _selectedCourtType;
        public CourtType SelectedCourtType
        {
            get => _selectedCourtType;
            set
            {
                if (SetProperty(ref _selectedCourtType, value))
                {
                    FilterEntries();
                }
            }
        }

        private ObservableCollection<CompactPriceEntry> _allEntries;
        private ObservableCollection<CompactPriceEntry> _filteredEntries;
        public ObservableCollection<CompactPriceEntry> FilteredEntries
        {
            get => _filteredEntries;
            set => SetProperty(ref _filteredEntries, value);
        }

        private CompactPriceEntry _selectedEntry;
        public CompactPriceEntry SelectedEntry
        {
            get => _selectedEntry;
            set => SetProperty(ref _selectedEntry, value);
        }

        public PricingAdminViewModel()
        {
            Load();
        }

        public void Load()
        {
            // Load Court Types for Tabs
            CourtTypes = _courtBus.GetCourtTypes();
            if (SelectedCourtType == null && CourtTypes.Any())
            {
                _selectedCourtType = CourtTypes.First();
                OnPropertyChanged(nameof(SelectedCourtType));
            }

            var rawEntries = _adminBus.GetAllPriceEntries();
            
            var grouped = rawEntries
                .GroupBy(e => new { e.CourtTypeId, e.SlotId })
                .Select(g => new CompactPriceEntry
                {
                    CourtTypeId = g.Key.CourtTypeId,
                    CourtTypeName = g.First().CourtTypeName,
                    SlotId = g.Key.SlotId,
                    SlotName = g.First().SlotName,
                    
                    PriceLe = g.FirstOrDefault(e => e.BookingType == "Lẻ")?.Price ?? 0,
                    IdLe = g.FirstOrDefault(e => e.BookingType == "Lẻ")?.Id,
                    
                    PriceFixed = g.FirstOrDefault(e => e.BookingType == "Cố định")?.Price ?? 0,
                    IdFixed = g.FirstOrDefault(e => e.BookingType == "Cố định")?.Id
                })
                .OrderBy(c => c.SlotId); // Sort by slot time/id within the group

            _allEntries = new ObservableCollection<CompactPriceEntry>(grouped);
            FilterEntries();
        }

        private void FilterEntries()
        {
            if (SelectedCourtType == null)
            {
                FilteredEntries = new ObservableCollection<CompactPriceEntry>();
                return;
            }

            var filtered = _allEntries
                .Where(e => e.CourtTypeId == SelectedCourtType.Id)
                .OrderBy(e => e.SlotId);
            
            FilteredEntries = new ObservableCollection<CompactPriceEntry>(filtered);
        }
    }
}
