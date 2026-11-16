using System;
using Xunit;
using SportLab.Models;

namespace SportLab.Tests
{
    public class SimpleTests
    {
        // 1. Перевірка, що Email користувача не порожній і зберігається правильно
        [Fact]
        public void User_Email_ShouldNotBeEmpty()
        {
            // Arrange
            var user = new User
            {
                Name = "John Doe",
                Email = "john@example.com",
                Password = "securePass123",
                Role = "Client"
            };

            // Act & Assert
            Assert.True(!string.IsNullOrEmpty(user.Email));
            Assert.Equal("john@example.com", user.Email);
        }

        // 2. Перевірка розрахунку тривалості підписки (різниця днів)
        [Fact]
        public void Subscription_Duration_ShouldCalculateCorrectly()
        {
            // Arrange
            var start = DateTime.Today;
            var end = DateTime.Today.AddDays(30);
            var subscription = new Subscription
            {
                StartDate = start,
                EndDate = end
            };

            // Act
            var actualDays = (subscription.EndDate - subscription.StartDate).Days;

            // Assert
            Assert.Equal(30, actualDays);
        }

        // 3. Перевірка, що новий тариф створюється з правильною ціною
        [Fact]
        public void Plan_Price_ShouldBeSetCorrectly()
        {
            // Arrange
            var plan = new Plan
            {
                Name = "Premium",
                Description = "All access",
                Price = 1500.50m,
                Duration = 30,
                Type = "Premium"
            };

            // Act & Assert
            Assert.Equal(1500.50m, plan.Price);
            Assert.True(plan.Price > 0);
        }

        // 4. Перевірка, що за замовчуванням бронювання має статус "Booked"
        [Fact]
        public void WorkoutBooking_DefaultStatus_ShouldBeBooked()
        {
            // Arrange
            var booking = new WorkoutBooking();

            // Act & Assert
            Assert.Equal(BookingStatuses.Booked, booking.Status);
            Assert.True(booking.BookingDate <= DateTime.Now); // Дата створення не в майбутньому
        }

        // 5. Перевірка, що підписка за замовчуванням є активною
        [Fact]
        public void Subscription_IsActive_ShouldBeTrueByDefault()
        {
            // Arrange
            var subscription = new Subscription();

            // Act & Assert
            Assert.True(subscription.IsActive);
            Assert.Equal(true, subscription.IsActive); // Додаткова перевірка через Equal
        }
    }
}