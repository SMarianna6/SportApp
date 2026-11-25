using System.ComponentModel.DataAnnotations;
using SportLab.Models;

namespace SportLab.Tests;

public class WorkoutSlotValidationTests
{
    [Fact]
    public void Validate_ValidSlot_ReturnsNoErrors()
    {
        // Arrange
        var start = DateTime.Today.AddDays(1).AddHours(10);
        var slot = CreateSlot(start, start.AddHours(1), capacity: 10, booked: 5);

        // Act
        var results = slot.Validate(new ValidationContext(slot)).ToList();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_EndTimeNotAfterStartTime_ReturnsTimeRangeError()
    {
        // Arrange
        var start = DateTime.Today.AddDays(1).AddHours(10);
        var slot = CreateSlot(start, start.AddMinutes(-30), capacity: 10, booked: 0);

        // Act
        var results = slot.Validate(new ValidationContext(slot)).ToList();

        // Assert
        var error = Assert.Single(results);
        Assert.Contains(nameof(WorkoutSlot.EndTime), error.MemberNames);
        Assert.Contains("End time", error.ErrorMessage);
    }

    [Fact]
    public void Validate_BookedExceedsCapacity_ReturnsCapacityError()
    {
        // Arrange
        var start = DateTime.Today.AddDays(1).AddHours(10);
        var slot = CreateSlot(start, start.AddHours(1), capacity: 5, booked: 6);

        // Act
        var results = slot.Validate(new ValidationContext(slot)).ToList();

        // Assert
        var error = Assert.Single(results);
        Assert.Contains(nameof(WorkoutSlot.Booked), error.MemberNames);
        Assert.Contains("capacity", error.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static WorkoutSlot CreateSlot(DateTime start, DateTime end, int capacity, int booked) => new()
    {
        ActivityName = "HIIT",
        TrainerId = 1,
        StartTime = start,
        EndTime = end,
        Capacity = capacity,
        Booked = booked,
        Status = WorkoutStatuses.Available
    };
}
