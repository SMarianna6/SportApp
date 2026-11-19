using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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
        private readonly IStringLocalizer<ProfileController> _localizer;

        public ProfileController(
            SportLabContext context,
            IWebHostEnvironment environment,
            IStringLocalizer<ProfileController> localizer)
        {
            _context = context;
            _environment = environment;
            _localizer = localizer;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task RefreshUserCookieAsync(User user)
        {
            var role = Roles.Normalize(user.Role);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name ?? user.Email),
                new Claim("FullName", user.Name ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

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
            var notAssigned = _localizer["NotAssigned"].Value;

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
                        : notAssigned
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
                .ThenByDescending(m => m.Id)
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
                    message = _localizer["NameRequired"].Value
                });
            }

            var name = request.Name.Trim();

            if (name.Length < 2 || name.Length > 100)
            {
                return BadRequest(new
                {
                    success = false,
                    message = _localizer["NameLength"].Value
                });
            }

            var user = await _context.Users.FindAsync(CurrentUserId);

            if (user == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = _localizer["UserNotFound"].Value
                });
            }

            user.Name = name;

            await _context.SaveChangesAsync();

            await RefreshUserCookieAsync(user);

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
                    message = _localizer["InvalidBooking"].Value
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
                    message = _localizer["BookingNotFound"].Value
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
                    message = _localizer["BookingCannotCancel"].Value
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
                message = _localizer["BookingCancelled"].Value
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
                request.Weight > 300)
            {
                return BadRequest(new
                {
                    success = false,
                    message = _localizer["InvalidWeight"].Value
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

        // DELETE WEIGHT RECORD
        [HttpPost]
        [Authorize(Roles = Roles.Client)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteWeight([FromBody] DeleteWeightRequest request)
        {
            if (request == null || request.Id <= 0)
            {
                return BadRequest(new { success = false, message = _localizer["InvalidRecord"].Value });
            }

            var metric = await _context.MetricLogs
                .FirstOrDefaultAsync(m => m.Id == request.Id && m.UserId == CurrentUserId);

            if (metric == null)
            {
                return NotFound(new { success = false, message = _localizer["WeightRecordNotFound"].Value });
            }

            _context.MetricLogs.Remove(metric);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = _localizer["WeightRecordDeleted"].Value });
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
                    message = _localizer["SelectImage"].Value
                });
            }

            const long maxFileSize = 5 * 1024 * 1024;

            if (avatar.Length > maxFileSize)
            {
                return BadRequest(new
                {
                    success = false,
                    message = _localizer["ImageTooLarge"].Value
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
                    message = _localizer["InvalidImageFormat"].Value
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
                return BadRequest(new { success = false, message = _localizer["InvalidRequest"].Value });

            int trainerId = CurrentUserId;

            var user = await _context.Users.FindAsync(trainerId);

            if (user == null)
                return NotFound(new { success = false, message = _localizer["UserNotFound"].Value });

            // Update User.Name if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var trimmed = request.Name.Trim();

                if (trimmed.Length < 2 || trimmed.Length > 100)
                    return BadRequest(new
                    {
                        success = false,
                        message = _localizer["NameLength"].Value
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
                return BadRequest(new { success = false, message = _localizer["SelectImage"].Value });

            const long maxFileSize = 5 * 1024 * 1024;

            if (photo.Length > maxFileSize)
                return BadRequest(new { success = false, message = _localizer["ImageTooLarge"].Value });

            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            if (!allowedExtensions.Contains(extension) || !HasValidImageSignature(photo))
                return BadRequest(new { success = false, message = _localizer["InvalidImageFormat"].Value });

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
                return NotFound(new { success = false, message = _localizer["UserNotFound"].Value });

            return Json(new { success = true, name = user.Name, email = user.Email });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAccountSettings(
            [FromBody] UpdateAccountRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = _localizer["InvalidRequest"].Value });

            var name  = request.Name?.Trim();
            var email = request.Email?.Trim();

            if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 100)
                return BadRequest(new
                {
                    success = false,
                    message = _localizer["FullNameLength"].Value
                });

            if (string.IsNullOrWhiteSpace(email) ||
                !System.Text.RegularExpressions.Regex.IsMatch(
                    email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return BadRequest(new { success = false, message = _localizer["InvalidEmail"].Value });
            }

            var user = await _context.Users.FindAsync(CurrentUserId);

            if (user == null)
                return NotFound(new { success = false, message = _localizer["UserNotFound"].Value });

            // Check email uniqueness
            bool emailTaken = await _context.Users
                .AnyAsync(u => u.Email == email && u.Id != user.Id);

            if (emailTaken)
                return Conflict(new
                {
                    success = false,
                    message = _localizer["EmailInUse"].Value
                });

            user.Name  = name;
            user.Email = email;

            await _context.SaveChangesAsync();

            await RefreshUserCookieAsync(user);

            return Json(new { success = true, name = user.Name, email = user.Email });
        }

        // UPDATE PASSWORD
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(
            [FromBody] UpdatePasswordRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = _localizer["InvalidRequest"].Value });

            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                return BadRequest(new
                {
                    success = false,
                    message = _localizer["CurrentPasswordRequired"].Value
                });

            if (string.IsNullOrWhiteSpace(request.NewPassword) ||
                request.NewPassword.Length < 6)
            {
                return BadRequest(new
                {
                    success = false,
                    message = _localizer["NewPasswordMinLength"].Value
                });
            }

            if (request.NewPassword != request.ConfirmPassword)
                return BadRequest(new
                {
                    success = false,
                    message = _localizer["PasswordsMismatch"].Value
                });

            var user = await _context.Users.FindAsync(CurrentUserId);

            if (user == null)
                return NotFound(new { success = false, message = _localizer["UserNotFound"].Value });

            if (!PasswordHasher.Verify(request.CurrentPassword, user.Password))
                return BadRequest(new
                {
                    success = false,
                    message = _localizer["CurrentPasswordIncorrect"].Value
                });

            user.Password = PasswordHasher.Hash(request.NewPassword);

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = _localizer["PasswordUpdated"].Value });
        }

        // DELETE ACCOUNT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _context.Users.FindAsync(CurrentUserId);

            if (user == null)
            {
                return NotFound(new { success = false, message = _localizer["UserNotFound"].Value });
            }

            try
            {
                var metrics = await _context.MetricLogs.Where(m => m.UserId == user.Id).ToListAsync();
                _context.MetricLogs.RemoveRange(metrics);

                var bookings = await _context.WorkoutBookings.Where(b => b.UserId == user.Id).ToListAsync();
                _context.WorkoutBookings.RemoveRange(bookings);

                var orders = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .Where(o => o.UserId == user.Id)
                    .ToListAsync();
                _context.Orders.RemoveRange(orders);

                var messages = await _context.Messages
                    .Where(m => m.SenderId == user.Id || m.ReceiverId == user.Id)
                    .ToListAsync();
                _context.Messages.RemoveRange(messages);

                var subs = await _context.Subscriptions.Where(s => s.UserId == user.Id).ToListAsync();
                _context.Subscriptions.RemoveRange(subs);

                var reviews = await _context.Reviews.Where(r => r.UserId == user.Id).ToListAsync();
                _context.Reviews.RemoveRange(reviews);

                var trainerProfile = await _context.TrainerProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (trainerProfile != null)
                {
                    _context.TrainerProfiles.Remove(trainerProfile);
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                await HttpContext.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);

                return Json(new { success = true, message = _localizer["AccountDeleted"].Value });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = _localizer["DbError", ex.Message].Value
                });
            }
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
                coach = w.Trainer?.Name ?? _localizer["Tba"].Value,
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
                return BadRequest(_localizer["InvalidScheduleData"].Value);

            if (string.IsNullOrWhiteSpace(dto.Type) ||
                dto.Type.Trim().Length < 2 ||
                dto.Type.Trim().Length > 100)
            {
                return BadRequest(
                    _localizer["ActivityNameLength"].Value);
            }

            if (dto.Capacity < 1 || dto.Capacity > 1000)
                return BadRequest(_localizer["CapacityRange"].Value);

            if (!DateTime.TryParseExact(
                dto.Date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
            {
                return BadRequest(_localizer["InvalidDate"].Value);
            }

            if (!TimeSpan.TryParse(dto.TimeStart, out var startTime) ||
                !TimeSpan.TryParse(dto.TimeEnd, out var endTime))
            {
                return BadRequest(_localizer["InvalidTime"].Value);
            }

            if (startTime < TimeSpan.FromHours(8) ||
                endTime > TimeSpan.FromHours(20) ||
                endTime <= startTime)
            {
                return BadRequest(
                    _localizer["TimeRange"].Value);
            }

            var startDateTime = date.Date.Add(startTime);
            var endDateTime = date.Date.Add(endTime);

            if (startDateTime <= DateTime.Now)
                return BadRequest(_localizer["FutureTimeRequired"].Value);

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
                        _localizer["WorkoutWithBookingsCannotEdit"].Value);
                }

                if (slot.Status == "Cancelled" ||
                    slot.Status == "Completed")
                {
                    return BadRequest(
                        _localizer["CancelledOrCompletedCannotEdit"].Value);
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
                return BadRequest(_localizer["InvalidWorkoutId"].Value);

            var slot = await _context.WorkoutSlots
                .Include(w => w.Bookings)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (slot == null)
                return NotFound();

            if (slot.TrainerId != CurrentUserId)
                return Forbid();

            if (slot.Status == "Cancelled")
                return BadRequest(_localizer["WorkoutAlreadyCancelled"].Value);

            if (slot.Status == "Completed")
                return BadRequest(
                    _localizer["CompletedWorkoutCannotCancel"].Value);

            foreach (var booking in slot.Bookings)
            {
                _context.Messages.Add(new Message
                {
                    SenderId = CurrentUserId,
                    ReceiverId = booking.UserId,
                    Text = _localizer["CancellationNotice", slot.ActivityName, slot.StartTime.ToString("dd.MM.yyyy HH:mm")].Value,
                    SentDate = DateTime.Now
                });
            }

            slot.Status = "Cancelled";

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = _localizer["WorkoutCancelled"].Value
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
                return BadRequest(_localizer["InvalidWorkoutId"].Value);

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
                    _localizer["WorkoutWithBookingsCannotDelete"].Value);
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
                return BadRequest(_localizer["InvalidWorkoutId"].Value);

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
                    _localizer["WorkoutAlreadyStarted"].Value);
            }

            if (slot.Status == "Cancelled" ||
                slot.Status == "Completed")
            {
                return BadRequest(
                    _localizer["WorkoutNoLongerAvailable"].Value);
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
                message = _localizer["BookingCancelled"].Value
            });
        }

        [HttpGet]
        [Authorize(Roles = Roles.TrainerRoles)]
        public async Task<IActionResult> GetClients()
        {
            int trainerId = CurrentUserId;
            var today = DateTime.Today;
            var now = DateTime.Now;

            var bookings = await _context.WorkoutBookings
                .AsNoTracking()
                .Where(b => b.WorkoutSlot.TrainerId == trainerId)
                .Select(b => new
                {
                    b.UserId,
                    name = b.User.Name,
                    email = b.User.Email,
                    goal = b.User.Goal,
                    startTime = b.WorkoutSlot.StartTime,
                    status = b.WorkoutSlot.Status,
                    b.BookingDate
                })
                .ToListAsync();

            var userIds = bookings.Select(b => b.UserId).Distinct().ToList();

            var weights = await _context.MetricLogs
                .AsNoTracking()
                .Where(m => userIds.Contains(m.UserId))
                .OrderBy(m => m.RecordDate)
                .Select(m => new { m.UserId, m.Weight })
                .ToListAsync();

            var subscriptions = await _context.Subscriptions
                .AsNoTracking()
                .Where(s => userIds.Contains(s.UserId))
                .Select(s => new { s.UserId, s.IsActive, s.EndDate })
                .ToListAsync();

            var clients = bookings
                .GroupBy(b => b.UserId)
                .Select(g =>
                {
                    var first = g.First();
                    var past = g.Where(b => b.startTime < now && b.status != "Cancelled").ToList();
                    var lastWorkout = (past.Count > 0 ? past : g.ToList()).Max(b => b.startTime);

                    var sub = subscriptions
                        .Where(s => s.UserId == g.Key)
                        .OrderByDescending(s => s.EndDate)
                        .FirstOrDefault();

                    string membership = "Inactive";
                    if (sub != null && sub.IsActive && sub.EndDate.Date >= today)
                    {
                        membership = sub.EndDate.Date <= today.AddDays(7) ? "Expiring" : "Active";
                    }

                    string membershipLabel = membership switch
                    {
                        "Active" => _localizer["StatusActive"].Value,
                        "Expiring" => _localizer["StatusExpiring"].Value,
                        _ => _localizer["StatusInactive"].Value
                    };

                    return new
                    {
                        id = g.Key,
                        name = first.name,
                        email = first.email,
                        goal = string.IsNullOrWhiteSpace(first.goal) ? _localizer["NotSet"].Value : first.goal,
                        workouts = g.Count(b => b.status != "Cancelled"),
                        lastWorkout,
                        status = membership,
                        statusLabel = membershipLabel,
                        weights = weights.Where(w => w.UserId == g.Key).Select(w => w.Weight).ToList()
                    };
                })
                .OrderByDescending(c => c.lastWorkout)
                .ToList();

            return Json(new { success = true, clients });
        }

        [HttpGet]
        [Authorize(Roles = Roles.TrainerRoles)]
        public async Task<IActionResult> GetTrainerDashboardData()
        {
            int trainerId = CurrentUserId;

            DateTime today = DateTime.Today;
            DateTime dayAfterTomorrow = today.AddDays(2);

            var slots = await _context.WorkoutSlots
                .AsNoTracking()
                .Include(w => w.Bookings)
                    .ThenInclude(b => b.User)
                .Where(w =>
                    w.TrainerId == trainerId &&
                    w.StartTime >= today &&
                    w.StartTime < dayAfterTomorrow &&
                    w.Status != "Cancelled")
                .OrderBy(w => w.StartTime)
                .ToListAsync();

            var result = slots.Select(slot => new
            {
                id = slot.Id,
                activityName = slot.ActivityName,
                date = slot.StartTime.ToString("yyyy-MM-dd"),
                startTime = slot.StartTime.ToString("HH:mm"),
                endTime = slot.EndTime.ToString("HH:mm"),
                status = slot.Status,
                capacity = slot.Capacity,
                booked = slot.Bookings.Count,
                clients = slot.Bookings
                    .Select(b => new
                    {
                        id = b.UserId,
                        name = b.User.Name
                    })
                    .ToList()
            });

            return Json(new
            {
                success = true,
                slots = result
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

    public class DeleteWeightRequest
    {
        public int Id { get; set; }
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