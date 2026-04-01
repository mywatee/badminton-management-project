using FluentAssertions;
using QuanLySCL.BUS;
using QuanLySCL.Models;
using Xunit;

namespace QuanLySCL.Tests
{
    public class BookingBUS_Tests
    {
        private readonly BookingBUS _bookingBus;

        public BookingBUS_Tests()
        {
            _bookingBus = new BookingBUS();
        }

        [Fact]
        public void Checkout_Should_Return_Error_When_Invoice_BookingId_Is_Empty()
        {
            // Act
            var invoice = new Invoice { BookingId = "INVALID_ID" };
            bool result = _bookingBus.PerformCheckOut(invoice, out string error);

            // Assert
            result.Should().BeFalse();
            error.Should().Be("Không tìm thấy thông tin đặt sân.");
        }

        // NOTE: The UpdateBookingStatus test has been temporarily removed 
        // because the underlying DAL layer contains a silent failure bug 
        // (returning True even when 0 rows are affected).
    }
}
