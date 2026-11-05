using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Filters;
using SportLab.Models;

namespace SportLab.Controllers
{
    [Authorize(Roles = Roles.AdminRoles)]
    public class AdminController : Controller
    {
        private readonly SportLabContext _context;

        public AdminController(SportLabContext context)
        {
            _context = context;
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
            ViewBag.TariffNames = _context.Plans.Select(p => p.Name).ToList();
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
                return Json(new { users = new string[0], products = new string[0], tariffs = new string[0], faqs = new string[0] });

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
                .Select(p => new { id = p.Id, name = p.Name, subtitle = p.Price + " $", url = $"/Admin/Tariffs?openTariff={p.Id}" })
                .ToListAsync();

            var faqs = await _context.FAQs
                .Where(f => f.Question.Contains(q))
                .Take(3)
                .Select(f => new { id = f.Id, name = f.Question, subtitle = "FAQ", url = $"/Admin/FAQ?openFAQ=/{f.Id}" })
                .ToListAsync();

            return Json(new { users, products, tariffs, faqs });
        }

        [HttpGet]
        public async Task<IActionResult> GetUsersList()
        {
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
                    ?? (u.Role == "Trainer" || u.Role == "Staff" ? "Staff" : "No Plan")
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
                    return BadRequest("A password is required for new users.");
                }

                user = new User
                {
                    Name = userDto.Name,
                    Email = userDto.Email,
                    Role = userDto.Role,
                    Password = userDto.Password ?? "Default123!",
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
                    user.Password = userDto.Password;
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
            var startOfWeek = today.AddDays(-1 * diff).Date;
            var endOfWeek = startOfWeek.AddDays(7);

            var slots = await _context.Set<WorkoutSlot>()
                .Include(s => s.Trainer)
                .Where(s => s.StartTime >= startOfWeek && s.StartTime < endOfWeek)
                .Select(s => new
                {
                    id = s.Id,
                    day = s.StartTime.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)s.StartTime.DayOfWeek,
                    timeStart = s.StartTime.ToString("HH:mm"),
                    timeEnd = s.EndTime.ToString("HH:mm"),
                    type = s.ActivityName ?? "HIIT",
                    coach = s.Trainer != null ? s.Trainer.Name : "TBA",
                    booked = s.Booked,
                    capacity = s.Capacity
                })
                .ToListAsync();

            return Json(slots);
        }

        [HttpPost]
        public async Task<IActionResult> SaveScheduleApi([FromBody] ScheduleDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var trainer = await _context.Users.FirstOrDefaultAsync(u => u.Name == dto.Coach)
                          ?? await _context.Users.FirstOrDefaultAsync();

            if (trainer == null) return BadRequest("No trainers found in database.");

            var today = DateTime.Today;
            var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startOfWeek = today.AddDays(-1 * diff).Date;
            var targetDate = startOfWeek.AddDays(dto.Day - 1);

            var startTimeParts = dto.TimeStart.Split(':');
            var endTimeParts = dto.TimeEnd.Split(':');

            var startDateTime = targetDate.AddHours(int.Parse(startTimeParts[0])).AddMinutes(int.Parse(startTimeParts[1]));
            var endDateTime = targetDate.AddHours(int.Parse(endTimeParts[0])).AddMinutes(int.Parse(endTimeParts[1]));

            string calculatedStatus = dto.Booked >= dto.Capacity ? "Booked" : "Available";

            if (dto.Id <= 0)
            {
                var newSlot = new WorkoutSlot
                {
                    ActivityName = dto.Type,
                    TrainerId = trainer.Id,
                    StartTime = startDateTime,
                    EndTime = endDateTime,
                    Capacity = dto.Capacity,
                    Booked = dto.Booked,
                    Status = calculatedStatus
                };
                _context.Set<WorkoutSlot>().Add(newSlot);
                await _context.SaveChangesAsync();
                return Json(new { id = newSlot.Id });
            }
            else
            {
                var existingSlot = await _context.Set<WorkoutSlot>().FindAsync(dto.Id);
                if (existingSlot != null)
                {
                    existingSlot.ActivityName = dto.Type;
                    existingSlot.TrainerId = trainer.Id;
                    existingSlot.StartTime = startDateTime;
                    existingSlot.EndTime = endDateTime;
                    existingSlot.Capacity = dto.Capacity;
                    existingSlot.Booked = dto.Booked;
                    existingSlot.Status = calculatedStatus;

                    await _context.SaveChangesAsync();
                    return Json(new { id = existingSlot.Id });
                }
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteScheduleApi([FromBody] DeleteDto dto)
        {
            var slot = await _context.Set<WorkoutSlot>().FindAsync(dto.Id);
            if (slot != null)
            {
                _context.Set<WorkoutSlot>().Remove(slot);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
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
        public int Booked { get; set; }
        public int Capacity { get; set; }
    }
}
