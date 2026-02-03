using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Employees;
using DottIn.Domain.TimeKeepings;
using DottIn.Infra.Data.Contexts;
using DottIn.Infra.Data.Interceptors;
using DottIn.Infra.Data.Repositories;
using DottIn.Infra.Data.UoW;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DottIn.Infra.Data.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfraData(this IServiceCollection services, string connectionString)
        {
            services.AddScoped<PublishDomainEventsInterceptor>();

            services.AddDbContext<DottInContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<PublishDomainEventsInterceptor>();

                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);

                    npgsqlOptions.CommandTimeout(30);
                });

                options.AddInterceptors(interceptor);
            });

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IBranchRepository, BranchRepository>();
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<ITimeKeepingRepository, TimeKeepingRepository>();

            return services;
        }
    }
}
