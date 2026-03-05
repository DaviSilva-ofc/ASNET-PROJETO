using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Models;

namespace AspnetCoreStarter.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<School> Schools { get; set; }
    }
}
