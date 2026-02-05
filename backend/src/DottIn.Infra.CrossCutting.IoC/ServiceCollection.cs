using DottIn.Application.Features.TimeKeepings.Commands.ClockIn;
using DottIn.Application.Features.TimeKeepings.Validators;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Employees;
using DottIn.Domain.Storage;
using DottIn.Domain.TimeKeepings;
using DottIn.Infra.Data.Contexts;
using DottIn.Infra.Data.Interceptors;
using DottIn.Infra.Data.Repositories;
using DottIn.Infra.Data.UoW;
using DottIn.Infra.Messaging.Consumers;
using DottIn.Infra.Services.Storage;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DottIn.Infra.CrossCutting.IoC
{
    public static class ServiceCollection
    {
        public static IServiceCollection RegisterApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(ClockInCommand).Assembly);
            });

            services.AddValidatorsFromAssembly(typeof(ClockInCommandValidator).Assembly);

            return services;
        }

        public static IServiceCollection RegisterInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<PublishDomainEventsInterceptor>();

            services.AddDbContext<DottInContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<PublishDomainEventsInterceptor>();

                options.UseNpgsql(configuration.GetConnectionString("DottInDb"), npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);

                    npgsqlOptions.CommandTimeout(30);
                });

                options.AddInterceptors(interceptor);
            });

            services.AddScoped<IFileStorageService>(fs => new FileStorageService(configuration["AzureBlob:ConnectionString"]!, configuration["AzureBlob:ContainerName"]!));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IBranchRepository, BranchRepository>();
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<ITimeKeepingRepository, TimeKeepingRepository>();

            return services;
        }

        public static IServiceCollection AddMassTransitConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMassTransit(cfg =>
            {
                cfg.AddConsumer<EmployeeImageConsumer>();

                cfg.AddEntityFrameworkOutbox<DottInContext>(o =>
                {
                    o.UsePostgres();

                    o.QueryDelay = TimeSpan.FromSeconds(10);

                    o.QueryMessageLimit = 50;

                    o.DuplicateDetectionWindow = TimeSpan.FromMinutes(30);

                    o.UseBusOutbox(bo =>
                    {
                        bo.MessageDeliveryLimit = 50;
                    });
                });

                cfg.UsingRabbitMq((context, config) =>
                {
                    var rabbitMqConnection = configuration.GetConnectionString("RabbitMQ");

                    config.Host(rabbitMqConnection);

                    config.UseRawJsonSerializer();

                    config.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

                    config.ConfigureEndpoints(context);
                });
            });

            return services;
        }
    }
}
