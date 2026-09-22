using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using DottIn.Admin;
using DottIn.Admin.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddScoped<AdminState>();
builder.Services.AddScoped<SessionStorageService>();
builder.Services.AddScoped<DashboardSessionCache>();
builder.Services.AddScoped<BrowserGeolocationService>();
builder.Services.AddScoped<BrowserDownloadService>();
builder.Services.AddScoped(sp => new AuthService(
    new HttpClient { BaseAddress = new Uri(apiBaseUrl) },
    sp.GetRequiredService<AdminState>()));
builder.Services.AddScoped<AdminAuthorizationHandler>();
builder.Services.AddScoped(sp =>
{
    var authorizationHandler = sp.GetRequiredService<AdminAuthorizationHandler>();
    authorizationHandler.InnerHandler = new HttpClientHandler();

    return new HttpClient(authorizationHandler)
    {
        BaseAddress = new Uri(apiBaseUrl)
    };
});
builder.Services.AddScoped<AdminQueryCache>();
builder.Services.AddScoped<AdminApiClient>();
builder.Services.AddScoped<BranchClockService>();
builder.Services.AddScoped<OperationalAccessService>();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.VisibleStateDuration = 3000;
});

await builder.Build().RunAsync();
