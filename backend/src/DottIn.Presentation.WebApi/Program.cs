using DottIn.Presentation.WebApi.Endpoints.Internal;
using DottIn.Infra.CrossCutting.IoC;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.RegisterApplication(builder.Configuration);

builder.Services.RegisterInfrastructure(builder.Configuration);

builder.Services.AddMassTransitConfiguration(builder.Configuration);

var app = builder.Build();

app.UseEndpoints<Program>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/v1/dottin.json");
    app.UseSwaggerUI(opt =>
    {
        opt.SwaggerEndpoint("/openapi/v1/dottin.json", "DottIn API V1");
        opt.DocumentTitle = "DottIn API Documentation";
        opt.DefaultModelExpandDepth(-1);
    });
}

app.UseHttpsRedirection();

app.Run();