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
                // Створюємо базу даних та схему, якщо вони ще не існують
                context.Database.EnsureCreated();

                // Перевірка наявності даних у таблиці Plan згідно з вимогами ЛР4
                if (context.Plan.Any())
                {
                    return; // Базу даних уже ініціалізовано початковими даними
                }

                // 1. Початкові користувачі (тренери та клієнти для слотів і підписок)
                var trainer1 = new User
                {
                    Name = "Олександр Коваленко",
                    Email = "alex.trainer@sportlab.ua",
                    Password = "TrainerPass123!",
                    Role = "Trainer",
                    IsBlocked = false
                };

                var trainer2 = new User
                {
                    Name = "Марина Шевченко",
                    Email = "marina.trainer@sportlab.ua",
                    Password = "TrainerPass123!",
                    Role = "Trainer",
                    IsBlocked = false
                };

                var client1 = new User
                {
                    Name = "Іван Бондаренко",
                    Email = "ivan.client@sportlab.ua",
                    Password = "ClientPass123!",
                    Role = "Client",
                    IsBlocked = false
                };

                var client2 = new User
                {
                    Name = "Олена Мельник",
                    Email = "olena.client@sportlab.ua",
                    Password = "ClientPass123!",
                    Role = "Client",
                    IsBlocked = false
                };

                context.Users.AddRange(trainer1, trainer2, client1, client2);
                context.SaveChanges();

                // 2. Початкові тарифи (Plan)
                var plans = new List<Plan>
                {
                    new Plan
                    {
                        Name = "Basic Start",
                        Type = "Базовий",
                        Price = 850.00m,
                        Duration = 30,
                        Description = "Ранковий та денний доступ до залу (07:00 - 16:00). Вступний інструктаж тренера, персональна шафка та душові."
                    },
                    new Plan
                    {
                        Name = "Unlimited Pro",
                        Type = "Популярний",
                        Price = 1450.00m,
                        Duration = 30,
                        Description = "Цілодобовий безлімітний доступ 24/7 до всіх зон. Усі групові заняття (CrossFit, TRX, Yoga), фінська сауна та джакузі."
                    },
                    new Plan
                    {
                        Name = "Student Drive",
                        Type = "Студентський",
                        Price = 650.00m,
                        Duration = 30,
                        Description = "Доступ з 09:00 до 18:00 за наявності студентського квитка. Функціональні тренінги та знижка 10% у фітнес-барі."
                    },
                    new Plan
                    {
                        Name = "Annual VIP Unlimited",
                        Type = "Преміум",
                        Price = 11900.00m,
                        Duration = 365,
                        Description = "Повний річний абонемент без будь-яких обмежень. 5 персональних занять, консультація нутриціолога та 15% знижка в магазині."
                    }
                };

                context.Plan.AddRange(plans);
                context.SaveChanges();

                // 3. Початкові тренувальні слоти (WorkoutSlot)
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

                // 4. Тестові оформлені підписки (Subscription)
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

                // 5. Тестові записи показників ваги (MetricLog)
                var metricLogs = new List<MetricLog>
                {
                    new MetricLog
                    {
                        UserId = client1.Id,
                        Weight = 78.5f,
                        RecordDate = DateTime.Today.AddDays(-7),
                        Notes = "Початок тренувального циклу"
                    },
                    new MetricLog
                    {
                        UserId = client1.Id,
                        Weight = 77.8f,
                        RecordDate = DateTime.Today,
                        Notes = "Прогрес після першого тижня кардіо"
                    },
                    new MetricLog
                    {
                        UserId = client2.Id,
                        Weight = 59.2f,
                        RecordDate = DateTime.Today,
                        Notes = "Контрольне зважування після занять йогою"
                    }
                };

                context.MetricLogs.AddRange(metricLogs);

                context.SaveChanges();
            }
        }
    }
}
