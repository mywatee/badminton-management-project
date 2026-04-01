using QuanLySCL.DAL;
using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace QuanLySCL.BUS
{
    public class BookingBUS
    {
        private readonly BookingDAL _bookingDal = new BookingDAL();

        public ObservableCollection<Booking> GetAllBookings()
        {
            return _bookingDal.GetAllBookings();
        }

        public int CountActiveCustomersToday()
        {
            return _bookingDal.CountActiveCustomersToday();
        }

        public ObservableCollection<Booking> GetBookingsByCustomerId(string customerId)
        {
            return _bookingDal.GetBookingsByCustomerId(customerId);
        }

        public ObservableCollection<TimeSlot> GetAllTimeSlots()
        {
            return _bookingDal.GetAllTimeSlots();
        }

        public ObservableCollection<CourtScheduleItem> GetScheduleBookings(DateTime startDate, DateTime endDate)
        {
            return _bookingDal.GetScheduleBookings(startDate, endDate);
        }

        public bool CreateBooking(
            string customerId,
            string courtId,
            string slotId,
            DateTime usageDate,
            string bookingTypeVN,
            List<(string serviceId, int quantity, decimal price)> selectedServices,
            out string bookingId,
            out string error)
        {
            return _bookingDal.CreateBookingWithDetail(customerId, courtId, slotId, usageDate, bookingTypeVN, selectedServices, out bookingId, out error);
        }

        public bool IsCourtSlotFree(string courtId, string slotId, DateTime usageDate)
        {
            return _bookingDal.IsCourtSlotFree(courtId, slotId, usageDate);
        }

        public decimal GetPriceForCourtSlot(string courtId, string slotId, string bookingType)
        {
            return _bookingDal.GetPriceForCourtSlot(courtId, slotId, bookingType);
        }
        public (decimal dailyRevenue, int activeBookings, decimal monthlyGrowth) GetBookingStats()
        {
            return _bookingDal.GetBookingStats();
        }

        public Booking GetBookingById(string id)
        {
            return _bookingDal.GetBookingById(id);
        }

        public Booking GetActiveBookingByCourt(string courtId)
        {
            return _bookingDal.GetActiveBookingByCourt(courtId);
        }

        public ObservableCollection<Booking> GetActiveBookings()
        {
            return _bookingDal.GetActiveBookings();
        }


        public bool PerformCheckIn(string bookingId, out string error)
        {
            error = null;
            var booking = _bookingDal.GetBookingById(bookingId);
            if (booking == null)
            {
                error = "Không tìm thấy thông tin đặt sân.";
                return false;
            }

            // 1. Update Booking Status
            if (!_bookingDal.UpdateBookingStatus(bookingId, "Checked-in", out error))
                return false;

            // 2. Update Court Status
            var courtDal = new CourtDAL();
            if (courtDal.UpdateCourtStatus(booking.CourtId, "In-use") <= 0)
            {
                error = "Không thể cập nhật trạng thái sân.";
                return false;
            }

            return true;
        }

        public bool PerformCheckOut(Invoice invoice, out string error)
        {
            error = null;
            var booking = _bookingDal.GetBookingById(invoice.BookingId);
            if (booking == null)
            {
                error = "Không tìm thấy thông tin đặt sân.";
                return false;
            }

            return _bookingDal.CheckOutWithTransaction(invoice, booking.CourtId, out error);
        }

        public bool UpdateBookingStatus(string bookingId, string statusEn, out string error)
        {
            return _bookingDal.UpdateBookingStatus(bookingId, statusEn, out error);
        }

        public void AutoCompleteOverdueBookings()
        {
            _bookingDal.AutoCompleteOverdueBookings();
        }
    }
}
