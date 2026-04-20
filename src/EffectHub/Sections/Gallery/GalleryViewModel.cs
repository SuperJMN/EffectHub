using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using EffectHub.Core.Models;
using EffectHub.Core.Services;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.SourceGenerators;
using Zafiro.Avalonia.Dialogs;
using Zafiro.UI.Commands;

namespace EffectHub.Sections.Gallery;

public partial class GalleryViewModel : ReactiveObject
{
    private readonly ReadOnlyObservableCollection<Effect> effects;
    [Reactive] private string searchText = "";

    public ReadOnlyObservableCollection<Effect> Effects => effects;

    public ReactiveCommand<Effect, Unit> ViewDetailCommand { get; }

    public Action<Effect>? EditEffectCallback { get; set; }

    public GalleryViewModel(IEffectRepository repository, IDialog dialogService)
    {
        repository.Connect()
            .Filter(this.WhenAnyValue(x => x.SearchText)
                .Throttle(TimeSpan.FromMilliseconds(300), AvaloniaScheduler.Instance)
                .Select(CreateFilter))
            .SortBy(e => e.Name)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Bind(out effects)
            .Subscribe();

        ViewDetailCommand = ReactiveCommand.CreateFromTask<Effect>(async effect =>
        {
            var detailVm = new EffectDetailViewModel(effect);
            var confirmed = await dialogService.Show(detailVm, effect.Name, (_, closeable) => new IOption[]
            {
                new Option("Duplicate and Edit", ReactiveCommand.Create(() =>
                {
                    EditEffectCallback?.Invoke(effect);
                    closeable.Close();
                }).Enhance(), new Zafiro.Avalonia.Dialogs.Settings { IsDefault = true }),
                new Option("Close", ReactiveCommand.Create(closeable.Close).Enhance(), new Zafiro.Avalonia.Dialogs.Settings { IsCancel = true }),
            });
        });
    }

    private static Func<Effect, bool> CreateFilter(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return _ => true;
        }

        return effect =>
            effect.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            effect.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            effect.Tags.Any(t => t.Contains(search, StringComparison.OrdinalIgnoreCase));
    }
}
