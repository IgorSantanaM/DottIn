using Microsoft.AspNetCore.Components.WebView.Maui;
using AndroidWebView = Android.Webkit.WebView;

namespace DottIn.Mobile.Platforms.Android;

/// <summary>
/// Custom BlazorWebViewHandler that fixes the JavaScript bridge race condition in .NET 10 MAUI.
/// </summary>
public class CustomBlazorWebViewHandler : BlazorWebViewHandler
{
    protected override void ConnectHandler(AndroidWebView platformView)
    {
        platformView.Settings.JavaScriptEnabled = true;
        base.ConnectHandler(platformView);
    }
}
