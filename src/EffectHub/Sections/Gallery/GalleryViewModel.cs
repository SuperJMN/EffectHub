using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using EffectHub.Core.Models;
using EffectHub.Core.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace EffectHub.Sections.Gallery;

public partial class GalleryViewModel : ReactiveObject
{
    private readonly IEffectRepository repository;
    private readonly ReadOnlyObservableCollection<Effect> effects;
    [Reactive] private string searchText = "";

    public ReadOnlyObservableCollection<Effect> Effects => effects;

    public GalleryViewModel(IEffectRepository repository)
    {
        this.repository = repository;

        repository.Connect()
            .Filter(this.WhenAnyValue(x => x.SearchText)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .Select(CreateFilter))
            .SortBy(e => e.Name)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Bind(out effects)
            .Subscribe();
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
