using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using EffectHub.Core.Models;
using EffectHub.Core.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace EffectHub.Sections.MyEffects;

public partial class MyEffectsViewModel : ReactiveObject
{
    private readonly IEffectRepository repository;
    private readonly ReadOnlyObservableCollection<Effect> effects;
    [Reactive] private Effect? selectedEffect;

    public ReadOnlyObservableCollection<Effect> Effects => effects;

    public ReactiveCommand<string, Unit> DeleteCommand { get; }
    public ReactiveCommand<Effect, Unit> DuplicateCommand { get; }

    public MyEffectsViewModel(IEffectRepository repository)
    {
        this.repository = repository;

        repository.Connect()
            .SortBy(e => e.UpdatedAt, SortDirection.Descending)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Bind(out effects)
            .Subscribe();

        DeleteCommand = ReactiveCommand.CreateFromTask<string>(async id => { await repository.Delete(id); });

        DuplicateCommand = ReactiveCommand.CreateFromTask<Effect>(async effect =>
        {
            var duplicate = effect with
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = $"{effect.Name} (Copy)",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await repository.Save(duplicate);
        });
    }
}
