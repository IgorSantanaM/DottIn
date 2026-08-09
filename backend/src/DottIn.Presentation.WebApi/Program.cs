using DottIn.Presentation.WebApi.Endpoints.Internal;
using DottIn.Infra.CrossCutting.IoC;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using DottIn.Infra.Data.Contexts;
using DottIn.Presentation.WebApi.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using System.Text.Json.Serialization;
using DottIn.Presentation.WebApi.Security;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

ProductionConfigurationValidator.Validate(builder.Configuration, builder.Environment);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length > 0)
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserContext>();
builder.Services.AddScoped<TenantAccessService>();
builder.Services.AddScoped<TenantAuthorizationFilter>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("public-auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

builder.Services.RegisterApplication(builder.Configuration);

builder.Services.RegisterInfrastructure(builder.Configuration);

// Always call AddMassTransitConfiguration - it handles disabled mode internally
builder.Services.AddMassTransitConfiguration(builder.Configuration);

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints<Program>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opt =>
    {
        opt.SwaggerEndpoint("/swagger/v1/swagger.json", "Rental Motorcycle API V1");
        opt.DocumentTitle = "DottIn API";
        opt.DefaultModelExpandDepth(-1);
    });
}

if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", app.Environment.IsDevelopment()))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<DottInContext>();

    await dbContext.Database.MigrateAsync();
}

app.MapGet("/", () => Results.Ok(new { service = "DottIn API", status = "healthy" }));
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }))
    .AllowAnonymous()
    .ExcludeFromDescription();

app.MapGet("/health/ready", async (DottInContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ready" })
        : Results.Json(new { status = "unavailable", dependency = "database" }, statusCode: StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous()
    .ExcludeFromDescription();

app.Run();
