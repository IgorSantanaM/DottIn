namespace DottIn.Mobile;

public partial class App : Application
{
    private readonly AppLifecycleService _lifecycle;
    public App(AppLifecycleService lifecycle)
    {
        _lifecycle = lifecycle;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new MainPage()) { Title = "DottIn" };
        window.Resumed += (_, _) => _lifecycle.NotifyResumed();
        return window;
    }
}
