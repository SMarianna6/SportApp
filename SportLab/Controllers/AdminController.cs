using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Filters;
using SportLab.Models;
using SportLab.Services;

namespace SportLab.Controllers
{
    [Authorize(Roles = Roles.AdminRoles)]
    public class AdminController : Controller
    {
        private readonly SportLabContext _context;
        private readonly IStringLocalizer<AdminController> _localizer;

        public AdminController(SportLabContext context, IStringLocalizer<AdminController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public IActionResult Index()
        {
            var model = new AdminPanelData
            {
                Tariffs = _context.Plans.ToList(),
                Products = _context.Products.ToList(),
                Users = _context.Users.ToList(),
            };

            return View(model);
        }

        public IActionResult Users()
        {
            var tariffNames = _context.Plans.Select(p => p.Name).ToList();
            tariffNames.Insert(0, _localizer["NoPlanSelected"]);

            ViewBag.TariffNames = tariffNames;
            return View();
        }

        public IActionResult Tariffs()
        {
            return View();
        }

        public IActionResult Shop()
        {
            return View();
        }

        public IActionResult FAQ()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Schedule()
        {
            ViewBag.Trainers = await _context.Users.ToListAsync();
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(User user)
        {
            if (ModelState.IsValid)
            {
                _context.Users.Add(user);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> GlobalSearch(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Json(new { users = new string[0], products = new string[0], tariffs = new string[0], faqs = new string[0], reviews = new string[0] });

            var users = await _context.Users
                .Where(u => u.Name.Contains(q) || u.Email.Contains(q))
                .Take(4)
                .Select(u => new { id = u.Id, name = u.Name, subtitle = u.Email, url = $"/Admin/Users?openUser={u.Id}" })
                .ToListAsync();

            var products = await _context.Products
                .Where(p => p.Name.Contains(q))
                .Take(4)
                .Select(p => new { id = p.Id, name = p.Name, subtitle = p.Category, url = $"/Admin/Shop?openProduct={p.Id}" })
                .ToListAsync();

            var tariffs = await _context.Plans
                .Where(p => p.Name.Contains(q))
                .Take(4)
                .Select(p => new { id = p.Id, name = p.Name, subtitle = p.Price + " ₴", url = $"/Admin/Tariffs?openTariff={p.Id}" })
                .ToListAsync();

            var faqLabel = _localizer["FaqLabel"].Value;
            var reviewByPrefix = _localizer["ReviewByPrefix"].Value;

            var faqs = await _context.FAQs
                .Where(f => f.Question.Contains(q))
                .Take(3)
                .Select(f => new { id = f.Id, name = f.Question, subtitle = faqLabel, url = $"/Admin/FAQ?openFAQ={f.Id}" })
                .ToListAsync();

            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.Comment.Contains(q))
                .Take(3)
                .Select(r => new { id = r.Id, name = reviewByPrefix + r.User.Name, subtitle = r.Comment, url = $"/Admin/Moderation?openReview={r.Id}" })
                .ToListAsync();

            return Json(new { users, products, tariffs, faqs, reviews });
        }

        [HttpGet]
        public async Task<IActionResult> GetUsersList()
        {
            var staffLabel = _localizer["Staff"].Value;
            var noPlanLabel = _localizer["NoPlan"].Value;

            var users = await _context.Users
                .Include(u => u.Subscriptions)
                .ThenInclude(s => s.Plan)
                .Select(u => new
                {
                    id = u.Id,
                    name = u.Name,
                    email = u.Email,
                    role = u.Role,
                    active = !u.IsBlocked,
                    tariff = u.Subscriptions
                    .OrderByDescending(s => s.Id)
                    .Where(s => s.Plan != null)
                    .Select(s => s.Plan!.Name)
                    .FirstOrDefault()
                    ?? (u.Role == "Trainer" || u.Role == "Staff" ? staffLabel : noPlanLabel)
                })
                .ToListAsync();

            return Json(users);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUserApi(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserApi(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsBlocked = !user.IsBlocked;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SaveUserApi([FromBody] UserDto userDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            User? user;
            if (userDto.Id == 0)
            {
                if (string.IsNullOrWhiteSpace(userDto.Password))
                {
                    return BadRequest(_localizer["PasswordRequiredNewUser"].Value);
                }

                user = new User
                {
                    Name = userDto.Name,
                    Email = userDto.Email,
                    Role = userDto.Role,
                    Password = PasswordHasher.Hash(userDto.Password ?? "Default123!"),
                    IsBlocked = false
                };
                _context.Users.Add(user);
            }
            else
            {
                user = await _context.Users.Include(u => u.Subscriptions).FirstOrDefaultAsync(u => u.Id == userDto.Id);
                if (user == null) return NotFound();

                user.Name = userDto.Name;
                user.Email = userDto.Email;
                user.Role = userDto.Role;
                if (!string.IsNullOrEmpty(userDto.Password))
                {
                    user.Password = PasswordHasher.Hash(userDto.Password);
                }
            }

            var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Name == userDto.Tariff);

            if (plan != null)
            {
                var lastSubscription = user.Subscriptions.OrderByDescending(s => s.Id).FirstOrDefault();

                if (lastSubscription == null || lastSubscription.PlanId != plan.Id)
                {
                    foreach (var sub in user.Subscriptions.Where(s => s.IsActive))
                    {
                        sub.IsActive = false;
                        _context.Update(sub);
                    }

                    var newSubscription = new Subscription
                    {
                        PlanId = plan.Id,
                        User = user,
                        StartDate = DateTime.Today,
                        EndDate = DateTime.Today.AddDays(plan.Duration),
                        IsActive = true
                    };
                    _context.Set<Subscription>().Add(newSubscription);
                }
            }
            else if (string.IsNullOrWhiteSpace(userDto.Tariff) || userDto.Tariff == "No plan selected" || userDto.Tariff == _localizer["NoPlanSelected"])
            {
                foreach (var sub in user.Subscriptions.Where(s => s.IsActive))
                {
                    sub.IsActive = false;
                    _context.Update(sub);
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetFaqList()
        {
            var faqs = await _context.FAQs
                .OrderBy(f => f.SortOrder)
                .Select(f => new
                {
                    id = f.Id,
                    question = f.Question,
                    answer = f.Answer,
                    sortOrder = f.SortOrder,
                    isActive = f.IsActive
                })
                .ToListAsync();

            return Json(faqs);
        }

        [HttpPost]
        public async Task<IActionResult> SaveFaqApi([FromBody] FAQ faq)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (faq.Id == 0)
            {
                _context.FAQs.Add(faq);
            }
            else
            {
                var existing = await _context.FAQs.FindAsync(faq.Id);
                if (existing != null)
                {
                    existing.Question = faq.Question;
                    existing.Answer = faq.Answer;
                    existing.IsActive = faq.IsActive;
                    existing.SortOrder = faq.SortOrder;
                }
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFaqApi(int id)
        {
            var faq = await _context.FAQs.FindAsync(id);
            if (faq != null)
            {
                _context.FAQs.Remove(faq);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateFaqOrderApi([FromBody] List<FaqOrderDto> orderData)
        {
            foreach (var item in orderData)
            {
                var faq = await _context.FAQs.FindAsync(item.Id);
                if (faq != null)
                {
                    faq.SortOrder = item.SortOrder;
                }
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetTariffsList()
        {
            var tariffs = await _context.Plans
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    description = p.Description,
                    price = p.Price,
                    days = p.Duration,
                    type = p.Type ?? "Standard"
                })
                .ToListAsync();

            return Json(tariffs);
        }

        [HttpPost]
        public async Task<IActionResult> SaveTariffApi([FromBody] PlanDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (dto.Id == 0)
            {
                var newPlan = new Plan
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Price = dto.Price,
                    Duration = dto.Days,
                    Type = dto.Type
                };
                _context.Plans.Add(newPlan);
            }
            else
            {
                var existingPlan = await _context.Plans.FindAsync(dto.Id);
                if (existingPlan != null)
                {
                    existingPlan.Name = dto.Name;
                    existingPlan.Description = dto.Description;
                    existingPlan.Price = dto.Price;
                    existingPlan.Duration = dto.Days;
                    existingPlan.Type = dto.Type;
                }
                else
                {
                    return NotFound();
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTariffApi(int id)
        {
            var plan = await _context.Plans.FindAsync(id);
            if (plan != null)
            {
                _context.Plans.Remove(plan);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetProductList()
        {
            var products = await _context.Products
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    description = p.Description,
                    category = p.Category ?? "Other",
                    price = p.Price,
                    stock = p.StockQuantity,
                    photo = p.PhotoUrl
                })
                .ToListAsync();

            return Json(products);
        }

        [HttpPost]
        public async Task<IActionResult> SaveProductApi(int id, string name, string? description, string category, decimal price, int stock, IFormFile? photo)
        {
            Product? product;

            string? photoPath = null;
            if (photo != null && photo.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(fileStream);
                }

                photoPath = "/images/products/" + uniqueFileName;
            }

            if (id == 0)
            {
                product = new Product
                {
                    Name = name,
                    Description = description,
                    Category = category,
                    Price = price,
                    StockQuantity = stock,
                    PhotoUrl = photoPath
                };
                _context.Products.Add(product);
            }
            else
            {
                product = await _context.Products.FindAsync(id);
                if (product == null) return NotFound();

                product.Name = name;
                product.Description = description;
                product.Category = category;
                product.Price = price;
                product.StockQuantity = stock;

                if (photoPath != null)
                {
                    product.PhotoUrl = photoPath;
                }
            }

            await _context.SaveChangesAsync();

            return Json(new { id = product.Id, photo = product.PhotoUrl });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProductStockApi([FromBody] StockUpdateDto dto)
        {
            var product = await _context.Products.FindAsync(dto.Id);
            if (product != null)
            {
                product.StockQuantity = Math.Max(0, dto.Stock);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProductApi([FromBody] DeleteDto dto)
        {
            var product = await _context.Products.FindAsync(dto.Id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetScheduleList()
        {
            var today = DateTime.Today;
            var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startOfWeek = today.AddDays(-diff).Date;
            var endOfWeek = startOfWeek.AddDays(7);

            var slots = await _context.WorkoutSlots
                .Where(s => s.StartTime >= startOfWeek && s.StartTime < endOfWeek)
                .Select(s => new
                {
                    id = s.Id,
                    day = s.StartTime.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)s.StartTime.DayOfWeek,
                    timeStart = s.StartTime.ToString("HH:mm"),
                    timeEnd = s.EndTime.ToString("HH:mm"),
                    type = s.ActivityName ?? "HIIT",
                    coach = s.Trainer != null ? s.Trainer.Name : "TBA",
                    booked = s.Bookings.Count(),
                    capacity = s.Capacity
                })
                .ToListAsync();

            return Json(slots);
        }

        [HttpPost]
        public async Task<IActionResult> SaveScheduleApi([FromBody] ScheduleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.Day < 1 || dto.Day > 7)
                return BadRequest(_localizer["InvalidDayError"].Value);

            if (dto.Capacity < 1)
                return BadRequest(_localizer["CapacityZeroError"].Value);

            if (!TimeSpan.TryParse(dto.TimeStart, out var startTime) ||
                !TimeSpan.TryParse(dto.TimeEnd, out var endTime))
                return BadRequest(_localizer["InvalidTimeFormatError"].Value);

            if (startTime < TimeSpan.FromHours(8) ||
                endTime > TimeSpan.FromHours(20) ||
                endTime <= startTime)
                return BadRequest(_localizer["InvalidClassTimeError"].Value);

            var trainer = await _context.Users
                .FirstOrDefaultAsync(u => u.Name == dto.Coach);

            if (trainer == null)
                return BadRequest(_localizer["TrainerNotFoundError"].Value);

            var today = DateTime.Today;
            var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startOfWeek = today.AddDays(-diff).Date;
            var targetDate = startOfWeek.AddDays(dto.Day - 1);

            var startDateTime = targetDate.Add(startTime);
            var endDateTime = targetDate.Add(endTime);

            if (dto.Id <= 0)
            {
                var newSlot = new WorkoutSlot
                {
                    ActivityName = dto.Type,
                    TrainerId = trainer.Id,
                    StartTime = startDateTime,
                    EndTime = endDateTime,
                    Capacity = dto.Capacity,
                    Status = "Available"
                };

                _context.WorkoutSlots.Add(newSlot);
                await _context.SaveChangesAsync();

                return Json(new { id = newSlot.Id });
            }

            var existingSlot = await _context.WorkoutSlots
                .Include(s => s.Bookings)
                .FirstOrDefaultAsync(s => s.Id == dto.Id);

            if (existingSlot == null)
                return NotFound();

            if (dto.Capacity < existingSlot.Bookings.Count)
                return BadRequest(_localizer["CapacityLowerThanBookingsError"].Value);

            existingSlot.ActivityName = dto.Type;
            existingSlot.TrainerId = trainer.Id;
            existingSlot.StartTime = startDateTime;
            existingSlot.EndTime = endDateTime;
            existingSlot.Capacity = dto.Capacity;

            if (existingSlot.Status != "Cancelled" && existingSlot.Status != "Completed")
            {
                existingSlot.Status = existingSlot.Bookings.Count >= existingSlot.Capacity
                    ? "Booked"
                    : "Available";
            }

            await _context.SaveChangesAsync();

            return Json(new { id = existingSlot.Id });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteScheduleApi([FromBody] DeleteDto dto)
        {
            var slot = await _context.WorkoutSlots
                .Include(s => s.Bookings)
                .FirstOrDefaultAsync(s => s.Id == dto.Id);

            if (slot == null)
                return NotFound();

            if (slot.Bookings.Any())
                return BadRequest(_localizer["ClassHasBookingsError"].Value);

            _context.WorkoutSlots.Remove(slot);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Moderation()
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .OrderBy(r => r.ModerationStatus == "Pending" ? 0 : 1)
                .ThenByDescending(r => r.CreatedDate)
                .ToListAsync();

            return View(reviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound(new { success = false, message = _localizer["ReviewNotFoundError"].Value });

            review.ModerationStatus = "Approved";
            await _context.SaveChangesAsync();

            return Json(new { success = true, status = "Approved", message = _localizer["ReviewApprovedMsg"].Value });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HideReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound(new { success = false, message = _localizer["ReviewNotFoundError"].Value });

            review.ModerationStatus = "Hidden";
            await _context.SaveChangesAsync();

            return Json(new { success = true, status = "Hidden", message = _localizer["ReviewHiddenMsg"].Value });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound(new { success = false, message = _localizer["ReviewNotFoundError"].Value });

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = _localizer["ReviewDeletedMsg"].Value });
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            var today = DateTime.Today;
            var startOfYear = new DateTime(today.Year, 1, 1);

            var shopOrdersRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);

            var subscriptionRevenue = await _context.Subscriptions
                .Where(s => s.Plan != null)
                .SumAsync(s => (decimal?)s.Plan.Price) ?? 0;

            var totalRevenue = shopOrdersRevenue + subscriptionRevenue;

            var activeMembers = await _context.Users.CountAsync(u => u.Role == Roles.Client && !u.IsBlocked);

            var classBookings = await _context.WorkoutBookings.CountAsync();

            var shopSales = await _context.OrderDetails
                .Where(od => od.Order != null && od.Product != null)
                .SumAsync(od => (decimal?)od.UnitPrice * od.Quantity) ?? 0;

            var stats = new
            {
                revenue = totalRevenue,
                activeMembers = activeMembers,
                classBookings = classBookings,
                shopSales = shopSales
            };

            var revenueData = new int[12];
            var newUsersData = new int[12];

            var yearOrders = await _context.Orders
                .Where(o => o.OrderDate >= startOfYear && o.OrderDate.Year == today.Year)
                .ToListAsync();

            foreach (var order in yearOrders)
            {
                revenueData[order.OrderDate.Month - 1] += (int)order.TotalAmount;
            }

            var yearSubscriptions = await _context.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.StartDate >= startOfYear && s.StartDate.Year == today.Year && s.Plan != null)
                .ToListAsync();

            foreach (var sub in yearSubscriptions)
            {
                revenueData[sub.StartDate.Month - 1] += (int)sub.Plan.Price;
            }

            var usersThisYear = await _context.Users
                .Where(u => u.Role == Roles.Client)
                .ToListAsync();

            foreach (var user in usersThisYear)
            {
                int monthIndex = (user.Id * 7) % 12;
                newUsersData[monthIndex]++;
            }

            var chart = new
            {
                revenue = revenueData,
                newUsers = newUsersData
            };

            var events = new List<object>();

            var recentOrders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(2)
                .Select(o => new { o.Id, o.TotalAmount, o.OrderDate })
                .ToListAsync();

            foreach (var o in recentOrders)
            {
                events.Add(new
                {
                    Text = _localizer["ActivityNewOrder", o.Id, o.TotalAmount].Value,
                    Date = o.OrderDate
                });
            }

            var recentSubscriptions = await _context.Subscriptions
                .Include(s => s.User)
                .Include(s => s.Plan)
                .Where(s => s.Plan != null && s.User != null)
                .OrderByDescending(s => s.StartDate)
                .Take(2)
                .Select(s => new { UserName = s.User!.Name, PlanName = s.Plan!.Name, s.StartDate })
                .ToListAsync();

            foreach (var s in recentSubscriptions)
            {
                events.Add(new
                {
                    Text = _localizer["ActivitySubscription", s.UserName, s.PlanName].Value,
                    Date = s.StartDate
                });
            }

            var recentUsers = await _context.Users
                .Where(u => u.Role == Roles.Client)
                .OrderByDescending(u => u.Id)
                .Take(2)
                .Select(u => new { u.Name })
                .ToListAsync();

            foreach (var u in recentUsers)
            {
                events.Add(new
                {
                    Text = _localizer["ActivityNewUser", u.Name].Value,
                    Date = DateTime.Now.AddHours(-2)
                });
            }

            var recentBookings = await _context.WorkoutBookings
                .Include(b => b.User)
                .Include(b => b.WorkoutSlot)
                .OrderByDescending(b => b.BookingDate)
                .Take(1)
                .Select(b => new { UserName = b.User!.Name, ActivityName = b.WorkoutSlot!.ActivityName, b.BookingDate })
                .ToListAsync();

            foreach (var b in recentBookings)
            {
                events.Add(new
                {
                    Text = _localizer["ActivityNewBooking", b.UserName, b.ActivityName].Value,
                    Date = b.BookingDate
                });
            }

            var sortedEvents = events
                .OrderByDescending(e => ((dynamic)e).Date)
                .Take(6)
                .Select(e => new {
                    text = ((dynamic)e).Text,
                    time = FormatTimeAgo(((dynamic)e).Date)
                })
                .ToList();

            return Json(new { stats, chart, events = sortedEvents });
        }

        private string FormatTimeAgo(DateTime date)
        {
            var span = DateTime.Now - date;
            if (span.TotalMinutes < 60) return _localizer["TimeMinAgo", (int)span.TotalMinutes].Value;
            if (span.TotalHours < 24) return _localizer["TimeHoursAgo", (int)span.TotalHours].Value;
            return _localizer["TimeDaysAgo", (int)span.TotalDays].Value;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrdersList()
        {
            var unknownCustomer = _localizer["Unknown"].Value;
            var notAvailable = _localizer["NotAvailable"].Value;
            var unknownProduct = _localizer["UnknownProduct"].Value;

            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    id = o.Id,
                    customerName = o.User != null ? o.User.Name : unknownCustomer,
                    customerEmail = o.User != null ? o.User.Email : notAvailable,
                    orderDate = o.OrderDate,
                    totalAmount = o.TotalAmount,
                    status = o.Status,
                    itemsCount = o.OrderDetails.Sum(od => od.Quantity),
                    items = o.OrderDetails.Select(od => new
                    {
                        productName = od.Product != null ? od.Product.Name : unknownProduct,
                        quantity = od.Quantity,
                        unitPrice = od.UnitPrice,
                        photoUrl = od.Product != null ? od.Product.PhotoUrl : null
                    }).ToList()
                })
                .ToListAsync();

            return Json(orders);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatusApi(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound(new { success = false, message = _localizer["OrderNotFoundError"].Value });

            order.Status = status;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        public IActionResult Orders() => View();
    }

    public class UserDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }
        public required string Password { get; set; }
        public required string Tariff { get; set; }
    }

    public class FaqOrderDto
    {
        public int Id { get; set; }
        public int SortOrder { get; set; }
    }

    public class PlanDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public decimal Price { get; set; }
        public int Days { get; set; }
        public string? Type { get; set; }
    }

    public class StockUpdateDto
    {
        public int Id { get; set; }
        public int Stock { get; set; }
    }

    public class DeleteDto
    {
        public int Id { get; set; }
    }

    public class ScheduleDto
    {
        public int Id { get; set; }
        public int Day { get; set; }
        public required string TimeStart { get; set; }
        public required string TimeEnd { get; set; }
        public required string Type { get; set; }
        public required string Coach { get; set; }
        public int Capacity { get; set; }
    }
}
