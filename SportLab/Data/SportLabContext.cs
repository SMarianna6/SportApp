using Microsoft.EntityFrameworkCore;

namespace SportLab.Data
{
    public class SportLabContext : DbContext
    {
        public SportLabContext(DbContextOptions<SportLabContext> options)
            : base(options)
        {
        }
    }
}
