using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using EffectHub.Core.Models;
using EffectHub.Core.Services;
using EffectHub.Services;
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
    public ReactiveCommand<Effect, Unit> ExportCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportCommand { get; }

    public MyEffectsViewModel(IEffectRepository repository, IEffectPackager packager, IFileDialogService fileDialog)
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

        ExportCommand = ReactiveCommand.CreateFromTask<Effect>(async effect =>
        {
            await using var stream = await fileDialog.SaveFile(
                "Export Effect",
                $"{effect.Name}.effecthub",
                ["effecthub"]);

            if (stream is null) return;

            await packager.Export(effect, stream);
        });

        ImportCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await using var stream = await fileDialog.OpenFile(
                "Import Effect",
                ["effecthub"]);

            if (stream is null) return;

            var result = await packager.Import(stream);
            if (result.IsSuccess)
            {
                await repository.Save(result.Value);
            }
        });
    }
}
