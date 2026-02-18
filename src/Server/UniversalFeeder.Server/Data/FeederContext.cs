using Microsoft.EntityFrameworkCore;
using UniversalFeeder.Server.Models;

namespace UniversalFeeder.Server.Data
{
    public class FeederContext : DbContext
    {
        public FeederContext(DbContextOptions<FeederContext> options) : base(options) { }

        public DbSet<Feeder> Feeders { get; set; }
        public DbSet<FeedingLog> Logs { get; set; }
    }
}
