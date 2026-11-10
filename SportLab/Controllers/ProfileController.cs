using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;
using SportLab.Services;

namespace SportLab.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly SportLabContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProfileController(
            SportLabContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public IActionResult Index()
        {
            if (User.IsAdmin())
                return RedirectToAction("Index", "Admin");

            if (User.IsInRole(Roles.Trainer))
                return RedirectToAction(nameof(Trainer));

            if (User.IsInRole(Roles.Moderator))
                return RedirectToAction("Index", "Moderation");

            return RedirectToAction(nameof(Client));
        }

        // CLIENT PROFILE

        [Authorize(Roles = Roles.Client)]
        [HttpGet]
        public async Task<IActionResult> Client()
        {
            var user = await _context.Users.FindAsync(CurrentUserId);

            if (user == null)
                return NotFound();

            return View(user);
        }

        [Authorize(Roles = Roles.Client)]
        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            int userId = CurrentUserId;
            var now = DateTime.Now;

            var subscription = await _context.Subscriptions
                .Include(s => s.Plan)
                .Where(s =>
                    s.UserId == userId &&
                    s.IsActive &&
                    s.EndDate >= DateTime.Today)
                .OrderByDescending(s => s.StartDate)
                .Select(s => new
                {
                    planName = s.Plan.Name,
                    description = s.Plan.Description,
                    price = s.Plan.Price,
                    duration = s.Plan.Duration,
                    type = s.Plan.Type,
                    endDate = s.EndDate,
                    isActive = s.IsActive
                })
                .FirstOrDefaultAsync();

            var nextWorkout = await _context.WorkoutBookings
                .Where(b =>
                    b.UserId == userId &&
                    b.WorkoutSlot.StartTime >= now &&
                    b.WorkoutSlot.Status != "Cancelled" &&
                    b.WorkoutSlot.Status != "Completed")
                .OrderBy(b => b.WorkoutSlot.StartTime)
                .Select(b => new
                {
                    id = b.WorkoutSlot.Id,
                    activityName = b.WorkoutSlot.ActivityName,
                    startTime = b.WorkoutSlot.StartTime,
                    endTime = b.WorkoutSlot.EndTime,
                    trainerName = b.WorkoutSlot.Trainer != null
                        ? b.WorkoutSlot.Trainer.Name
                        : "Not assigned"
                })
                .FirstOrDefaultAsync();

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new
                {
                    id = o.Id,
                    orderDate = o.OrderDate,
                    totalAmount = o.TotalAmount,
                    status = o.Status,
                    products = o.OrderDetails.Select(d => new
                    {
                        name = d.Product.Name,
                        quantity = d.Quantity,
                        photoUrl = d.Product.PhotoUrl
                    }).ToList()
                })
                .ToListAsync();

            var weights = await _context.MetricLogs
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.RecordDate)
                .Take(10)
                .Select(m => new
                {
                    id = m.Id,
                    weight = m.Weight,
                    recordDate = m.RecordDate
                })
                .ToListAsync();

            var latestWeights = weights
                .OrderByDescending(w => w.recordDate)
                .Take(2)
                .ToList();

            decimal? weightChange = null;

            if (latestWeights.Count == 2)
            {
                weightChange =
                    Convert.ToDecimal(latestWeights[0].weight) -
                    Convert.ToDecimal(latestWeights[1].weight);
            }

            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Name,
                    u.AvatarUrl
                })
                .FirstOrDefaultAsync();

            return Json(new
            {
                success = true,
                user,
                subscription,
                nextWorkout,
                orders,
                weights,
                weightChange
            });
        }

        // PROFILE NAME

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateName(
            [FromBody] UpdateNameRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Name is required."
                });
            }

            var name = request.Name.Trim();

            if (name.Length < 2 || name.Length > 100)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Name must be between 2 and 100 characters."
                });
            }

            var user = await _context.Users.FindAsync(CurrentUserId);

            if (user == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found."
                });
            }

            user.Name = name;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                name = user.Name
            });
        }

        // CLIENT BOOKING CANCELLATION

        [HttpPost]
        [Authorize(Roles = Roles.Client)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(
            [FromBody] CancelBookingRequest request)
        {
            if (request == null || request.BookingId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid booking."
                });
            }

            int userId = CurrentUserId;

            var booking = await _context.WorkoutBookings
                .Include(b => b.WorkoutSlot)
                    .ThenInclude(w => w.Bookings)
                .FirstOrDefaultAsync(b =>
                    b.WorkoutSlotId == request.BookingId &&
                    b.UserId == userId);

            if (booking == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Booking not found."
                });
            }

            var slot = booking.WorkoutSlot;

            if (slot.StartTime <= DateTime.Now ||
                slot.Status == "Cancelled" ||
                slot.Status == "Completed")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "This booking can no longer be cancelled."
                });
            }

            _context.WorkoutBookings.Remove(booking);

            int remainingBookings = slot.Bookings.Count - 1;

            slot.Booked = remainingBookings;
            slot.Status = remainingBookings >= slot.Capacity
                ? "Booked"
                : "Available";

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Your booking has been cancelled."
            });
        }

        // CLIENT WEIGHT TRACKING

        [HttpPost]
        [Authorize(Roles = Roles.Client)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWeight(
            [FromBody] AddWeightRequest request)
        {
            if (request == null ||
                request.Weight <= 0 ||
                request.Weight > 500)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Enter a valid weight."
                });
            }

            var metric = new MetricLog
            {
                UserId = CurrentUserId,
                Weight = request.Weight,
                RecordDate = DateTime.Now
            };

            _context.MetricLogs.Add(metric);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                id = metric.Id,
                weight = metric.Weight,
                recordDate = metric.RecordDate
            });
        }

        // AVATAR MANAGEMENT

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            var user = await _context.Users.FindAsync(CurrentUserId);

            if (user == null)
                return NotFound();

            if (avatar == null || avatar.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please select an image."
                });
            }

            const long maxFileSize = 5 * 1024 * 1024;

            if (avatar.Length > maxFileSize)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Image size must not exceed 5 MB."
                });
            }

            var extension = Path.GetExtension(avatar.FileName)
                .ToLowerInvariant();

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            if (!allowedExtensions.Contains(extension) ||
                !HasValidImageSignature(avatar))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Only valid JPG, PNG and WEBP images are allowed."
                });
            }

            var avatarsFolder = Path.Combine(
                _environment.WebRootPath,
                "images",
                "avatars");

            Directory.CreateDirectory(avatarsFolder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(avatarsFolder, fileName);

            await using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }

            var oldAvatarUrl = user.AvatarUrl;

            user.AvatarUrl = $"/images/avatars/{fileName}";

            await _context.SaveChangesAsync();

            DeleteAvatarFile(oldAvatarUrl, avatarsFolder);

            return Json(new
            {
                success = true,
                avatarUrl = user.AvatarUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvatar()
        {
            var user = await _context.Users.FindAsync(CurrentUserId);

            if (user == null)
                return NotFound();

            var oldAvatarUrl = user.AvatarUrl;

            user.AvatarUrl = null;

            await _context.SaveChangesAsync();

            var avatarsFolder = Path.Combine(
                _environment.WebRootPath,
                "images",
                "avatars");

            DeleteAvatarFile(oldAvatarUrl, avatarsFolder);

            return Json(new
            {
                success = true
            });
        }

        private static bool HasValidImageSignature(IFormFile file)
        {
            using var stream = file.OpenReadStream();

            var header = new byte[12];
            var bytesRead = stream.Read(header, 0, header.Length);

            // JPEG
            if (bytesRead >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF)
            {
                return true;
            }

            // PNG
            if (bytesRead >= 8 &&
                header[0] == 0x89 &&
                header[1] == 0x50 &&
                header[2] == 0x4E &&
                header[3] == 0x47 &&
                header[4] == 0x0D &&
                header[5] == 0x0A &&
                header[6] == 0x1A &&
                header[7] == 0x0A)
            {
                return true;
            }

            // WEBP
            if (bytesRead >= 12 &&
                header[0] == 0x52 &&
                header[1] == 0x49 &&
                header[2] == 0x46 &&
                header[3] == 0x46 &&
                header[8] == 0x57 &&
                header[9] == 0x45 &&
                header[10] == 0x42 &&
                header[11] == 0x50)
            {
                return true;
            }

            return false;
        }

        private static void DeleteAvatarFile(
            string? avatarUrl,
            string avatarsFolder)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl))
                return;

            const string avatarPrefix = "/images/avatars/";

            var normalizedUrl = avatarUrl.Replace('\\', '/');

            if (!normalizedUrl.StartsWith(
                avatarPrefix,
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var fileName = Path.GetFileName(normalizedUrl);

            if (string.IsNullOrWhiteSpace(fileName))
                return;

            var filePath = Path.Combine(avatarsFolder, fileName);

            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        // TRAINER PROFILE

        [HttpGet]
        [Authorize(Roles = Roles.TrainerRoles)]
        public async Task<IActionResult> Trainer()
        {
            var trainer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == CurrentUserId);

            if (trainer == null)
                return NotFound();

            return View(trainer);
        }

        // GET TRAINER PROFILE DATA — JSON for async Fetch on Trainer dashboard
        [HttpGet]
        [Authorize(Roles = Roles.TrainerRoles)]
        public async Task<IActionResult> GetTrainerData()
        {
            int trainerId = CurrentUserId;

            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == trainerId)
                .Select(u => new { u.Name, u.Email, u.AvatarUrl })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { success = false });

            var profile = await _context.TrainerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(tp => tp.UserId == trainerId);

            return Json(new
            {
                success         = true,
                name            = user.Name,
                email           = user.Email,
                avatarUrl       = user.AvatarUrl,
                dateOfBirth     = profile?.DateOfBirth?.ToString("yyyy-MM-dd"),
                about           = profile?.About,
                experience      = profile?.Experience,
                specializations = profile?.Specializations,
                photoUrl        = profile?.PhotoUrl
            });
        }

        // UPDATE TRAINER PROFILE — saves TrainerProfile fields
        [HttpPost]
        [Authorize(Roles = Roles.TrainerRoles)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTrainerProfile(
            [FromBody] UpdateTrainerProfileRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Invalid request." });

            int trainerId = CurrentUserId;

            var user = await _context.Users.FindAsync(trainerId);

            if (user == null)
                return NotFound(new { success = false, message = "User not found." });

            // Update User.Name if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var trimmed = request.Name.Trim();

                if (trimmed.Length < 2 || trimmed.Length > 100)
                    return BadRequest(new
                    {
                        success = false,
                        message = "Name must be between 2 and 100 characters."
                    });

                user.Name = trimmed;
            }

            // Find or create TrainerProfile
            var profile = await _context.TrainerProfiles
                .FirstOrDefaultAsync(tp => tp.UserId == trainerId);

            if (profile == null)
            {
                profile = new TrainerProfile { UserId = trainerId };
                _context.TrainerProfiles.Add(profile);
            }

            if (request.DateOfBirth.HasValue)
                profile.DateOfBirth = request.DateOfBirth.Value;

            if (request.About != null)
                profile.About = request.About.Trim();

            if (request.Experience != null)
                profile.Experience = request.Experience.Trim();

            if (request.Specializations != null)
                profile.Specializations = request.Specializations.Trim();

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // GET CLIENT HISTORY — trainer views specific client's data
        [HttpGet]
        [Authorize(Roles = Roles.TrainerRoles)]
        public async Task<IActionResult> ClientHistory(int id)
        {
            if (id <= 0) return NotFound();

            int trainerId = CurrentUserId;

            // Verify this client has booked with this trainer
            bool hasRelation = await _context.WorkoutBookings
                .AnyAsync(b =>
                    b.UserId == id &&
                    b.WorkoutSlot.TrainerId == trainerId);

            if (!hasRelation && !User.IsAdmin())
                return Forbid();

            var client = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new { u.Name, u.AvatarUrl, u.Email })
                .FirstOrDefaultAsync();

            if (client == null) return NotFound();

            ViewBag.ClientId   = id;
            ViewBag.ClientName = client.Name;
            ViewBag.AvatarUrl  = client.AvatarUrl;

            return View();
        }

        // GET CLIENT HISTORY DATA — JSON for the history view
        [HttpGet]
        [Authorize(Roles = Roles.TrainerRoles)]
        public async Task<IActionResult> GetClientHistoryData(int id)
        {
            if (id <= 0)
                return BadRequest(new { success = false });

            int trainerId = CurrentUserId;

            bool hasRelation = await _context.WorkoutBookings
                .AnyAsync(b =>
                    b.UserId == id &&
                    b.WorkoutSlot.TrainerId == trainerId);

            if (!hasRelation && !User.IsAdmin())
                return Forbid();

            var now = DateTime.Now;

            // Past completed/past workouts this client attended with this trainer
            var workouts = await _context.WorkoutBookings
                .AsNoTracking()
                .Include(b => b.WorkoutSlot)
                .Where(b =>
                    b.UserId == id &&
                    b.WorkoutSlot.TrainerId == trainerId &&
                    b.WorkoutSlot.StartTime < now)
                .OrderByDescending(b => b.WorkoutSlot.StartTime)
                .Select(b => new
                {
                    id           = b.WorkoutSlot.Id,
                    activityName = b.WorkoutSlot.ActivityName,
                    startTime    = b.WorkoutSlot.StartTime,
                    endTime      = b.WorkoutSlot.EndTime,
                    status       = b.WorkoutSlot.Status
                })
                .ToListAsync();

            // Weight logs for this client
            var weights = await _context.MetricLogs
                .AsNoTracking()
                .Where(m => m.UserId == id)
                .OrderBy(m => m.RecordDate)
                .Select(m => new
                {
                    id         = m.Id,
                    weight     = m.Weight,
                    recordDate = m.RecordDate,
                    notes      = m.Notes
                })
                .ToListAsync();

            var client = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new { u.Name, u.AvatarUrl })
                .FirstOrDefaultAsync();

            return Json(new
            {
                success   = true,
                client,
                workouts,
                weights
            });
        }

        [HttpGet]
        [Authorize(Roles = Roles.TrainerRoles)]
        public async Task<IActionResult> Clients()
        {
            int trainerId = CurrentUserId;

            var clients = await _context.WorkoutBookings
                .Where(b => b.WorkoutSlot.TrainerId == trainerId)
                .Select(b => b.User)
                .Distinct()
                .ToListAsync();

            return View(clients);
        }

        [HttpPost]
        [Authorize(Roles = Roles.TrainerRoles)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadTrainerPhoto(IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
                return BadRequest(new { success = false, message = "Please select an image." });

            const long maxFileSize = 5 * 1024 * 1024;

            if (photo.Length > maxFileSize)
                return BadRequest(new { success = false, message = "Image size must not exceed 5 MB." });

            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            if (!allowedExtensions.Contains(extension) || !HasValidImageSignature(photo))
                return BadRequest(new { success = false, message = "Only valid JPG, PNG and WEBP images are allowed." });

            var folder = Path.Combine(_environment.WebRootPath, "images", "avatars");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(folder, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            int trainerId = CurrentUserId;
            var profile = await _context.TrainerProfiles
                .FirstOrDefaultAsync(tp => tp.UserId == trainerId);

            if (profile == null)
            {
                profile = new TrainerProfile { UserId = trainerId };
                _context.TrainerProfiles.Add(profile);
            }

            var oldPhoto = profile.PhotoUrl;
            profile.PhotoUrl = $"/images/avatars/{fileName}";

            await _context.SaveChangesAsync();
            DeleteAvatarFile(oldPhoto, folder);

            return Json(new { success = true, photoUrl = profile.PhotoUrl });
        }

        // SETTINGS — pass current user data for pre-fill
        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var user = await _context.Users.FindAsync(CurrentUserId);

            if (user == null) return NotFound();

            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> GetAccount()
        {
            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == CurrentUserId)
                .Select(u => new { u.Name, u.Email })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { success = false, message = "User not found." });

            return Json(new { success = true, name = user.Name, email = user.Email });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAccountSettings(
            [FromBody] UpdateAccountRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Invalid request." });

            var name  = request.Name?.Trim();
            var email = request.Email?.Trim();

            if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 100)
                return BadRequest(new
                {
                    success = false,
                    message = "Full name must be between 2 and 100 characters."
                });

            if (string.IsNullOrWhiteSpace(email) ||
                !System.Text.RegularExpressions.Regex.IsMatch(
                    email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return BadRequest(new { success = false, message = "Invalid email address." });
            }

            var user = await _context.Users.FindAsync(CurrentUserId);

            if (user == null)
                return NotFound(new { success = false, message = "User not found." });

            // Check email uniqueness
            bool emailTaken = await _context.Users
                .AnyAsync(u => u.Email == email && u.Id != user.Id);

            if (emailTaken)
                return Conflict(new
                {
                    success = false,
                    message = "This email is already in use by another account."
                });

            user.Name  = name;
            user.Email = email;

            await _context.SaveChangesAsync();

            return Json(new { success = true, name = user.Name, email = user.Email });
        }

        // UPDATE PASSWORD
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(
            [FromBody] UpdatePasswordRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Invalid request." });

            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                return BadRequest(new
                {
                    success = false,
                    message = "Current password is required."
                });

            if (string.IsNullOrWhiteSpace(request.NewPassword) ||
                request.NewPassword.Length < 6)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "New password must be at least 6 characters."
                });
            }

            if (request.NewPassword != request.ConfirmPassword)
                return BadRequest(new
                {
                    success = false,
                    message = "Passwords do not match."
                });

            var user = await _context.Users.FindAsync(CurrentUserId);

            if (user == null)
                return NotFound(new { success = false, message = "User not found." });

            if (!PasswordHasher.Verify(request.CurrentPassword, user.Password))
                return BadRequest(new
                {
                    success = false,
                    message = "Current password is incorrect."
                });

            user.Password = PasswordHasher.Hash(request.NewPassword);

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Password updated successfully." });
        }

        // SHARED PROFILE SCHEDULE

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Schedule()
        {
            bool isTrainer = User.IsInRole(Roles.Trainer);
            bool isClient = User.IsInRole(Roles.Client);

            if (!isTrainer && !isClient)
                return Forbid();

            int userId = CurrentUserId;

            var today = DateTime.Today;
            int diff = ((int)today.DayOfWeek + 6) % 7;

            var startOfWeek = today.AddDays(-diff);
            var endOfWeek = startOfWeek.AddDays(7);

            IQueryable<WorkoutSlot> query = _context.WorkoutSlots
                .AsNoTracking()
                .Include(w => w.Trainer)
                .Include(w => w.Bookings)
                    .ThenInclude(b => b.User)
                .Where(w =>
                    w.StartTime >= startOfWeek &&
                    w.StartTime < endOfWeek);

            if (isTrainer)
            {
                query = query.Where(w => w.TrainerId == userId);
            }
            else
            {
                query = query.Where(w =>
                    w.Bookings.Any(b => b.UserId == userId));
            }

            var slots = await query
                .OrderBy(w => w.StartTime)
                .ToListAsync();

            foreach (var slot in slots)
            {
                slot.Booked = slot.Bookings.Count;
            }

            return View(slots);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetScheduleList()
        {
            bool isTrainer = User.IsInRole(Roles.Trainer);
            bool isClient = User.IsInRole(Roles.Client);

            if (!isTrainer && !isClient)
                return Forbid();

            int userId = CurrentUserId;

            var today = DateTime.Today;
            int diff = ((int)today.DayOfWeek + 6) % 7;

            var startOfWeek = today.AddDays(-diff);
            var endOfWeek = startOfWeek.AddDays(7);

            IQueryable<WorkoutSlot> query = _context.WorkoutSlots
                .AsNoTracking()
                .Include(w => w.Trainer)
                .Include(w => w.Bookings)
                    .ThenInclude(b => b.User)
                .Where(w =>
                    w.StartTime >= startOfWeek &&
                    w.StartTime < endOfWeek);

            if (isTrainer)
            {
                query = query.Where(w => w.TrainerId == userId);
            }
            else
            {
                query = query.Where(w =>
                    w.Bookings.Any(b => b.UserId == userId));
            }

            var slots = await query
                .OrderBy(w => w.StartTime)
                .ToListAsync();

            var result = slots.Select(w => new
            {
                id = w.Id,
                date = w.StartTime.ToString("yyyy-MM-dd"),
                day = w.StartTime.DayOfWeek == DayOfWeek.Sunday
                    ? 7
                    : (int)w.StartTime.DayOfWeek,
                timeStart = w.StartTime.ToString("HH:mm"),
                timeEnd = w.EndTime.ToString("HH:mm"),
                type = w.ActivityName,
                coach = w.Trainer?.Name ?? "TBA",
                booked = w.Bookings.Count,
                capacity = w.Capacity,
                status = w.Status,
                clients = isTrainer
                    ? w.Bookings.Select(b => new
                    {
                        id = b.UserId,
                        name = b.User.Name
                    }).ToList()
                    : null
            });

            return Json(result);
        }

        // CREATE OR EDIT TRAINER SLOT FROM PROFILE SCHEDULE

        [HttpPost]
        [Authorize(Roles = Roles.TrainerRoles)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveScheduleApi(
            [FromForm] ProfileScheduleDto dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest("Invalid schedule data.");

            if (string.IsNullOrWhiteSpace(dto.Type) ||
                dto.Type.Trim().Length < 2 ||
                dto.Type.Trim().Length > 100)
            {
                return BadRequest(
                    "Activity name must contain between 2 and 100 characters.");
            }

            if (dto.Capacity < 1 || dto.Capacity > 1000)
                return BadRequest("Capacity must be between 1 and 1000.");

            if (!DateTime.TryParseExact(
                dto.Date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
            {
                return BadRequest("Invalid date.");
            }

            if (!TimeSpan.TryParse(dto.TimeStart, out var startTime) ||
                !TimeSpan.TryParse(dto.TimeEnd, out var endTime))
            {
                return BadRequest("Invalid time.");
            }

            if (startTime < TimeSpan.FromHours(8) ||
                endTime > TimeSpan.FromHours(20) ||
                endTime <= startTime)
            {
                return BadRequest(
                    "The workout must be between 08:00 and 20:00, and the end time must be later than the start time.");
            }

            var startDateTime = date.Date.Add(startTime);
            var endDateTime = date.Date.Add(endTime);

            if (startDateTime <= DateTime.Now)
                return BadRequest("A workout must be scheduled for a future time.");

            int trainerId = CurrentUserId;

            WorkoutSlot slot;

            if (dto.Id > 0)
            {
                slot = await _context.WorkoutSlots
                    .Include(w => w.Bookings)
                    .FirstOrDefaultAsync(w => w.Id == dto.Id);

                if (slot == null)
                    return NotFound();

                if (slot.TrainerId != trainerId)
                    return Forbid();

                if (slot.Bookings.Any())
                {
                    return BadRequest(
                        "A workout with existing bookings cannot be edited.");
                }

                if (slot.Status == "Cancelled" ||
                    slot.Status == "Completed")
                {
                    return BadRequest(
                        "Cancelled or completed workouts cannot be edited.");
                }

                slot.ActivityName = dto.Type.Trim();
                slot.StartTime = startDateTime;
                slot.EndTime = endDateTime;
                slot.Capacity = dto.Capacity;
                slot.Booked = 0;
                slot.Status = "Available";
            }
            else
            {
                slot = new WorkoutSlot
                {
                    ActivityName = dto.Type.Trim(),
                    TrainerId = trainerId,
                    StartTime = startDateTime,
                    EndTime = endDateTime,
                    Capacity = dto.Capacity,
                    Booked = 0,
                    Status = "Available"
                };

                _context.WorkoutSlots.Add(slot);
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                id = slot.Id
            });
        }

        // TRAINER CANCELS A SLOT AND NOTIFIES BOOKED CLIENTS

        [HttpPost]
        [Authorize(Roles = Roles.TrainerRoles)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelScheduleApi(
            [FromForm] int id)
        {
            if (id <= 0)
                return BadRequest("Invalid workout ID.");

            var slot = await _context.WorkoutSlots
                .Include(w => w.Bookings)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (slot == null)
                return NotFound();

            if (slot.TrainerId != CurrentUserId)
                return Forbid();

            if (slot.Status == "Cancelled")
                return BadRequest("This workout is already cancelled.");

            if (slot.Status == "Completed")
                return BadRequest(
                    "A completed workout cannot be cancelled.");

            foreach (var booking in slot.Bookings)
            {
                _context.Messages.Add(new Message
                {
                    SenderId = CurrentUserId,
                    ReceiverId = booking.UserId,
                    Text =
                        $"Your training session \"{slot.ActivityName}\" on " +
                        $"{slot.StartTime:dd.MM.yyyy HH:mm} was cancelled by the trainer. " +
                        "Please book another slot if needed.",
                    SentDate = DateTime.Now
                });
            }

            slot.Status = "Cancelled";

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Workout cancelled successfully."
            });
        }

        // DELETE EMPTY TRAINER SLOT

        [HttpPost]
        [Authorize(Roles = Roles.TrainerRoles)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteScheduleApi(
            [FromForm] int id)
        {
            if (id <= 0)
                return BadRequest("Invalid workout ID.");

            var slot = await _context.WorkoutSlots
                .Include(w => w.Bookings)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (slot == null)
                return NotFound();

            if (slot.TrainerId != CurrentUserId)
                return Forbid();

            if (slot.Bookings.Any())
            {
                return BadRequest(
                    "A workout with bookings cannot be deleted. Cancel it instead.");
            }

            _context.WorkoutSlots.Remove(slot);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true
            });
        }

        // CLIENT CANCELS OWN BOOKING FROM PROFILE SCHEDULE

        [HttpPost]
        [Authorize(Roles = Roles.Client)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelScheduleBooking(
            [FromForm] int id)
        {
            if (id <= 0)
                return BadRequest("Invalid workout ID.");

            int userId = CurrentUserId;

            var booking = await _context.WorkoutBookings
                .Include(b => b.WorkoutSlot)
                    .ThenInclude(w => w.Bookings)
                .FirstOrDefaultAsync(b =>
                    b.WorkoutSlotId == id &&
                    b.UserId == userId);

            if (booking == null)
                return NotFound();

            var slot = booking.WorkoutSlot;

            if (slot.StartTime <= DateTime.Now)
            {
                return BadRequest(
                    "You cannot cancel a workout that has already started.");
            }

            if (slot.Status == "Cancelled" ||
                slot.Status == "Completed")
            {
                return BadRequest(
                    "This workout is no longer available.");
            }

            _context.WorkoutBookings.Remove(booking);

            int remainingBookings = slot.Bookings.Count - 1;

            slot.Booked = remainingBookings;
            slot.Status = remainingBookings >= slot.Capacity
                ? "Booked"
                : "Available";

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Your booking has been cancelled."
            });
        }
    }

    // REQUEST MODELS

    public class UpdateTrainerProfileRequest
    {
        public string? Name            { get; set; }
        public DateTime? DateOfBirth   { get; set; }
        public string? About           { get; set; }
        public string? Experience      { get; set; }
        public string? Specializations { get; set; }
    }

    public class UpdateAccountRequest
    {
        public string? Name  { get; set; }
        public string? Email { get; set; }
    }

    public class UpdatePasswordRequest
    {
        public string? CurrentPassword { get; set; }
        public string? NewPassword     { get; set; }
        public string? ConfirmPassword { get; set; }
    }

    public class UpdateNameRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class CancelBookingRequest
    {
        public int BookingId { get; set; }
    }

    public class AddWeightRequest
    {
        public float Weight { get; set; }
    }

    public class ProfileScheduleDto
    {
        public int Id { get; set; }

        public required string Date { get; set; }

        public required string TimeStart { get; set; }

        public required string TimeEnd { get; set; }

        public required string Type { get; set; }

        public int Capacity { get; set; } = 15;
    }
}