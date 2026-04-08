using DottIn.Application.Features.Branches.Queries.GetBranchByOwner;
using DottIn.Application.Features.Subscriptions.Services;
using DottIn.Application.Features.TimeKeepings.Commands.ClockIn;
using DottIn.Application.Features.TimeKeepings.Validators;
using DottIn.Domain.Auth;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Employees;
using DottIn.Domain.Exports;
using DottIn.Domain.HolidayCalendars;
using DottIn.Domain.Storage;
using DottIn.Domain.Subscriptions;
using DottIn.Domain.TimeKeepings;
using DottIn.Infra.Data.Contexts;
using DottIn.Infra.Data.Interceptors;
using DottIn.Infra.Data.Repositories;
using DottIn.Infra.Data.UoW;
using DottIn.Infra.Messaging.Consumers;
using DottIn.Infra.Services.Auth;
using DottIn.Infra.Services.Storage;
using DottIn.Infra.Services.Stripe;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AppIStripeService = DottIn.Application.Interfaces.IStripeService;

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

            services.AddValidatorsFromAssemblies(
            [
                typeof(ClockInCommandValidator).Assembly,
                typeof(GetBranchByOwnerQuery).Assembly
            ]);

            services.AddScoped<ITenantSubscriptionService, TenantSubscriptionService>();

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
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IBranchRepository, BranchRepository>();
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<ITimeKeepingRepository, TimeKeepingRepository>();
            services.AddScoped<IHolidayCalendarRepository, HolidayCalendarRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IDominioMappingRepository, DominioMappingRepository>();
            services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
            services.AddScoped<ITenantSubscriptionRepository, TenantSubscriptionRepository>();

            services.Configure<StripeSettings>(configuration.GetSection("Stripe"));
            services.AddScoped<StripeService>();
            services.AddScoped<IStripeService>(sp => sp.GetRequiredService<StripeService>());
            services.AddScoped<AppIStripeService>(sp => sp.GetRequiredService<StripeService>());

            return services;
        }

        public static IServiceCollection AddMassTransitConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var isDisabled = configuration.GetValue<bool>("MassTransit:Disabled");
            
            if (isDisabled)
            {
                // Register no-op IPublishEndpoint for when MassTransit is disabled
                services.AddSingleton<MassTransit.IPublishEndpoint, NoOpPublishEndpoint>();
                return services;
            }
            
            var useInMemory = configuration.GetValue<bool>("MassTransit:UseInMemory");
            
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

                if (useInMemory)
                {
                    cfg.UsingInMemory((context, config) =>
                    {
                        config.ConfigureEndpoints(context);
                    });
                }
                else
                {
                    cfg.UsingRabbitMq((context, config) =>
                    {
                        var rabbitMqConnection = configuration.GetConnectionString("RabbitMQ");

                        config.Host(rabbitMqConnection);

                        config.UseRawJsonSerializer();

                        config.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

                        config.ConfigureEndpoints(context);
                    });
                }
            });

            return services;
        }
    }
    
    /// <summary>
    /// No-op implementation of IPublishEndpoint for local development without MassTransit
    /// </summary>
    public class NoOpPublishEndpoint : MassTransit.IPublishEndpoint
    {
        public MassTransit.ConnectHandle ConnectPublishObserver(MassTransit.IPublishObserver observer) => 
            new NoOpConnectHandle();

        public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class => 
            Task.CompletedTask;

        public Task Publish<T>(T message, MassTransit.IPipe<MassTransit.PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class => 
            Task.CompletedTask;

        public Task Publish<T>(T message, MassTransit.IPipe<MassTransit.PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class => 
            Task.CompletedTask;

        public Task Publish(object message, CancellationToken cancellationToken = default) => 
            Task.CompletedTask;

        public Task Publish(object message, MassTransit.IPipe<MassTransit.PublishContext> publishPipe, CancellationToken cancellationToken = default) => 
            Task.CompletedTask;

        public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default) => 
            Task.CompletedTask;

        public Task Publish(object message, Type messageType, MassTransit.IPipe<MassTransit.PublishContext> publishPipe, CancellationToken cancellationToken = default) => 
            Task.CompletedTask;

        public Task Publish<T>(object values, CancellationToken cancellationToken = default) where T : class => 
            Task.CompletedTask;

        public Task Publish<T>(object values, MassTransit.IPipe<MassTransit.PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class => 
            Task.CompletedTask;

        public Task Publish<T>(object values, MassTransit.IPipe<MassTransit.PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class => 
            Task.CompletedTask;
    }
    
    public class NoOpConnectHandle : MassTransit.ConnectHandle
    {
        public void Disconnect() { }
        public void Dispose() { }
    }
}
