using System;
using Avalonia;
using Zafiro.Avalonia.Mcp.AppHost;
using ReactiveUI.Avalonia;

namespace EffectHub.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .UseMcpDiagnostics()
#if DEBUG
            .WithDeveloperTools()
#endif
            .UseReactiveUI(_ => { });
}
