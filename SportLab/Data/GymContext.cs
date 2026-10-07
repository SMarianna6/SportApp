using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SportLab.Models;

namespace SportLab.Data
{
    public class GymContext : DbContext
    {
        public GymContext(DbContextOptions<GymContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Tariff> Tariffs { get; set; }
        public DbSet<WorkoutSlot> WorkoutSlots { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Product> Products { get; set; }
    }
}