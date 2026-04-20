using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EffectHub.Api.Client;
using EffectHub.Core.Compilation;
using EffectHub.Core.Configuration;
using EffectHub.Core.Rendering;
using EffectHub.Core.Services;
using EffectHub.Identity;
using EffectHub.Sections.Editor;
using EffectHub.Sections.Gallery;
using EffectHub.Sections.MyEffects;
using EffectHub.Sections.Settings;
using EffectHub.Services;
using EffectHub.Storage.Local;
using Microsoft.Extensions.DependencyInjection;
using Zafiro.Avalonia.Dialogs;
using Zafiro.Avalonia.Misc;
using Zafiro.Settings;

namespace EffectHub;

public class App : Application
{
    private const string DesktopFallbackApiBaseUrl = "http://localhost:5120";

    /// <summary>
    /// Per-platform persistence backend for <see cref="ISettings{T}"/>. Each head (Desktop, Browser,
    /// Android, …) MUST set this before <c>BuildAvaloniaApp().Start*</c> so that <see cref="App"/>
    /// can wire up settings without knowing platform-specific storage APIs.
    /// </summary>
    public static ISettingsStore? SettingsStoreOverride { get; set; }

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

        // User-editable settings (API URL, …). The persistence backend is supplied by the head
        // via App.SettingsStoreOverride. If a head forgets to set it, fall back to a no-op
        // in-memory store so the app keeps working (settings just won't persist across runs).
        var store = SettingsStoreOverride ?? new InMemorySettingsStore();
        var settings = new JsonSettings<EffectHubSettings>(
            "effecthub.settings.json",
            store,
            () => new EffectHubSettings());
        var endpointProvider = new ApiEndpointProvider(settings);
        services.AddSingleton<ISettingsStore>(store);
        services.AddSingleton<ISettings<EffectHubSettings>>(settings);
        services.AddSingleton<IApiEndpointProvider>(endpointProvider);

        // Resolve the initial API URL: Desktop accepts an env-var override and falls back to
        // localhost; WASM uses whatever the user has configured (empty until first run).
        var initialUrl = ResolveInitialApiUrl(endpointProvider);

        var httpClient = new HttpClient();
        if (!string.IsNullOrWhiteSpace(initialUrl))
        {
            httpClient.BaseAddress = new Uri(initialUrl);
        }

        var apiRepo = new ApiEffectRepository(httpClient, identityProvider, identityProvider);
        services.AddSingleton<IEffectRepository>(apiRepo);
        services.AddSingleton(apiRepo); // for RefreshAsync access

        // React to URL changes (Settings screen / first-run dialog) by retargeting the HttpClient
        // and refreshing the cached effect list. Safe between requests.
        endpointProvider.Changes.Subscribe(url =>
        {
            if (string.IsNullOrWhiteSpace(url))
                return;
            try
            {
                httpClient.BaseAddress = new Uri(url);
                _ = apiRepo.RefreshAsync();
            }
            catch
            {
                // Ignored: invalid URL leaves BaseAddress untouched. The Settings screen surfaces
                // the issue via "Test connection".
            }
        });

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
        services.AddSingleton<SettingsViewModel>();

        services.AddSingleton<MainViewModel>();

        var provider = services.BuildServiceProvider();

        // Initialize identity and load effects from API
        _ = InitializeAsync(provider);

        this.Connect(
            () => new MainView(),
            _ => provider.GetRequiredService<MainViewModel>(),
            () => new Window { Title = "EffectHub — Shader Gallery", Width = 1280, Height = 800 });

        // First-run prompt for the API URL. We wait until after the main view is connected so
        // DialogService.Create() can resolve the AdornerLayer / TopLevel.
        if (!endpointProvider.IsConfigured && OperatingSystem.IsBrowser())
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(
                () => _ = PromptForApiUrlAsync(provider),
                Avalonia.Threading.DispatcherPriority.Loaded);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task PromptForApiUrlAsync(IServiceProvider services)
    {
        var dialogService = services.GetRequiredService<IDialog>();
        var settings = services.GetRequiredService<ISettings<EffectHubSettings>>();

        var vm = new EffectHub.Sections.Settings.ApiUrlPromptViewModel();
        var result = await dialogService.ShowAndGetResult(
            vm,
            "Configure EffectHub backend",
            model => model.TrimmedUrl,
            icon: "🛰️");

        if (result.HasValue)
        {
            var url = result.Value;
            if (!string.IsNullOrWhiteSpace(url))
                settings.Update(s => s with { ApiBaseUrl = url });
        }
    }

    private static string ResolveInitialApiUrl(IApiEndpointProvider endpointProvider)
    {
        // Desktop allows EFFECTHUB_API_URL to override persisted settings (handy for CI / scripts).
        if (!OperatingSystem.IsBrowser())
        {
            var envUrl = Environment.GetEnvironmentVariable("EFFECTHUB_API_URL");
            if (!string.IsNullOrWhiteSpace(envUrl))
                return envUrl;
        }

        if (endpointProvider.IsConfigured)
            return endpointProvider.CurrentUrl;

        // No persisted setting: Desktop defaults to local API; WASM stays empty (Settings screen
        // will guide the user to configure it).
        return OperatingSystem.IsBrowser() ? string.Empty : DesktopFallbackApiBaseUrl;
    }

    private static async Task InitializeAsync(IServiceProvider services)
    {
        // Ensure identity is created
        var identity = services.GetRequiredService<NostrIdentityService>();
        await identity.GetOrCreateKeyPairAsync();

        // Load effects from API; fall back to built-in seed effects when API is unavailable
        var apiRepo = services.GetRequiredService<ApiEffectRepository>();
        try
        {
            await apiRepo.RefreshAsync();
        }
        catch
        {
            // ignored — RefreshAsync only populates cache on success
        }

        if (apiRepo.Count == 0)
        {
            apiRepo.SeedLocal(Core.SeedEffects.GetAll());
        }
    }

    private sealed class NoOpCpuFallbackCompiler : ICpuFallbackCompiler
    {
        public CpuFallbackCompilationResult Compile(string csharpCode)
            => CpuFallbackCompilationResult.Failure("CPU fallback compilation is not supported in the browser.");
    }

    /// <summary>
    /// Last-resort settings store used when no head registered one. Keeps the app functional but
    /// settings won't survive a restart.
    /// </summary>
    private sealed class InMemorySettingsStore : ISettingsStore
    {
        private readonly Dictionary<string, object?> map = new();
        private readonly object gate = new();

        public CSharpFunctionalExtensions.Result<T> Load<T>(string path, Func<T> createDefault)
        {
            lock (gate)
            {
                if (map.TryGetValue(path, out var existing) && existing is T typed)
                    return CSharpFunctionalExtensions.Result.Success(typed);

                var def = createDefault();
                map[path] = def;
                return CSharpFunctionalExtensions.Result.Success(def);
            }
        }

        public CSharpFunctionalExtensions.Result Save<T>(string path, T instance)
        {
            lock (gate)
            {
                map[path] = instance;
                return CSharpFunctionalExtensions.Result.Success();
            }
        }
    }
}
