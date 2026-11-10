using Microsoft.EntityFrameworkCore;
using SportLab.Models;

namespace SportLab.Data
{
    public class SportLabContext : DbContext
    {
        public SportLabContext(DbContextOptions<SportLabContext> options)
            : base(options)
        {
        }

        public DbSet<Plan> Plans { get; set; } = null!;
        public DbSet<Plan> Plan => Plans;   // legacy alias kept for existing code
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Subscription> Subscriptions { get; set; } = null!;
        public DbSet<WorkoutSlot> WorkoutSlots { get; set; } = null!;
        public DbSet<WorkoutBooking> WorkoutBookings { get; set; } = null!;
        public DbSet<TrainerProfile> TrainerProfiles { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderDetail> OrderDetails { get; set; } = null!;
        public DbSet<MetricLog> MetricLogs { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<FAQ> FAQs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // ---------------- User ----------------
            b.Entity<User>(e =>
            {
                e.Property(u => u.Name).HasMaxLength(100).IsRequired();
                e.Property(u => u.Email).HasMaxLength(150).IsRequired();
                e.Property(u => u.Password).HasMaxLength(256).IsRequired();
                e.Property(u => u.Role).HasMaxLength(50).IsRequired();
                e.Property(u => u.AvatarUrl).HasMaxLength(500);
                e.Property(u => u.Goal).HasMaxLength(120);

                e.HasIndex(u => u.Email).IsUnique();

                // 1:1 — a Trainer has at most one TrainerProfile
                e.HasOne(u => u.TrainerProfile)
                 .WithOne(p => p.User!)
                 .HasForeignKey<TrainerProfile>(p => p.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------------- TrainerProfile ----------------
            b.Entity<TrainerProfile>(e =>
            {
                e.HasIndex(p => p.UserId).IsUnique();
                e.Property(p => p.About).HasMaxLength(1000);
                e.Property(p => p.Experience).HasMaxLength(255);
                e.Property(p => p.Specializations).HasMaxLength(500);
                e.Property(p => p.PhotoUrl).HasMaxLength(500);
            });

            // ---------------- Plan ----------------
            b.Entity<Plan>(e =>
            {
                e.Property(p => p.Name).HasMaxLength(100).IsRequired();
                e.Property(p => p.Description).IsRequired();
                e.Property(p => p.Price).HasPrecision(18, 2);
                e.Property(p => p.Type).HasMaxLength(50);
            });

            // Legacy empty subclass — remove it from the model so TPH doesn't
            // add a Discriminator column to the Tariffs table.
            b.Ignore<Tariff>();

            // ---------------- Subscription ----------------
            b.Entity<Subscription>(e =>
            {
                e.HasOne(s => s.User).WithMany(u => u.Subscriptions)
                 .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(s => s.Plan).WithMany(p => p.Subscriptions)
                 .HasForeignKey(s => s.PlanId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(s => new { s.UserId, s.IsActive, s.EndDate });  // "active tariff"
            });

            // ---------------- WorkoutSlot ----------------
            b.Entity<WorkoutSlot>(e =>
            {
                e.Property(s => s.ActivityName).HasMaxLength(100).IsRequired();
                e.Property(s => s.Status).HasMaxLength(20)
                 .HasDefaultValue(WorkoutStatuses.Available);
                e.Property(s => s.RowVersion).IsRowVersion();

                // LEGACY single-client FK — kept for old views; new code uses WorkoutBookings.
                e.HasOne(s => s.User).WithMany(u => u.WorkoutSlots)
                 .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.SetNull);

                // Trainer FK — Restrict so SQL Server doesn't complain about
                // multiple cascade paths (User -> WorkoutSlot vs User -> WorkoutBooking).
                e.HasOne(s => s.Trainer).WithMany()
                 .HasForeignKey(s => s.TrainerId).OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(s => new { s.StartTime, s.EndTime });       // weekly grid
                e.HasIndex(s => new { s.TrainerId, s.StartTime });     // trainer dashboard
            });

            // ---------------- WorkoutBooking ----------------
            b.Entity<WorkoutBooking>(e =>
            {
                e.Property(x => x.Status).HasMaxLength(20).IsRequired();
                e.Property(x => x.RowVersion).IsRowVersion();

                // One row per (slot, client) — prevents double booking at the DB level.
                e.HasIndex(x => new { x.WorkoutSlotId, x.UserId }).IsUnique();

                e.HasOne(x => x.WorkoutSlot).WithMany(s => s.Bookings)
                 .HasForeignKey(x => x.WorkoutSlotId).OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.User).WithMany(u => u.WorkoutBookings)
                 .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(x => new { x.UserId, x.BookingDate });       // "next workout"
            });

            // ---------------- MetricLog ----------------
            b.Entity<MetricLog>(e =>
            {
                e.HasOne(m => m.User).WithMany(u => u.MetricLogs)
                 .HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(m => new { m.UserId, m.RecordDate });       // weight tracker
            });

            // ---------------- Message ----------------
            b.Entity<Message>(e =>
            {
                e.HasOne(m => m.Sender).WithMany(u => u.SentMessages)
                 .HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(m => m.Receiver).WithMany(u => u.ReceivedMessages)
                 .HasForeignKey(m => m.ReceiverId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(m => new { m.ReceiverId, m.SentDate });
            });

            // ---------------- Order / OrderDetail ----------------
            b.Entity<Order>(e =>
            {
                e.Property(o => o.TotalAmount).HasPrecision(18, 2);
                e.Property(o => o.Status).HasMaxLength(50).IsRequired();
                e.HasOne(o => o.User).WithMany(u => u.Orders)
                 .HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(o => new { o.UserId, o.OrderDate });         // profile / recent orders
            });

            b.Entity<OrderDetail>(e =>
            {
                e.Property(d => d.UnitPrice).HasPrecision(18, 2);
                e.HasOne(d => d.Order).WithMany(o => o.OrderDetails)
                 .HasForeignKey(d => d.OrderId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                 .HasForeignKey(d => d.ProductId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(d => new { d.OrderId, d.ProductId }).IsUnique();
            });

            // ---------------- Product ----------------
            b.Entity<Product>(e =>
            {
                e.Property(p => p.Name).IsRequired();
                e.Property(p => p.Price).HasPrecision(18, 2);
            });

            // ---------------- Review ----------------
            b.Entity<Review>(e =>
            {
                e.HasOne(r => r.User).WithMany(u => u.Reviews)
                 .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(r => new { r.UserId, r.CreatedDate });
            });
        }
    }
}