using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Controllers;
using SportLab.Data;
using SportLab.Models;

namespace SportLab.Tests;

public class ScheduleControllerTests
{
    [Fact]
    public async Task BookJson_ClientWithActiveSubscription_CreatesBookingAndMarksLastSpotBooked()
    {
        // Arrange
        using var context = TestInfrastructure.CreateContext(Guid.NewGuid().ToString());
        var (client, trainer) = await SeedUsersAsync(context);
        await SeedSubscriptionAsync(context, client.Id);
        var slot = await SeedSlotAsync(context, trainer.Id, capacity: 1);

        var controller = CreateController(context, client.Id);

        // Act
        var result = await controller.BookJson(slot.Id);

        // Assert
        Assert.IsType<JsonResult>(result);

        context.ChangeTracker.Clear();
        var booking = await context.WorkoutBookings.SingleAsync();
        Assert.Equal(client.Id, booking.UserId);
        Assert.Equal(slot.Id, booking.WorkoutSlotId);

        var updatedSlot = await context.WorkoutSlots.FindAsync(slot.Id);
        Assert.NotNull(updatedSlot);
        Assert.Equal(1, updatedSlot!.Booked);
        Assert.Equal(WorkoutStatuses.Booked, updatedSlot.Status);
    }

    [Fact]
    public async Task BookJson_DuplicateBooking_ReturnsBadRequestAndKeepsSingleBooking()
    {
        // Arrange
        using var context = TestInfrastructure.CreateContext(Guid.NewGuid().ToString());
        var (client, trainer) = await SeedUsersAsync(context);
        await SeedSubscriptionAsync(context, client.Id);
        var slot = await SeedSlotAsync(context, trainer.Id, capacity: 3);

        context.WorkoutBookings.Add(new WorkoutBooking
        {
            WorkoutSlotId = slot.Id,
            UserId = client.Id,
            BookingDate = DateTime.Now,
            Status = BookingStatuses.Booked
        });
        slot.Booked = 1;
        await context.SaveChangesAsync();

        var controller = CreateController(context, client.Id);

        // Act
        var result = await controller.BookJson(slot.Id);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);

        context.ChangeTracker.Clear();
        Assert.Equal(1, await context.WorkoutBookings.CountAsync());
    }

    [Fact]
    public async Task BookJson_ClientWithoutActiveSubscription_ReturnsBadRequestAndDoesNotBook()
    {
        // Arrange
        using var context = TestInfrastructure.CreateContext(Guid.NewGuid().ToString());
        var (client, trainer) = await SeedUsersAsync(context);
        var slot = await SeedSlotAsync(context, trainer.Id, capacity: 2);

        var controller = CreateController(context, client.Id);

        // Act
        var result = await controller.BookJson(slot.Id);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);

        context.ChangeTracker.Clear();
        Assert.Empty(context.WorkoutBookings);

        var unchangedSlot = await context.WorkoutSlots.FindAsync(slot.Id);
        Assert.NotNull(unchangedSlot);
        Assert.Equal(0, unchangedSlot!.Booked);
    }

    private static ScheduleController CreateController(SportLabContext context, int userId)
    {
        var controllerContext = TestInfrastructure.CreateControllerContext(userId, out _);
        var localizer = TestInfrastructure.CreateLocalizer<ScheduleController>();

        return new ScheduleController(context, localizer.Object)
        {
            ControllerContext = controllerContext
        };
    }

    private static async Task<(User Client, User Trainer)> SeedUsersAsync(SportLabContext context)
    {
        var suffix = Guid.NewGuid().ToString("N");

        var client = new User
        {
            Name = "Test Client",
            Email = $"client-{suffix}@example.com",
            Password = "ClientPass123!",
            Role = Roles.Client
        };

        var trainer = new User
        {
            Name = "Test Trainer",
            Email = $"trainer-{suffix}@example.com",
            Password = "TrainerPass123!",
            Role = Roles.Trainer
        };

        context.Users.AddRange(client, trainer);
        await context.SaveChangesAsync();

        return (client, trainer);
    }

    private static async Task SeedSubscriptionAsync(SportLabContext context, int userId)
    {
        context.Subscriptions.Add(new Subscription
        {
            UserId = userId,
            PlanId = 1,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true
        });

        await context.SaveChangesAsync();
    }

    private static async Task<WorkoutSlot> SeedSlotAsync(SportLabContext context, int trainerId, int capacity)
    {
        var start = DateTime.Now.AddDays(1);

        var slot = new WorkoutSlot
        {
            ActivityName = "HIIT",
            TrainerId = trainerId,
            StartTime = start,
            EndTime = start.AddHours(1),
            Capacity = capacity,
            Booked = 0,
            Status = WorkoutStatuses.Available
        };

        context.WorkoutSlots.Add(slot);
        await context.SaveChangesAsync();

        return slot;
    }
}
