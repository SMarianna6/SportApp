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
    }
}
