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

        // Підтримка перевірки context.Plan.Any() згідно з завданням ЛР4 та стандартом EF
        public DbSet<Plan> Plan { get; set; } = null!;
        public DbSet<Plan> Plans { get; set; } = null!;
        public DbSet<Plan> Tariffs => Plan;

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Subscription> Subscriptions { get; set; } = null!;
        public DbSet<WorkoutSlot> WorkoutSlots { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderDetail> OrderDetails { get; set; } = null!;
        public DbSet<MetricLog> MetricLogs { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<FAQ> FAQs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Налаштування зв'язків для Plan та Subscription (1:N)
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Plan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Налаштування зв'язків для User та Subscription (1:N)
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.User)
                .WithMany(u => u.Subscriptions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Налаштування зв'язків для User та WorkoutSlot (1:N)
            modelBuilder.Entity<WorkoutSlot>()
                .HasOne(w => w.User)
                .WithMany(u => u.WorkoutSlots)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<WorkoutSlot>()
                .HasOne(w => w.Trainer)
                .WithMany()
                .HasForeignKey(w => w.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Налаштування зв'язків для User та MetricLog (1:N)
            modelBuilder.Entity<MetricLog>()
                .HasOne(m => m.User)
                .WithMany(u => u.MetricLogs)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Повідомлення
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
