using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EffectHub.Core.Compilation;
using EffectHub.Core.Rendering;
using EffectHub.Core.Services;
using EffectHub.Sections.Editor;
using EffectHub.Sections.Gallery;
using EffectHub.Sections.MyEffects;
using EffectHub.Services;
using EffectHub.Storage.Local;
using Microsoft.Extensions.DependencyInjection;

namespace EffectHub;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        if (OperatingSystem.IsBrowser())
        {
            var memoryRepo = new InMemoryEffectRepository();
            memoryRepo.Seed(Core.SeedEffects.GetAll());
            services.AddSingleton<IEffectRepository>(memoryRepo);
            services.AddSingleton<ICpuFallbackCompiler, NoOpCpuFallbackCompiler>();
        }
        else
        {
            var localRepo = new LocalEffectRepository();
            services.AddSingleton<IEffectRepository>(localRepo);
            services.AddSingleton<ICpuFallbackCompiler, CpuFallbackCompiler>();
            _ = InitializeAsync(localRepo);
        }

        services.AddSingleton<IShaderCompiler, ShaderCompiler>();
        services.AddSingleton<IIdentityProvider, LocalIdentityProvider>();
        services.AddSingleton<IEffectPackager, EffectPackager>();
        services.AddSingleton<IFileDialogService, FileDialogService>();

        services.AddSingleton<GalleryViewModel>();
        services.AddSingleton<EditorViewModel>();
        services.AddSingleton<MyEffectsViewModel>();

        var provider = services.BuildServiceProvider();

        var mainVm = new MainViewModel(
            provider.GetRequiredService<GalleryViewModel>(),
            provider.GetRequiredService<EditorViewModel>(),
            provider.GetRequiredService<MyEffectsViewModel>());

        if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Window
            {
                Title = "EffectHub — Shader Gallery",
                Width = 1280,
                Height = 800,
                Content = new MainView(),
                DataContext = mainVm
            };
        }
        else if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MainView { DataContext = mainVm };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task InitializeAsync(LocalEffectRepository repository)
    {
        await repository.LoadAll();

        var existing = await repository.GetAll();
        if (existing.IsSuccess)
        {
            var existingIds = existing.Value.Select(e => e.Id).ToHashSet();
            foreach (var seed in Core.SeedEffects.GetAll())
            {
                if (!existingIds.Contains(seed.Id))
                {
                    await repository.Save(seed);
                }
            }
        }
    }

    private sealed class NoOpCpuFallbackCompiler : ICpuFallbackCompiler
    {
        public CpuFallbackCompilationResult Compile(string csharpCode)
            => CpuFallbackCompilationResult.Failure("CPU fallback compilation is not supported in the browser.");
    }
}
