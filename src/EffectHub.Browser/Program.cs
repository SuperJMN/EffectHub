using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using EffectHub;
using EffectHub.Browser.Storage;
using ReactiveUI.Avalonia;

[assembly: SupportedOSPlatform("browser")]

namespace EffectHub.Browser;

internal sealed partial class Program
{
    private static async Task Main(string[] args)
    {
        App.SettingsStoreOverride = new LocalStorageSettingsStore();
        await BuildAvaloniaApp()
            .WithInterFont()
            .UseReactiveUI(_ => { })
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}
