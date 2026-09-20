namespace DottIn.Mobile.Services;

public sealed class AppLifecycleService
{
    public event Action? Resumed;
    public void NotifyResumed() => Resumed?.Invoke();
}
