using Microsoft.EntityFrameworkCore;
using UniversalFeeder.Server.Models;

namespace UniversalFeeder.Server.Data
{
    public class FeederContext : DbContext
    {
        public FeederContext(DbContextOptions<FeederContext> options) : base(options) { }

        public DbSet<Feeder> Feeders { get; set; }
        public DbSet<FeedType> FeedTypes { get; set; }
        public DbSet<FeedingSchedule> Schedules { get; set; }
        public DbSet<FeedingLog> Logs { get; set; }
    }
}
