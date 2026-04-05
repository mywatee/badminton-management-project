using QuanLySCL.DAL;
using QuanLySCL.Models;
using System.Collections.ObjectModel;

namespace QuanLySCL.BUS
{
    public class AdminBUS
    {
        private readonly BookingDAL _bookingDal = new BookingDAL();

        // Time Slot Management
        public ObservableCollection<TimeSlot> GetAllTimeSlots() => _bookingDal.GetAllTimeSlots();
        public bool AddTimeSlot(TimeSlot slot, out string error) => _bookingDal.AddTimeSlot(slot, out error);
        public bool UpdateTimeSlot(TimeSlot slot, out string error) => _bookingDal.UpdateTimeSlot(slot, out error);
        public bool DeleteTimeSlot(string id, out string error) => _bookingDal.DeleteTimeSlot(id, out error);

        // Pricing Management
        public ObservableCollection<PriceEntry> GetAllPriceEntries() => _bookingDal.GetAllPriceEntries();
        public bool AddPriceEntry(PriceEntry entry, out string error) => _bookingDal.AddPriceEntry(entry, out error);
        public bool UpdatePriceEntry(PriceEntry entry, out string error) => _bookingDal.UpdatePriceEntry(entry, out error);
        public bool DeletePriceEntry(string id, out string error) => _bookingDal.DeletePriceEntry(id, out error);

        // Bulk generate / upsert
        public bool UpsertTimeSlotsAndPrices(
            System.Collections.Generic.IReadOnlyList<TimeSlot> slots,
            System.Collections.Generic.IReadOnlyList<PriceEntry> prices,
            bool overwriteExisting,
            out int slotsInserted,
            out int slotsUpdated,
            out int pricesInserted,
            out int pricesUpdated,
            out string error)
            => _bookingDal.UpsertTimeSlotsAndPrices(slots, prices, overwriteExisting, out slotsInserted, out slotsUpdated, out pricesInserted, out pricesUpdated, out error);
    }
}
