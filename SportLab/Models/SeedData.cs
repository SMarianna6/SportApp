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
                // Ensure database is created
                context.Database.EnsureCreated();

                // Look for any Plans according to Lab 4 requirements
                if (context.Plan.Any())
                {
                    return; // Database has already been seeded
                }

                // 1. Initial users (trainers and clients for slots and subscriptions)
                var trainer1 = new User
                {
                    Name = "Alex Kovalenko",
                    Email = "alex.trainer@sportlab.ua",
                    Password = "TrainerPass123!",
                    Role = "Trainer",
                    IsBlocked = false
                };

                var trainer2 = new User
                {
                    Name = "Marina Shevchenko",
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

                // 2. Initial membership plans (Plan) with English descriptions
                var plans = new List<Plan>
                {
                    new Plan
                    {
                        Name = "Basic Start",
                        Type = "Basic",
                        Price = 850.00m,
                        Duration = 30,
                        Description = "Morning and afternoon gym access (07:00 - 16:00). Introductory coach orientation, personal locker, and shower facilities."
                    },
                    new Plan
                    {
                        Name = "Unlimited Pro",
                        Type = "Popular",
                        Price = 1450.00m,
                        Duration = 30,
                        Description = "24/7 Unlimited access to all workout zones. All group classes included (CrossFit, TRX, Yoga), Finnish sauna, and jacuzzi."
                    },
                    new Plan
                    {
                        Name = "Student Drive",
                        Type = "Student",
                        Price = 650.00m,
                        Duration = 30,
                        Description = "Gym access between 09:00 - 18:00 with valid student ID. Group functional workouts and 10% discount at the nutrition bar."
                    },
                    new Plan
                    {
                        Name = "Annual VIP Unlimited",
                        Type = "Premium",
                        Price = 11900.00m,
                        Duration = 365,
                        Description = "All-inclusive 365-day pass with no restrictions. 5 personal training sessions, doctor assessment, and 15% store discount."
                    }
                };

                context.Plan.AddRange(plans);
                context.SaveChanges();

                // 3. Initial workout slots (WorkoutSlot)
                var tomorrow = DateTime.Today.AddDays(1);
                var workoutSlots = new List<WorkoutSlot>
                {
                    new WorkoutSlot
                    {
                        ActivityName = "CrossFit Intense",
                        TrainerId = trainer1.Id,
                        UserId = client1.Id,
                        StartTime = tomorrow.AddHours(10), // 10:00
                        EndTime = tomorrow.AddHours(11),   // 11:00
                        Status = "Booked"
                    },
                    new WorkoutSlot
                    {
                        ActivityName = "TRX Functional",
                        TrainerId = trainer1.Id,
                        UserId = null,
                        StartTime = tomorrow.AddHours(12), // 12:00
                        EndTime = tomorrow.AddHours(13),   // 13:00
                        Status = "Available"
                    },
                    new WorkoutSlot
                    {
                        ActivityName = "Morning Yoga & Stretching",
                        TrainerId = trainer2.Id,
                        UserId = client2.Id,
                        StartTime = tomorrow.AddHours(9),  // 09:00
                        EndTime = tomorrow.AddHours(10),  // 10:00
                        Status = "Booked"
                    },
                    new WorkoutSlot
                    {
                        ActivityName = "Pilates Core Balance",
                        TrainerId = trainer2.Id,
                        UserId = null,
                        StartTime = tomorrow.AddHours(17), // 17:00
                        EndTime = tomorrow.AddHours(18),   // 18:00
                        Status = "Available"
                    },
                    new WorkoutSlot
                    {
                        ActivityName = "Powerlifting Basics",
                        TrainerId = trainer1.Id,
                        UserId = null,
                        StartTime = tomorrow.AddHours(18).AddMinutes(30), // 18:30
                        EndTime = tomorrow.AddHours(19).AddMinutes(30),   // 19:30
                        Status = "Available"
                    }
                };

                context.WorkoutSlots.AddRange(workoutSlots);

                // 4. Initial customer subscriptions (Subscription)
                var subscriptions = new List<Subscription>
                {
                    new Subscription
                    {
                        UserId = client1.Id,
                        PlanId = plans[1].Id, // Unlimited Pro
                        StartDate = DateTime.Today,
                        EndDate = DateTime.Today.AddDays(plans[1].Duration),
                        IsActive = true
                    },
                    new Subscription
                    {
                        UserId = client2.Id,
                        PlanId = plans[0].Id, // Basic Start
                        StartDate = DateTime.Today.AddDays(-10),
                        EndDate = DateTime.Today.AddDays(20),
                        IsActive = true
                    }
                };

                context.Subscriptions.AddRange(subscriptions);

                // 5. Initial fitness metric logs (MetricLog)
                var metricLogs = new List<MetricLog>
                {
                    new MetricLog
                    {
                        UserId = client1.Id,
                        Weight = 78.5f,
                        RecordDate = DateTime.Today.AddDays(-7),
                        Notes = "Initial fitness assessment and training start"
                    },
                    new MetricLog
                    {
                        UserId = client1.Id,
                        Weight = 77.8f,
                        RecordDate = DateTime.Today,
                        Notes = "Progress after first week of cardio"
                    },
                    new MetricLog
                    {
                        UserId = client2.Id,
                        Weight = 59.2f,
                        RecordDate = DateTime.Today,
                        Notes = "Post-yoga measurement session"
                    }
                };

                context.MetricLogs.AddRange(metricLogs);

                context.SaveChanges();
            }
        }
    }
}
