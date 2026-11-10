using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportLab.Data;
using SportLab.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SportLab.Models
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var context = new SportLabContext(
                serviceProvider.GetRequiredService<DbContextOptions<SportLabContext>>());

            // Schema is owned by EF Core migrations now.
            context.Database.Migrate();

            // Staff accounts (admin / moderator / legacy "Admin" → "Administrator")
            EnsureStaffAccounts(context);

            // ---- Core Users ---------------------------------------------------
            if (!context.Users.Any(u => u.Email == "alex.trainer@sportlab.ua"))
            {
                var trainer1 = new User { Name = "Alexander Kovalenko", Email = "alex.trainer@sportlab.ua", Password = PasswordHasher.Hash("TrainerPass123!"), Role = Roles.Trainer, IsBlocked = false };
                var trainer2 = new User { Name = "Maryna Shevchenko", Email = "marina.trainer@sportlab.ua", Password = PasswordHasher.Hash("TrainerPass123!"), Role = Roles.Trainer, IsBlocked = false };
                var trainer3 = new User { Name = "Maksym Tkachenko", Email = "maksym@sportlab.ua", Password = PasswordHasher.Hash("Pass123!"), Role = Roles.Trainer, IsBlocked = false };
                var trainer4 = new User { Name = "Oksana Koval", Email = "oksana@sportlab.ua", Password = PasswordHasher.Hash("Pass123!"), Role = Roles.Trainer, IsBlocked = false };
                var trainer5 = new User { Name = "Dmytro Boyko", Email = "dmytro@sportlab.ua", Password = PasswordHasher.Hash("Pass123!"), Role = Roles.Trainer, IsBlocked = false };

                var client1 = new User { Name = "Ivan Bondarenko", Email = "ivan.client@sportlab.ua", Password = PasswordHasher.Hash("ClientPass123!"), Role = Roles.Client, IsBlocked = false, Goal = "Weight Loss" };
                var client2 = new User { Name = "Olena Melnyk", Email = "olena.client@sportlab.ua", Password = PasswordHasher.Hash("ClientPass123!"), Role = Roles.Client, IsBlocked = false, Goal = "Flexibility" };

                context.Users.AddRange(trainer1, trainer2, trainer3, trainer4, trainer5, client1, client2);
                context.SaveChanges();
            }

            // ---- Plans + slots + subscriptions + metric logs -------------
            if (!context.Plans.Any())
            {
                var plans = new List<Plan>
                {
                    new Plan { Name = "Basic Start",        Type = "Basic",   Price = 850.00m,   Duration = 30, Description = "Morning and day access (07:00 - 16:00). Intro trainer session, personal locker and showers." },
                    new Plan { Name = "Unlimited Pro",      Type = "Popular", Price = 1450.00m,  Duration = 30, Description = "24/7 unlimited access to all zones. All group classes, sauna and jacuzzi." },
                    new Plan { Name = "Student Drive",      Type = "Student", Price = 650.00m,   Duration = 30, Description = "Access from 09:00 to 18:00 with a student ID. Functional training sessions and a 10% discount at the fitness bar." },
                    new Plan { Name = "Annual VIP Unlimited", Type = "Premium", Price = 11900.00m, Duration = 365, Description = "Full annual membership with no restrictions. 5 personal training sessions, a consultation with a nutritionist, and a 15% discount in the store." }
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
                        new WorkoutSlot { ActivityName = "HIIT",     TrainerId = trainerUsers[0].Id, StartTime = startOfWeek.AddHours(8), EndTime = startOfWeek.AddHours(9), Status = WorkoutStatuses.Available },
                        new WorkoutSlot { ActivityName = "Yoga",     TrainerId = trainerUsers[1].Id, UserId = clientUsers[0].Id, StartTime = startOfWeek.AddHours(10), EndTime = startOfWeek.AddHours(11), Status = WorkoutStatuses.Booked, Booked = 1 },
                        new WorkoutSlot { ActivityName = "Strength", TrainerId = trainerUsers[2].Id, StartTime = startOfWeek.AddHours(17), EndTime = startOfWeek.AddHours(18), Status = WorkoutStatuses.Available },
                        new WorkoutSlot { ActivityName = "Pilates",  TrainerId = trainerUsers[3].Id, StartTime = startOfWeek.AddDays(1).AddHours(9), EndTime = startOfWeek.AddDays(1).AddHours(10), Status = WorkoutStatuses.Available },
                        new WorkoutSlot { ActivityName = "Boxing",   TrainerId = trainerUsers[4].Id, StartTime = startOfWeek.AddDays(1).AddHours(18), EndTime = startOfWeek.AddDays(1).AddHours(19), Status = WorkoutStatuses.Available },
                        new WorkoutSlot { ActivityName = "Cycling",  TrainerId = trainerUsers[0].Id, UserId = clientUsers[1].Id, StartTime = startOfWeek.AddDays(1).AddHours(19), EndTime = startOfWeek.AddDays(1).AddHours(20), Status = WorkoutStatuses.Booked, Booked = 1 }
                    };
                    context.WorkoutSlots.AddRange(workoutSlots);
                    context.SaveChanges();

                    var seedBookings = workoutSlots
                        .Where(s => s.UserId.HasValue)
                        .Select(s => new WorkoutBooking
                        {
                            WorkoutSlotId = s.Id,
                            UserId = s.UserId!.Value,
                            BookingDate = s.StartTime.AddDays(-1),
                            Status = BookingStatuses.Booked
                        })
                        .ToList();
                    context.WorkoutBookings.AddRange(seedBookings);

                    context.Subscriptions.AddRange(
                        new Subscription { UserId = clientUsers[0].Id, PlanId = plans[1].Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(plans[1].Duration), IsActive = true },
                        new Subscription { UserId = clientUsers[1].Id, PlanId = plans[0].Id, StartDate = DateTime.Today.AddDays(-10), EndDate = DateTime.Today.AddDays(20), IsActive = true });

                    context.MetricLogs.AddRange(
                        new MetricLog { UserId = clientUsers[0].Id, Weight = 78.5f, RecordDate = DateTime.Today.AddDays(-7), Notes = "Start of the training cycle" },
                        new MetricLog { UserId = clientUsers[0].Id, Weight = 77.8f, RecordDate = DateTime.Today, Notes = "Progress after the first week of cardio" },
                        new MetricLog { UserId = clientUsers[1].Id, Weight = 59.2f, RecordDate = DateTime.Today, Notes = "Check-in weighing after a yoga session" });

                    context.SaveChanges();
                }
            }

            // ---- Products -------------------------------------------------
            if (!context.Products.Any())
            {
                var products = new List<Product>
                {
                    new Product { Name = "Optimum 100% Whey Gold Standard", Category = "Nutrition",   Description = "24g of high-quality whey protein per serving with whey protein isolate as the primary ingredient. Double Rich Chocolate flavor, 2.27kg tub.", Price = 1850.00m, StockQuantity = 28, Rating = "5.0", PhotoUrl = "https://images.unsplash.com/photo-1579722821273-0f6c7d44362f?w=600&h=450&fit=crop" },
                    new Product { Name = "Essential BCAA Energy 2:1:1",    Category = "Nutrition",   Description = "Instantized Branched Chain Amino Acids with electrolytes for sustained muscle endurance and post-workout recovery. Green Apple flavor, 400g.", Price = 720.00m,  StockQuantity = 35, Rating = "4.8", PhotoUrl = "https://images.unsplash.com/photo-1593095948071-474c5cc2989d?w=600&h=450&fit=crop" },
                    new Product { Name = "SportLab Pro Shaker 750ml",      Category = "Accessories", Description = "BPA-free leak-proof fitness shaker with stainless steel blending ball and dual measurement scales (ml/oz).", Price = 380.00m,  StockQuantity = 60, Rating = "4.9", PhotoUrl = "https://images.unsplash.com/photo-1517838277536-f5f99be501cd?w=600&h=450&fit=crop" },
                    new Product { Name = "SportLab Performance Hoodie",    Category = "Apparel",     Description = "Breathable athletic fleece pullover engineered for pre-workout warm-ups and everyday athletic comfort. Unisex fit.", Price = 1650.00m, StockQuantity = 20, Rating = "4.9", PhotoUrl = "https://images.unsplash.com/photo-1556906781-9a412961c28c?w=600&h=450&fit=crop" }
                };
                context.Products.AddRange(products);
                context.SaveChanges();
            }

            // Runs on every startup, idempotent. Ensures every Trainer has a profile row.
            EnsureTrainerProfiles(context);

            // ---- EXTENDED DEMO DATA (NEW) ---------------------------------
            // Adds a rich set of clients, trainers, history logs, and future bookings
            // to make the application look heavily used and populated.
            SeedExtendedDemoData(context);
        }

        // ------------------------------------------------------------------
        private static void EnsureStaffAccounts(SportLabContext context)
        {
            if (!context.Users.Any(u => u.Email == "admin@sportlab.ua"))
            {
                context.Users.Add(new User { Name = "SportLab Administrator", Email = "admin@sportlab.ua", Password = PasswordHasher.Hash("AdminPass123!"), Role = Roles.Administrator, IsBlocked = false });
            }
            if (!context.Users.Any(u => u.Email == "moderator@sportlab.ua"))
            {
                context.Users.Add(new User { Name = "SportLab Moderator", Email = "moderator@sportlab.ua", Password = PasswordHasher.Hash("ModeratorPass123!"), Role = Roles.Moderator, IsBlocked = false });
            }
            foreach (var legacyAdmin in context.Users.Where(u => u.Role == Roles.AdminAlias).ToList())
            {
                legacyAdmin.Role = Roles.Administrator;
            }
            context.SaveChanges();
        }

        // ------------------------------------------------------------------
        private static void EnsureTrainerProfiles(SportLabContext context)
        {
            var demo = new Dictionary<string, (DateTime Dob, string About, string Exp, string Spec, string Photo)>
            {
                ["alex.trainer@sportlab.ua"] = (
                    new DateTime(1988, 4, 12),
                    "Certified strength & conditioning coach with 8 years of experience training athletes across HIIT, boxing and functional strength.",
                    "8 years",
                    "CrossFit,Strength,HIIT,Boxing",
                    "https://images.unsplash.com/photo-1567013127542-490d757e51fc?w=400&h=400&fit=crop&crop=faces"),
                ["marina.trainer@sportlab.ua"] = (
                    new DateTime(1992, 9, 3),
                    "Yoga & pilates instructor focused on mobility, posture and mindful recovery. RYT-500 certified.",
                    "6 years",
                    "Yoga,Pilates,Mobility",
                    "https://images.unsplash.com/photo-1548690312-e3b507d8c110?w=400&h=400&fit=crop&crop=faces")
            };

            var trainers = context.Users.Where(u => u.Role == Roles.Trainer).ToList();
            var existing = context.TrainerProfiles.Select(p => p.UserId).ToHashSet();

            foreach (var t in trainers)
            {
                if (existing.Contains(t.Id)) continue;

                var profile = new TrainerProfile { UserId = t.Id };
                if (demo.TryGetValue(t.Email, out var d))
                {
                    profile.DateOfBirth = d.Dob;
                    profile.About = d.About;
                    profile.Experience = d.Exp;
                    profile.Specializations = d.Spec;
                    profile.PhotoUrl = d.Photo;
                }
                context.TrainerProfiles.Add(profile);
            }
            context.SaveChanges();
        }

        // ------------------------------------------------------------------
        private static void SeedExtendedDemoData(SportLabContext context)
        {
            // Anchor: Only run if the extended demo data hasn't been seeded yet
            if (context.Users.Any(u => u.Email == "john.doe@sportlab.ua")) return;

            var plans = context.Plans.ToList();
            var products = context.Products.ToList();
            if (!plans.Any() || !products.Any()) return;

            var today = DateTime.Today;

            // 1. Create New Trainers
            var sarah = new User { Name = "Sarah Jenkins", Email = "sarah.trainer@sportlab.ua", Password = PasswordHasher.Hash("TrainerPass123!"), Role = Roles.Trainer };
            var mike = new User { Name = "Mike Tyson", Email = "mike.trainer@sportlab.ua", Password = PasswordHasher.Hash("TrainerPass123!"), Role = Roles.Trainer };
            context.Users.AddRange(sarah, mike);
            context.SaveChanges();

            context.TrainerProfiles.AddRange(
                new TrainerProfile { UserId = sarah.Id, DateOfBirth = new DateTime(1990, 5, 20), About = "Expert in HIIT and cardiovascular conditioning. Let's get that heart rate up!", Experience = "5 years", Specializations = "HIIT,Cardio,Mobility", PhotoUrl = "https://images.unsplash.com/photo-1594381898411-846e7d193883?w=400&h=400&fit=crop&crop=faces" },
                new TrainerProfile { UserId = mike.Id, DateOfBirth = new DateTime(1985, 2, 15), About = "Boxing coach with a passion for technique and power. Former amateur champion.", Experience = "10 years", Specializations = "Boxing,Strength,CrossFit", PhotoUrl = "https://images.unsplash.com/photo-1549719386-74dfcbf7dbed?w=400&h=400&fit=crop&crop=faces" }
            );
            context.SaveChanges();

            // 2. Create New Clients
            var john = new User { Name = "John Doe", Email = "john.doe@sportlab.ua", Password = PasswordHasher.Hash("ClientPass123!"), Role = Roles.Client, Goal = "Weight Loss" };
            var emma = new User { Name = "Emma Smith", Email = "emma.smith@sportlab.ua", Password = PasswordHasher.Hash("ClientPass123!"), Role = Roles.Client, Goal = "Muscle Gain" };
            var david = new User { Name = "David Lee", Email = "david.lee@sportlab.ua", Password = PasswordHasher.Hash("ClientPass123!"), Role = Roles.Client, Goal = "Endurance" };
            var lisa = new User { Name = "Lisa Ray", Email = "lisa.ray@sportlab.ua", Password = PasswordHasher.Hash("ClientPass123!"), Role = Roles.Client, Goal = "Flexibility & Toning" };
            context.Users.AddRange(john, emma, david, lisa);
            context.SaveChanges();

            // 3. Assign Subscriptions (Different states)
            var planBasic = plans.First(p => p.Type == "Basic");
            var planPro = plans.First(p => p.Type == "Popular");

            context.Subscriptions.AddRange(
                new Subscription { UserId = john.Id, PlanId = planPro.Id, StartDate = today.AddDays(-20), EndDate = today.AddDays(10), IsActive = true },
                new Subscription { UserId = emma.Id, PlanId = planBasic.Id, StartDate = today.AddDays(-45), EndDate = today.AddDays(-15), IsActive = false }, // Expired
                new Subscription { UserId = david.Id, PlanId = planBasic.Id, StartDate = today, EndDate = today.AddDays(30), IsActive = true },
                new Subscription { UserId = lisa.Id, PlanId = planPro.Id, StartDate = today.AddDays(-28), EndDate = today.AddDays(2), IsActive = true } // Expiring soon
            );

            // 4. Generate Weight Logs (5 weeks of history for the sparkline charts)
            var random = new Random();
            for (int i = 5; i >= 0; i--)
            {
                var date = today.AddDays(-i * 7);

                // John (Losing weight)
                context.MetricLogs.Add(new MetricLog { UserId = john.Id, Weight = 92.0f - ((5 - i) * 1.2f), RecordDate = date, Notes = "Weekly check-in" });
                // Emma (Gaining weight)
                context.MetricLogs.Add(new MetricLog { UserId = emma.Id, Weight = 58.0f + ((5 - i) * 0.8f), RecordDate = date, Notes = "Bulking phase" });
                // David (Maintaining)
                context.MetricLogs.Add(new MetricLog { UserId = david.Id, Weight = 75.0f + (float)(random.NextDouble() * 0.5 - 0.25), RecordDate = date, Notes = "Feeling good" });
                // Lisa (Slight loss)
                context.MetricLogs.Add(new MetricLog { UserId = lisa.Id, Weight = 55.0f - ((5 - i) * 0.3f), RecordDate = date, Notes = "Yoga & pilates focus" });
            }

            // 5. Generate Past Workouts (History)
            var pastSlots = new List<WorkoutSlot>
            {
                new WorkoutSlot { ActivityName = "Yoga", TrainerId = sarah.Id, StartTime = today.AddDays(-10).AddHours(9), EndTime = today.AddDays(-10).AddHours(10), Status = WorkoutStatuses.Completed, Capacity = 10, Booked = 1 },
                new WorkoutSlot { ActivityName = "Boxing", TrainerId = mike.Id, StartTime = today.AddDays(-8).AddHours(18), EndTime = today.AddDays(-8).AddHours(19), Status = WorkoutStatuses.Completed, Capacity = 15, Booked = 1 },
                new WorkoutSlot { ActivityName = "HIIT", TrainerId = sarah.Id, StartTime = today.AddDays(-5).AddHours(8), EndTime = today.AddDays(-5).AddHours(9), Status = WorkoutStatuses.Completed, Capacity = 20, Booked = 2 }
            };
            context.WorkoutSlots.AddRange(pastSlots);
            context.SaveChanges();

            context.WorkoutBookings.AddRange(
                new WorkoutBooking { WorkoutSlotId = pastSlots[0].Id, UserId = lisa.Id, BookingDate = today.AddDays(-11), Status = BookingStatuses.Attended },
                new WorkoutBooking { WorkoutSlotId = pastSlots[1].Id, UserId = john.Id, BookingDate = today.AddDays(-9), Status = BookingStatuses.Attended },
                new WorkoutBooking { WorkoutSlotId = pastSlots[2].Id, UserId = john.Id, BookingDate = today.AddDays(-6), Status = BookingStatuses.Attended },
                new WorkoutBooking { WorkoutSlotId = pastSlots[2].Id, UserId = emma.Id, BookingDate = today.AddDays(-6), Status = BookingStatuses.Attended }
            );

            // 6. Generate Future Workouts (Upcoming)
            var futureSlots = new List<WorkoutSlot>
            {
                new WorkoutSlot { ActivityName = "Boxing", TrainerId = mike.Id, StartTime = today.AddDays(1).AddHours(18), EndTime = today.AddDays(1).AddHours(19), Status = WorkoutStatuses.Booked, Capacity = 15, Booked = 2 },
                new WorkoutSlot { ActivityName = "HIIT", TrainerId = sarah.Id, StartTime = today.AddDays(2).AddHours(8), EndTime = today.AddDays(2).AddHours(9), Status = WorkoutStatuses.Available, Capacity = 20, Booked = 0 },
                new WorkoutSlot { ActivityName = "Yoga", TrainerId = sarah.Id, StartTime = today.AddDays(3).AddHours(9), EndTime = today.AddDays(3).AddHours(10), Status = WorkoutStatuses.Booked, Capacity = 10, Booked = 1 }
            };
            context.WorkoutSlots.AddRange(futureSlots);
            context.SaveChanges();

            context.WorkoutBookings.AddRange(
                new WorkoutBooking { WorkoutSlotId = futureSlots[0].Id, UserId = john.Id, BookingDate = today.AddDays(-1), Status = BookingStatuses.Booked },
                new WorkoutBooking { WorkoutSlotId = futureSlots[0].Id, UserId = david.Id, BookingDate = today.AddDays(-1), Status = BookingStatuses.Booked },
                new WorkoutBooking { WorkoutSlotId = futureSlots[2].Id, UserId = lisa.Id, BookingDate = today, Status = BookingStatuses.Booked }
            );

            // 7. Generate Orders (Recent Orders for the client dashboard)
            var order1 = new Order { UserId = john.Id, OrderDate = today.AddDays(-3), TotalAmount = 2230.00m, Status = "Completed" };
            var order2 = new Order { UserId = emma.Id, OrderDate = today.AddDays(-1), TotalAmount = 380.00m, Status = "Processing" };

            context.Orders.AddRange(order1, order2);
            context.SaveChanges();

            // Note: Ensure you pick products from the actual context list
            var whey = products.First(p => p.Name.Contains("Whey"));
            var shaker = products.First(p => p.Name.Contains("Shaker"));

            context.OrderDetails.AddRange(
                new OrderDetail { OrderId = order1.Id, ProductId = whey.Id, Quantity = 1, UnitPrice = whey.Price },
                new OrderDetail { OrderId = order1.Id, ProductId = shaker.Id, Quantity = 1, UnitPrice = shaker.Price },
                new OrderDetail { OrderId = order2.Id, ProductId = shaker.Id, Quantity = 1, UnitPrice = shaker.Price }
            );

            context.SaveChanges();
        }
    }
}