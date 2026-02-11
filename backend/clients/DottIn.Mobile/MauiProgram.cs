using CommunityToolkit.Maui;
using DottIn.Mobile.Services;
using DottIn.Mobile.Services.Interfaces;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Refit;

namespace DottIn.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Inter-Regular.ttf", "InterRegular");
                fonts.AddFont("Inter-Medium.ttf", "InterMedium");
                fonts.AddFont("Inter-SemiBold.ttf", "InterSemiBold");
                fonts.AddFont("Inter-Bold.ttf", "InterBold");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // MudBlazor
        builder.Services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomCenter;
            config.SnackbarConfiguration.PreventDuplicates = true;
            config.SnackbarConfiguration.VisibleStateDuration = 3000;
        });

        // Configuration
        var apiBaseUrl = DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5000"  // Android emulator
            : "http://localhost:5000"; // iOS simulator

        // Register Services
        builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
        builder.Services.AddSingleton<ILocalDatabaseService, LocalDatabaseService>();

        // State Management
        builder.Services.AddSingleton<AppState>();

        // HTTP Client with JWT
        builder.Services.AddHttpClient("DottInApi", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddHttpMessageHandler<AuthorizationHandler>();

        builder.Services.AddTransient<AuthorizationHandler>();

        // Refit API Clients
        builder.Services.AddRefitClient<IAuthApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl));

        builder.Services.AddRefitClient<ITimeKeepingApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<AuthorizationHandler>();

        builder.Services.AddRefitClient<IEmployeeApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<AuthorizationHandler>();

        return builder.Build();
    }
}
