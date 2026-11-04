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
                try
                {
                    if (context.Database.CanConnect())
                    {
                        context.Database.ExecuteSqlRaw("SELECT TOP 1 Category, Rating FROM Products");
                        context.Database.ExecuteSqlRaw("SELECT TOP 1 ActivityName, UserId FROM WorkoutSlots");
                    }
                }
                catch
                {
                    context.Database.EnsureDeleted();
                }

                context.Database.EnsureCreated();

                // Lab 6: administrator and moderator accounts must exist even on an
                // already seeded database, so the role scenarios can be demonstrated.
                EnsureStaffAccounts(context);

                if (context.Plan.Any() && context.Products.Any())
                {
                    return;
                }

                if (!context.Users.Any(u => u.Email == "alex.trainer@sportlab.ua"))
                {
                    var trainer1 = new User { Name = "Alexander Kovalenko", Email = "alex.trainer@sportlab.ua", Password = "TrainerPass123!", Role = Roles.Trainer, IsBlocked = false };
                    var trainer2 = new User { Name = "Maryna Shevchenko", Email = "marina.trainer@sportlab.ua", Password = "TrainerPass123!", Role = Roles.Trainer, IsBlocked = false };
                    var trainer3 = new User { Name = "Maksym Tkachenko", Email = "maksym@sportlab.ua", Password = "Pass123!", Role = Roles.Trainer, IsBlocked = false };
                    var trainer4 = new User { Name = "Oksana Koval", Email = "oksana@sportlab.ua", Password = "Pass123!", Role = Roles.Trainer, IsBlocked = false };
                    var trainer5 = new User { Name = "Dmytro Boyko", Email = "dmytro@sportlab.ua", Password = "Pass123!", Role = Roles.Trainer, IsBlocked = false };

                    var client1 = new User { Name = "Ivan Bondarenko", Email = "ivan.client@sportlab.ua", Password = "ClientPass123!", Role = Roles.Client, IsBlocked = false };
                    var client2 = new User { Name = "Olena Melnyk", Email = "olena.client@sportlab.ua", Password = "ClientPass123!", Role = Roles.Client, IsBlocked = false };

                    context.Users.AddRange(trainer1, trainer2, trainer3, trainer4, trainer5, client1, client2);
                    context.SaveChanges();
                }

                if (!context.Plan.Any())
                {
                    var plans = new List<Plan>
                    {
                        new Plan
                        {
                            Name = "Basic Start", Type = "Basic", Price = 850.00m, Duration = 30,
                            Description = "Morning and day access (07:00 - 16:00). Intro trainer session, personal locker and showers."
                        },
                        new Plan
                        {
                            Name = "Unlimited Pro", Type = "Popular", Price = 1450.00m, Duration = 30,
                            Description = "24/7 unlimited access to all zones. All group classes, sauna and jacuzzi."
                        },
                        new Plan
                        {
                            Name = "Student Drive", Type = "Student", Price = 650.00m, Duration = 30,
                            Description = "Access from 09:00 to 18:00 with a student ID. Functional training sessions and a 10% discount at the fitness bar."
                        },
                        new Plan
                        {
                            Name = "Annual VIP Unlimited", Type = "Premium", Price = 11900.00m, Duration = 365,
                            Description = "Full annual membership with no restrictions. 5 personal training sessions, a consultation with a nutritionist, and a 15% discount in the store."
                        }
                    };
                    context.Plans.AddRange(plans);
                    context.SaveChanges();

                    var trainerUsers = context.Users.Where(u => u.Role == Roles.Trainer).ToList();
                    var clientUsers = context.Users.Where(u => u.Role == Roles.Client).ToList();

                    if (trainerUsers.Count >= 5 && clientUsers.Count >= 2)
                    {
                        var today = DateTime.Today;
                        var currentDayOfWeek = (int)today.DayOfWeek;
                        var startOfWeek = today.AddDays(currentDayOfWeek == 0 ? -6 : -(currentDayOfWeek - 1));

                        var workoutSlots = new List<WorkoutSlot>
                        {
                            new WorkoutSlot { ActivityName = "HIIT", TrainerId = trainerUsers[0].Id, StartTime = startOfWeek.AddHours(8), EndTime = startOfWeek.AddHours(9), Status = "Available" },
                            new WorkoutSlot { ActivityName = "Yoga", TrainerId = trainerUsers[1].Id, UserId = clientUsers[0].Id, StartTime = startOfWeek.AddHours(10), EndTime = startOfWeek.AddHours(11), Status = "Booked" },
                            new WorkoutSlot { ActivityName = "Strength", TrainerId = trainerUsers[2].Id, StartTime = startOfWeek.AddHours(17), EndTime = startOfWeek.AddHours(18), Status = "Available" },
                            new WorkoutSlot { ActivityName = "Pilates", TrainerId = trainerUsers[3].Id, StartTime = startOfWeek.AddDays(1).AddHours(9), EndTime = startOfWeek.AddDays(1).AddHours(10), Status = "Available" },
                            new WorkoutSlot { ActivityName = "Boxing", TrainerId = trainerUsers[4].Id, StartTime = startOfWeek.AddDays(1).AddHours(18), EndTime = startOfWeek.AddDays(1).AddHours(19), Status = "Available" },
                            new WorkoutSlot { ActivityName = "Cycling", TrainerId = trainerUsers[0].Id, UserId = clientUsers[1].Id, StartTime = startOfWeek.AddDays(1).AddHours(19), EndTime = startOfWeek.AddDays(1).AddHours(20), Status = "Booked" },
                            new WorkoutSlot { ActivityName = "Yoga", TrainerId = trainerUsers[1].Id, StartTime = startOfWeek.AddDays(2).AddHours(8), EndTime = startOfWeek.AddDays(2).AddHours(9), Status = "Available" },
                            new WorkoutSlot { ActivityName = "HIIT", TrainerId = trainerUsers[0].Id, StartTime = startOfWeek.AddDays(2).AddHours(12), EndTime = startOfWeek.AddDays(2).AddHours(13), Status = "Available" },
                            new WorkoutSlot { ActivityName = "Strength", TrainerId = trainerUsers[2].Id, StartTime = startOfWeek.AddDays(2).AddHours(18), EndTime = startOfWeek.AddDays(2).AddHours(19), Status = "Available" },
                            new WorkoutSlot { ActivityName = "Cycling", TrainerId = trainerUsers[0].Id, StartTime = startOfWeek.AddDays(3).AddHours(10), EndTime = startOfWeek.AddDays(3).AddHours(11), Status = "Available" },
                            new WorkoutSlot { ActivityName = "Pilates", TrainerId = trainerUsers[3].Id, StartTime = startOfWeek.AddDays(3).AddHours(16), EndTime = startOfWeek.AddDays(3).AddHours(17), Status = "Available" },
                            new WorkoutSlot { ActivityName = "Boxing", TrainerId = trainerUsers[4].Id, UserId = clientUsers[0].Id, StartTime = startOfWeek.AddDays(3).AddHours(19), EndTime = startOfWeek.AddDays(3).AddHours(20), Status = "Booked" },
                            new WorkoutSlot { ActivityName = "Strength", TrainerId = trainerUsers[2].Id, StartTime = startOfWeek.AddDays(4).AddHours(9), EndTime = startOfWeek.AddDays(4).AddHours(10), Status = "Available" },
                            new WorkoutSlot { ActivityName = "Yoga", TrainerId = trainerUsers[1].Id, StartTime = startOfWeek.AddDays(4).AddHours(13), EndTime = startOfWeek.AddDays(4).AddHours(14), Status = "Available" },
                            new WorkoutSlot { ActivityName = "HIIT", TrainerId = trainerUsers[0].Id, StartTime = startOfWeek.AddDays(4).AddHours(18), EndTime = startOfWeek.AddDays(4).AddHours(19), Status = "Available" },
                            new WorkoutSlot { ActivityName = "Cycling", TrainerId = trainerUsers[0].Id, UserId = clientUsers[1].Id, StartTime = startOfWeek.AddDays(5).AddHours(10), EndTime = startOfWeek.AddDays(5).AddHours(11), Status = "Booked" },
                            new WorkoutSlot { ActivityName = "Boxing", TrainerId = trainerUsers[4].Id, StartTime = startOfWeek.AddDays(5).AddHours(12), EndTime = startOfWeek.AddDays(5).AddHours(13), Status = "Available" },
                            new WorkoutSlot { ActivityName = "Yoga", TrainerId = trainerUsers[1].Id, StartTime = startOfWeek.AddDays(6).AddHours(11), EndTime = startOfWeek.AddDays(6).AddHours(12), Status = "Available" },
                            new WorkoutSlot { ActivityName = "Pilates", TrainerId = trainerUsers[3].Id, StartTime = startOfWeek.AddDays(6).AddHours(14), EndTime = startOfWeek.AddDays(6).AddHours(15), Status = "Available" }
                        };
                        context.WorkoutSlots.AddRange(workoutSlots);

                        var subscriptions = new List<Subscription>
                        {
                            new Subscription { UserId = clientUsers[0].Id, PlanId = plans[1].Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(plans[1].Duration), IsActive = true },
                            new Subscription { UserId = clientUsers[1].Id, PlanId = plans[0].Id, StartDate = DateTime.Today.AddDays(-10), EndDate = DateTime.Today.AddDays(20), IsActive = true }
                        };
                        context.Subscriptions.AddRange(subscriptions);

                        var metricLogs = new List<MetricLog>
                        {
                            new MetricLog { UserId = clientUsers[0].Id, Weight = 78.5f, RecordDate = DateTime.Today.AddDays(-7), Notes = "Start of the training cycle" },
                            new MetricLog { UserId = clientUsers[0].Id, Weight = 77.8f, RecordDate = DateTime.Today, Notes = "Progress after the first week of cardio" },
                            new MetricLog { UserId = clientUsers[1].Id, Weight = 59.2f, RecordDate = DateTime.Today, Notes = "Check-in weighing after a yoga session" }
                        };
                        context.MetricLogs.AddRange(metricLogs);

                        context.SaveChanges();
                    }
                }

                if (!context.Products.Any())
                {
                    var products = new List<Product>
                    {
                        new Product
                        {
                            Name = "Optimum 100% Whey Gold Standard",
                            Category = "Nutrition",
                            Description = "24g of high-quality whey protein per serving with whey protein isolate as the primary ingredient. Double Rich Chocolate flavor, 2.27kg tub.",
                            Price = 1850.00m,
                            StockQuantity = 28,
                            Rating = "5.0",
                            PhotoUrl = "https://images.unsplash.com/photo-1579722821273-0f6c7d44362f?w=600&h=450&fit=crop"
                        },
                        new Product
                        {
                            Name = "Essential BCAA Energy 2:1:1",
                            Category = "Nutrition",
                            Description = "Instantized Branched Chain Amino Acids with electrolytes for sustained muscle endurance and post-workout recovery. Green Apple flavor, 400g.",
                            Price = 720.00m,
                            StockQuantity = 35,
                            Rating = "4.8",
                            PhotoUrl = "https://images.unsplash.com/photo-1593095948071-474c5cc2989d?w=600&h=450&fit=crop"
                        },
                        new Product
                        {
                            Name = "SportLab Pro Shaker 750ml",
                            Category = "Accessories",
                            Description = "BPA-free leak-proof fitness shaker with stainless steel blending ball and dual measurement scales (ml/oz).",
                            Price = 380.00m,
                            StockQuantity = 60,
                            Rating = "4.9",
                            PhotoUrl = "https://images.unsplash.com/photo-1517838277536-f5f99be501cd?w=600&h=450&fit=crop"
                        },
                        new Product
                        {
                            Name = "Pro Lifting Straps Cotton",
                            Category = "Accessories",
                            Description = "Heavy-duty cotton wrist straps with neoprene wrist padding for maximum grip support during heavy deadlifts and pulls.",
                            Price = 290.00m,
                            StockQuantity = 45,
                            Rating = "4.7",
                            PhotoUrl = "https://images.unsplash.com/photo-1584735935682-2f2b69dff9d2?w=600&h=450&fit=crop"
                        },
                        new Product
                        {
                            Name = "SportLab Performance Hoodie",
                            Category = "Apparel",
                            Description = "Breathable athletic fleece pullover engineered for pre-workout warm-ups and everyday athletic comfort. Unisex fit.",
                            Price = 1650.00m,
                            StockQuantity = 20,
                            Rating = "4.9",
                            PhotoUrl = "https://images.unsplash.com/photo-1556906781-9a412961c28c?w=600&h=450&fit=crop"
                        },
                        new Product
                        {
                            Name = "Training Gym Shorts",
                            Category = "Apparel",
                            Description = "Ultralight quick-dry shorts with built-in compression liner and zippered phone pocket. Optimized for high-mobility training.",
                            Price = 890.00m,
                            StockQuantity = 30,
                            Rating = "4.6",
                            PhotoUrl = "https://images.unsplash.com/photo-1591195853828-11db59a44f6b?w=600&h=450&fit=crop"
                        },
                        new Product
                        {
                            Name = "Resistance Bands Set 5-Pack",
                            Category = "Equipment",
                            Description = "Premium natural latex loop resistance bands spanning 5 progressive resistance levels (X-Light to X-Heavy) with carrying pouch.",
                            Price = 650.00m,
                            StockQuantity = 25,
                            Rating = "4.8",
                            PhotoUrl = "https://images.unsplash.com/photo-1598289431512-b97b0917affc?w=600&h=450&fit=crop"
                        },
                        new Product
                        {
                            Name = "Hex Rubber Dumbbells 10kg Pair",
                            Category = "Equipment",
                            Description = "Commercial-grade cast iron hex dumbbells encased in virgin rubber to protect gym floors and reduce impact noise.",
                            Price = 1950.00m,
                            StockQuantity = 15,
                            Rating = "5.0",
                            PhotoUrl = "https://images.unsplash.com/photo-1638536532686-d610adfc8e5c?w=600&h=450&fit=crop"
                        }
                    };
                    context.Products.AddRange(products);
                    context.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Creates the accounts used to demonstrate the Lab 6 role scenarios.
        /// The method is idempotent: it only adds an account when that role is missing.
        /// </summary>
        private static void EnsureStaffAccounts(SportLabContext context)
        {
            if (!context.Users.Any(u => u.Email == "admin@sportlab.ua"))
            {
                context.Users.Add(new User
                {
                    Name = "SportLab Administrator",
                    Email = "admin@sportlab.ua",
                    Password = "AdminPass123!",
                    Role = Roles.Administrator,
                    IsBlocked = false
                });
            }

            if (!context.Users.Any(u => u.Email == "moderator@sportlab.ua"))
            {
                context.Users.Add(new User
                {
                    Name = "SportLab Moderator",
                    Email = "moderator@sportlab.ua",
                    Password = "ModeratorPass123!",
                    Role = Roles.Moderator,
                    IsBlocked = false
                });
            }

            // Records created before Lab 6 may still store the legacy "Admin" value.
            foreach (var legacyAdmin in context.Users.Where(u => u.Role == Roles.AdminAlias).ToList())
            {
                legacyAdmin.Role = Roles.Administrator;
            }

            context.SaveChanges();
        }
    }
}
