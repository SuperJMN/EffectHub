using EffectHub.Core.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace EffectHub.Sections.Gallery;

public partial class EffectDetailViewModel : ReactiveObject
{
    public Effect Effect { get; }
    [Reactive] private string skslCode;

    public EffectDetailViewModel(Effect effect)
    {
        Effect = effect;
        skslCode = effect.SkslCode;
    }
}
