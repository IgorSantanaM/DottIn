using Microsoft.AspNetCore.Components.WebView.Maui;
using AndroidWebView = Android.Webkit.WebView;

namespace DottIn.Mobile.Platforms.Android;

/// <summary>
/// Custom BlazorWebViewHandler that ensures JavaScript is enabled on the Android WebView.
/// Blazor startup is handled via the polling script in index.html with autostart="false".
/// </summary>
public class CustomBlazorWebViewHandler : BlazorWebViewHandler
{
    protected override void ConnectHandler(AndroidWebView platformView)
    {
        platformView.Settings.JavaScriptEnabled = true;
        base.ConnectHandler(platformView);
    }
}
