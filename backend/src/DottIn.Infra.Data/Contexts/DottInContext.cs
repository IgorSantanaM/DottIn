using DottIn.Domain.Branches;
using DottIn.Domain.Core.Models;
using DottIn.Domain.Employees;
using DottIn.Domain.TimeKeepings;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Infra.Data.Contexts
{
    public class DottInContext(DbContextOptions<DottInContext> options) : DbContext(options)
    {
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<TimeKeeping> TimeKeepings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<Event<Guid>>();

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DottInContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
        
    }
}
