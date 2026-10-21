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

        public DbSet<Tariff> Tariffs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<WorkoutSlot> WorkoutSlots { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<MetricLog> MetricLogs { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<FAQ> FAQs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            modelBuilder.Entity<WorkoutSlot>()
                .HasOne(w => w.Trainer)
                .WithMany()
                .HasForeignKey(w => w.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkoutSlot>()
                .HasOne(w => w.Client)
                .WithMany()
                .HasForeignKey(w => w.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
