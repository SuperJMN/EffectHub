using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EffectHub.Core.Compilation;
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

        var repository = new LocalEffectRepository();
        services.AddSingleton<IEffectRepository>(repository);
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

        var window = new Window
        {
            Title = "EffectHub — Shader Gallery",
            Width = 1280,
            Height = 800,
            Content = new MainView(),
            DataContext = mainVm
        };

        if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = window;
        }

        _ = InitializeAsync(repository);

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task InitializeAsync(LocalEffectRepository repository)
    {
        await repository.LoadAll();

        var existing = await repository.GetAll();
        if (existing.IsSuccess && existing.Value.Count == 0)
        {
            foreach (var seed in Core.SeedEffects.GetAll())
            {
                await repository.Save(seed);
            }
        }
    }
}
