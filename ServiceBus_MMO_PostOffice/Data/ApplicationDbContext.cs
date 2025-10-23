using Microsoft.EntityFrameworkCore;
using ServiceBus_MMO_PostOffice.Models;

namespace ServiceBus_MMO_PostOffice.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Guild> Guild { get; set; }
        public DbSet<Player> Player { get; set; }

    }
}
