using Avalonia;

namespace ForEachDb.Desktop;

public static class Program
{
    [STAThread]
    public static int Main(string[] args) => Run(args);

    public static int Run(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
