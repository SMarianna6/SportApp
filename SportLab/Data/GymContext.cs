using Microsoft.EntityFrameworkCore;
using SportLab.Models;

namespace SportLab.Data
{
    public class GymContext : DbContext
    {
        public GymContext(DbContextOptions<GymContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Plan> Plan { get; set; } = null!;
        public DbSet<Plan> Plans { get; set; } = null!;
        public DbSet<Plan> Tariffs => Plan;
        public DbSet<WorkoutSlot> WorkoutSlots { get; set; } = null!;
        public DbSet<Subscription> Subscriptions { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
    }
}