using CommunityToolkit.Maui;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Refit;
using System.Reflection;

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
            })
            .ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                handlers.AddHandler<BlazorWebView, DottIn.Mobile.Platforms.Android.CustomBlazorWebViewHandler>();
#endif
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomCenter;
            config.SnackbarConfiguration.PreventDuplicates = true;
            config.SnackbarConfiguration.VisibleStateDuration = 3000;
        });

        var metadata = typeof(MauiProgram).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .ToArray();
        string? GetMetadata(string key) => metadata.FirstOrDefault(attribute => attribute.Key == key)?.Value;

        var configuredApiBaseUrl = GetMetadata("DottInApiBaseUrl");
        builder.Services.AddSingleton(new ProfileExternalLinks(
            GetMetadata("DottInTermsUrl"),
            GetMetadata("DottInPrivacyUrl"),
            GetMetadata("DottInSupportUrl")));

#if DEBUG
        var apiBaseUrl = string.IsNullOrWhiteSpace(configuredApiBaseUrl)
            ? (DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5101" : "http://localhost:5101")
            : configuredApiBaseUrl;
#else
        var apiBaseUrl = configuredApiBaseUrl
            ?? throw new InvalidOperationException("Configure DottInApiBaseUrl para gerar o aplicativo de produção.");
#endif

        if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri)
#if !DEBUG
            || apiUri.Scheme != Uri.UriSchemeHttps
#endif
            )
            throw new InvalidOperationException("O endereço da API do DottIn é inválido ou inseguro.");

        builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();

        builder.Services.AddSingleton<AppState>();
        builder.Services.AddSingleton<BranchClockService>();

        builder.Services.AddTransient<AuthorizationHandler>();

        builder.Services.AddRefitClient<IAuthApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<AuthorizationHandler>();

        builder.Services.AddRefitClient<ITimeKeepingApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<AuthorizationHandler>();

        builder.Services.AddRefitClient<IEmployeeApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<AuthorizationHandler>();

        builder.Services.AddRefitClient<IBranchApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<AuthorizationHandler>();

        builder.Services.AddRefitClient<IHolidayCalendarApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<AuthorizationHandler>();

        builder.Services.AddRefitClient<IExportApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<AuthorizationHandler>();

        return builder.Build();
    }
}
