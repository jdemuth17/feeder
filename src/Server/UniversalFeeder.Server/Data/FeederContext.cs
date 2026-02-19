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
        public DbSet<SystemSetting> Settings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SystemSetting>().HasKey(s => s.Key);
        }
    }
}
