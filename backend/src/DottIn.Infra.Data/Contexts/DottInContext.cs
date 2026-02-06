using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Domain.HolidayCalendars;
using DottIn.Domain.TimeKeepings;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Infra.Data.Contexts
{
    public class DottInContext(DbContextOptions<DottInContext> options) : DbContext(options)
    {
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<TimeKeeping> TimeKeepings { get; set; }
        public DbSet<HolidayCalendar> HolidayCalendars { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<Domain.Core.Models.Event<Guid>>();

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DottInContext).Assembly);

            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();

            base.OnModelCreating(modelBuilder);
        }

    }
}
