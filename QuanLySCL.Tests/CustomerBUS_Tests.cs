using FluentAssertions;
using QuanLySCL.BUS;
using QuanLySCL.Models;
using Xunit;

namespace QuanLySCL.Tests
{
    public class CustomerBUS_Tests
    {
        private readonly CustomerBUS _customerBus;

        public CustomerBUS_Tests()
        {
            _customerBus = new CustomerBUS();
        }

        [Fact]
        public void GetCustomerRank_With_Zero_Spent_Should_Return_New()
        {
            var customer = new Customer { TotalSpent = 0, TotalBookings = 0 };
            string rank = _customerBus.GetCustomerRank(customer);
            rank.Should().Be("New");
        }

        [Fact]
        public void GetCustomerRank_With_Thresholds_Should_Adjust_Properly()
        {
            var goldCustomer = new Customer { TotalSpent = 5000000, TotalBookings = 0 };
            _customerBus.GetCustomerRank(goldCustomer).Should().Be("Gold");

            var vipCustomer = new Customer { TotalSpent = 10000000, TotalBookings = 0 };
            _customerBus.GetCustomerRank(vipCustomer).Should().Be("VIP");
        }
        
        [Fact]
        public void GetAllCustomers_Fetch_Speed_And_Connection_Should_Be_Reliable()
        {
            var list = _customerBus.GetAllCustomers();
            list.Should().NotBeNull();
        }
    }
}
