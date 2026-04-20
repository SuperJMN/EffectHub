using System;
using Avalonia;
using EffectHub.Desktop.Storage;
using Zafiro.Avalonia.Mcp.AppHost;
using ReactiveUI.Avalonia;

namespace EffectHub.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.SettingsStoreOverride = new IsolatedStorageSettingsStore();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

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
