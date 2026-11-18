using System;
using Xunit;
using SportLab.Models;

namespace SportLab.Tests
{
    public class SimpleTests
    {
        [Fact]
        public void User_Email_ShouldNotBeEmpty()
        {
            var user = new User
            {
                Name = "John Doe",
                Email = "john@example.com",
                Password = "securePass123",
                Role = "Client"
            };

            Assert.True(!string.IsNullOrEmpty(user.Email));
            Assert.Equal("john@example.com", user.Email);
        }

        [Fact]
        public void Subscription_Duration_ShouldCalculateCorrectly()
        {
            var start = DateTime.Today;
            var end = DateTime.Today.AddDays(30);
            var subscription = new Subscription
            {
                StartDate = start,
                EndDate = end
            };

            var actualDays = (subscription.EndDate - subscription.StartDate).Days;

            Assert.Equal(30, actualDays);
        }

        [Fact]
        public void Plan_Price_ShouldBeSetCorrectly()
        {
            var plan = new Plan
            {
                Name = "Premium",
                Description = "All access",
                Price = 1500.50m,
                Duration = 30,
                Type = "Premium"
            };

            Assert.Equal(1500.50m, plan.Price);
            Assert.True(plan.Price > 0);
        }

        [Fact]
        public void WorkoutBooking_DefaultStatus_ShouldBeBooked()
        {
            var booking = new WorkoutBooking();

            Assert.Equal(BookingStatuses.Booked, booking.Status);
            Assert.True(booking.BookingDate <= DateTime.Now);
        }

        [Fact]
        public void Subscription_IsActive_ShouldBeTrueByDefault()
        {
            var subscription = new Subscription();

            Assert.True(subscription.IsActive);
            Assert.Equal(true, subscription.IsActive);
        }
    }
}