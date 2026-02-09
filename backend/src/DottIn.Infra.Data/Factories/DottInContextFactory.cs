using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DottIn.Infra.Data.Factories
{
    public class DottInContextFactory : IDesignTimeDbContextFactory<DottInContext>
    {
        public DottInContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<DottInContext>();
            var connectionString = configuration.GetConnectionString("DottInDb");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Could not find a connection string named 'DottInDb'.");
            }

            optionsBuilder.UseNpgsql(connectionString);

            return new DottInContext(optionsBuilder.Options);
        }
    }
}
