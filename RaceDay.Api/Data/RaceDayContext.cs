using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data
{
    public class RaceDayContext : DbContext
    {
        public RaceDayContext(DbContextOptions<RaceDayContext> options) : base(options)
        {
        }

        public DbSet<Organiser> Organisers { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<RaceRoute> RaceRoutes { get; set; }
        public DbSet<Enrolment> Enrolments { get; set; }
        public DbSet<Result> Results { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // these match the UNIQUE columns in the Part 1 SQL script
            modelBuilder.Entity<Organiser>().HasIndex(o => o.Email).IsUnique();
            modelBuilder.Entity<Participant>().HasIndex(p => p.Email).IsUnique();
            modelBuilder.Entity<Enrolment>().HasIndex(e => e.RaceNumber).IsUnique();
            modelBuilder.Entity<RaceRoute>().HasIndex(r => r.CategoryId).IsUnique();
            modelBuilder.Entity<Result>().HasIndex(r => r.EnrolmentId).IsUnique();
        }
    }
}
