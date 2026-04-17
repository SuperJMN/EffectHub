using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EffectHub.Api.Client;
using EffectHub.Core.Compilation;
using EffectHub.Core.Rendering;
using EffectHub.Core.Services;
using EffectHub.Identity;
using EffectHub.Sections.Editor;
using EffectHub.Sections.Gallery;
using EffectHub.Sections.MyEffects;
using EffectHub.Services;
using EffectHub.Storage.Local;
using Microsoft.Extensions.DependencyInjection;
using Zafiro.Avalonia.Dialogs;
using Zafiro.Avalonia.Misc;

namespace EffectHub;

public class App : Application
{
    private const string DefaultApiBaseUrl = "http://localhost:5120";

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        // Identity — Nostr keypair auto-generated on first use
        IKeyStore keyStore;
        if (OperatingSystem.IsBrowser())
        {
            keyStore = new InMemoryKeyStore(); // WASM: in-memory (lost on reload, to be replaced with localStorage interop)
        }
        else
        {
            keyStore = new LocalFileKeyStore();
        }

        var identityService = new NostrIdentityService(keyStore);
        var identityProvider = new NostrIdentityProvider(identityService);
        services.AddSingleton(identityService);
        services.AddSingleton<IIdentityProvider>(identityProvider);
        services.AddSingleton<IIdentitySigner>(identityProvider);

        // Repository — API-backed with local fallback
        var apiBaseUrl = Environment.GetEnvironmentVariable("EFFECTHUB_API_URL") ?? DefaultApiBaseUrl;
        var httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
        var apiRepo = new ApiEffectRepository(httpClient, identityProvider, identityProvider);
        services.AddSingleton<IEffectRepository>(apiRepo);
        services.AddSingleton(apiRepo); // for RefreshAsync access

        // Compiler services
        if (OperatingSystem.IsBrowser())
        {
            services.AddSingleton<ICpuFallbackCompiler, NoOpCpuFallbackCompiler>();
        }
        else
        {
            services.AddSingleton<ICpuFallbackCompiler, CpuFallbackCompiler>();
        }

        services.AddSingleton<IShaderCompiler, ShaderCompiler>();
        services.AddSingleton<IEffectPackager, EffectPackager>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton(DialogService.Create());

        services.AddSingleton<GalleryViewModel>();
        services.AddSingleton<EditorViewModel>();
        services.AddSingleton<MyEffectsViewModel>();

        services.AddSingleton<MainViewModel>();

        var provider = services.BuildServiceProvider();

        // Initialize identity and load effects from API
        _ = InitializeAsync(provider);

        this.Connect(
            () => new MainView(),
            _ => provider.GetRequiredService<MainViewModel>(),
            () => new Window { Title = "EffectHub — Shader Gallery", Width = 1280, Height = 800 });

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task InitializeAsync(IServiceProvider services)
    {
        // Ensure identity is created
        var identity = services.GetRequiredService<NostrIdentityService>();
        await identity.GetOrCreateKeyPairAsync();

        // Load effects from API
        var apiRepo = services.GetRequiredService<ApiEffectRepository>();
        try
        {
            await apiRepo.RefreshAsync();
        }
        catch
        {
            // API unavailable — seed with built-in effects as fallback
            var cache = apiRepo;
            var seedEffects = Core.SeedEffects.GetAll();
            // If no effects loaded, the Gallery will be empty but functional
        }
    }

    private sealed class NoOpCpuFallbackCompiler : ICpuFallbackCompiler
    {
        public CpuFallbackCompilationResult Compile(string csharpCode)
            => CpuFallbackCompilationResult.Failure("CPU fallback compilation is not supported in the browser.");
    }
}
