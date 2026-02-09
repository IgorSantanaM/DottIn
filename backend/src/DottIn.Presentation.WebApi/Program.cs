using DottIn.Presentation.WebApi.Endpoints.Internal;
using DottIn.Infra.CrossCutting.IoC;
using Microsoft.IdentityModel.Tokens.Experimental;
using Microsoft.EntityFrameworkCore;
using DottIn.Infra.Data.Contexts;
using DottIn.Presentation.WebApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
        policy.AllowAnyOrigin();
    });
});

builder.Services.RegisterApplication(builder.Configuration);

builder.Services.RegisterInfrastructure(builder.Configuration);

builder.Services.AddMassTransitConfiguration(builder.Configuration);

var app = builder.Build();

app.UseEndpoints<Program>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opt =>
    {
        opt.SwaggerEndpoint("/swagger/v1/swagger.json", "Rental Motorcycle API V1");
        opt.DocumentTitle = "Rental Motorcycle Documentation";
        opt.DefaultModelExpandDepth(-1);
    });
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DottInContext>();

    await dbContext.Database.MigrateAsync();
}

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseHttpsRedirection();

app.Run();