using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportLab.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SportLab.Models
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new SportLabContext(
                serviceProvider.GetRequiredService<DbContextOptions<SportLabContext>>()))
            {
                context.Database.EnsureCreated();

                if (context.Plans.Any())
                {
                    return;
                }

                var trainer1 = new User
                {
                    Name = "Alexander Kovalenko",
                    Email = "alex.trainer@sportlab.ua",
                    Password = "TrainerPass123!",
                    Role = "Trainer",
                    IsBlocked = false
                };

                var trainer2 = new User
                {
                    Name = "Maryna Shevchenko",
                    Email = "marina.trainer@sportlab.ua",
                    Password = "TrainerPass123!",
                    Role = "Trainer",
                    IsBlocked = false
                };

                var client1 = new User
                {
                    Name = "Ivan Bondarenko",
                    Email = "ivan.client@sportlab.ua",
                    Password = "ClientPass123!",
                    Role = "Client",
                    IsBlocked = false
                };

                var client2 = new User
                {
                    Name = "Olena Melnyk",
                    Email = "olena.client@sportlab.ua",
                    Password = "ClientPass123!",
                    Role = "Client",
                    IsBlocked = false
                };

                context.Users.AddRange(trainer1, trainer2, client1, client2);
                context.SaveChanges();

                var plans = new List<Plan>
                {
                    new Plan
                    {
                        Name = "Basic Start",
                        Type = "Basic",
                        Price = 850.00m,
                        Duration = 30,
                        Description = "Morning and day access (07:00 - 16:00). Intro trainer session, personal locker and showers."
                    },
                    new Plan
                    {
                        Name = "Unlimited Pro",
                        Type = "Popular",
                        Price = 1450.00m,
                        Duration = 30,
                        Description = "24/7 unlimited access to all zones. All group classes, sauna and jacuzzi."
                    },
                    new Plan
                    {
                        Name = "Student Drive",
                        Type = "Student",
                        Price = 650.00m,
                        Duration = 30,
                        Description = "Access from 09:00 to 18:00 with a student ID. Functional training sessions and a 10% discount at the fitness bar."
                    },
                    new Plan
                    {
                        Name = "Annual VIP Unlimited",
                        Type = "Premium",
                        Price = 11900.00m,
                        Duration = 365,
                        Description = "Full annual membership with no restrictions. 5 personal training sessions, a consultation with a nutritionist, and a 15% discount in the store."
                    }
                };

                context.Plans.AddRange(plans);
                context.SaveChanges();

                var tomorrow = DateTime.Today.AddDays(1);
                var workoutSlots = new List<WorkoutSlot>
                {
                    new WorkoutSlot
                    {
                        ActivityName = "CrossFit Intense",
                        TrainerId = trainer1.Id,
                        UserId = client1.Id,
                        StartTime = tomorrow.AddHours(10),
                        EndTime = tomorrow.AddHours(11),
                        Status = "Booked"
                    },
                    new WorkoutSlot
                    {
                        ActivityName = "TRX Functional",
                        TrainerId = trainer1.Id,
                        UserId = null,
                        StartTime = tomorrow.AddHours(12),
                        EndTime = tomorrow.AddHours(13),
                        Status = "Available"
                    },
                    new WorkoutSlot
                    {
                        ActivityName = "Morning Yoga & Stretching",
                        TrainerId = trainer2.Id,
                        UserId = client2.Id,
                        StartTime = tomorrow.AddHours(9),
                        EndTime = tomorrow.AddHours(10),
                        Status = "Booked"
                    },
                    new WorkoutSlot
                    {
                        ActivityName = "Pilates Core Balance",
                        TrainerId = trainer2.Id,
                        UserId = null,
                        StartTime = tomorrow.AddHours(17),
                        EndTime = tomorrow.AddHours(18),
                        Status = "Available"
                    },
                    new WorkoutSlot
                    {
                        ActivityName = "Powerlifting Basics",
                        TrainerId = trainer1.Id,
                        UserId = null,
                        StartTime = tomorrow.AddHours(18).AddMinutes(30),
                        EndTime = tomorrow.AddHours(19).AddMinutes(30),
                        Status = "Available"
                    }
                };

                context.WorkoutSlots.AddRange(workoutSlots);

                var subscriptions = new List<Subscription>
                {
                    new Subscription
                    {
                        UserId = client1.Id,
                        PlanId = plans[1].Id,
                        StartDate = DateTime.Today,
                        EndDate = DateTime.Today.AddDays(plans[1].Duration),
                        IsActive = true
                    },
                    new Subscription
                    {
                        UserId = client2.Id,
                        PlanId = plans[0].Id,
                        StartDate = DateTime.Today.AddDays(-10),
                        EndDate = DateTime.Today.AddDays(20),
                        IsActive = true
                    }
                };

                context.Subscriptions.AddRange(subscriptions);

                var metricLogs = new List<MetricLog>
                {
                    new MetricLog
                    {
                        UserId = client1.Id,
                        Weight = 78.5f,
                        RecordDate = DateTime.Today.AddDays(-7),
                        Notes = "Start of the training cycle"
                    },
                    new MetricLog
                    {
                        UserId = client1.Id,
                        Weight = 77.8f,
                        RecordDate = DateTime.Today,
                        Notes = "Progress after the first week of cardio"
                    },
                    new MetricLog
                    {
                        UserId = client2.Id,
                        Weight = 59.2f,
                        RecordDate = DateTime.Today,
                        Notes = "Check-in weighing after a yoga session"
                    }
                };

                context.MetricLogs.AddRange(metricLogs);

                context.SaveChanges();
            }
        }
    }
}
