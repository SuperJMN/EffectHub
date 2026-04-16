using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using EffectHub.Core.Models;
using EffectHub.Core.Services;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.SourceGenerators;

namespace EffectHub.Sections.Gallery;

public partial class GalleryViewModel : ReactiveObject
{
    private readonly IEffectRepository repository;
    private readonly ReadOnlyObservableCollection<Effect> effects;
    [Reactive] private string searchText = "";
    [Reactive] private Effect? selectedEffect;
    private readonly ObservableAsPropertyHelper<bool> isDetailVisible;
    public bool IsDetailVisible => isDetailVisible.Value;

    public ReadOnlyObservableCollection<Effect> Effects => effects;

    public ReactiveCommand<Effect, Unit> ViewDetailCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseDetailCommand { get; }
    public ReactiveCommand<Effect, Unit> DuplicateAndEditCommand { get; }

    public Action<Effect>? EditEffectCallback { get; set; }

    public GalleryViewModel(IEffectRepository repository)
    {
        this.repository = repository;

        repository.Connect()
            .Filter(this.WhenAnyValue(x => x.SearchText)
                .Throttle(TimeSpan.FromMilliseconds(300), AvaloniaScheduler.Instance)
                .Select(CreateFilter))
            .SortBy(e => e.Name)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Bind(out effects)
            .Subscribe();

        ViewDetailCommand = ReactiveCommand.Create<Effect>(effect => SelectedEffect = effect);

        CloseDetailCommand = ReactiveCommand.Create(() => { SelectedEffect = null; });

        DuplicateAndEditCommand = ReactiveCommand.Create<Effect>(effect =>
        {
            EditEffectCallback?.Invoke(effect);
            SelectedEffect = null;
        });

        isDetailVisible = this.WhenAnyValue(x => x.SelectedEffect)
            .Select(e => e is not null)
            .ToProperty(this, x => x.IsDetailVisible);
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
