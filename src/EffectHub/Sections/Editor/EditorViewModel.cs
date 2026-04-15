using System.Reactive;
using System.Reactive.Linq;
using CSharpFunctionalExtensions;
using EffectHub.Core.Models;
using EffectHub.Core.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace EffectHub.Sections.Editor;

public enum TestSurface
{
    WarmGradient,
    CoolGradient,
    DarkSolid,
    LightSolid,
    Checkerboard,
    Photo,
}

public partial class EditorViewModel : ReactiveObject
{
    private readonly IShaderCompiler compiler;
    private readonly IEffectRepository repository;
    private readonly IIdentityProvider identity;

    [Reactive] private string effectName = "New Effect";
    [Reactive] private string description = "";
    [Reactive] private string skslCode = DefaultShader;
    [Reactive] private string? compilationError;
    [Reactive] private bool isCompiled;
    [Reactive] private string tags = "";
    [Reactive] private string? editingEffectId;
    [Reactive] private TestSurface selectedSurface = TestSurface.WarmGradient;

    public TestSurface[] AvailableSurfaces { get; } = Enum.GetValues<TestSurface>();

    private readonly ObservableAsPropertyHelper<IReadOnlyList<UniformDefinition>> detectedUniforms;
    public IReadOnlyList<UniformDefinition> DetectedUniforms => detectedUniforms.Value;

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> NewEffectCommand { get; }

    private const string DefaultShader =
        """
        // EffectHub — Write your SkSL shader here
        // Uniforms are auto-detected and shown as controls

        uniform float intensity;
        uniform float red;
        uniform float green;
        uniform float blue;

        half4 main(float2 coord) {
            half premul = half(intensity);
            return half4(half(red) * premul, half(green) * premul, half(blue) * premul, premul);
        }
        """;

    public EditorViewModel(IShaderCompiler compiler, IEffectRepository repository, IIdentityProvider identity)
    {
        this.compiler = compiler;
        this.repository = repository;
        this.identity = identity;

        var compilationResults = this.WhenAnyValue(x => x.SkslCode)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => compiler.Compile(code))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Publish()
            .RefCount();

        compilationResults
            .Subscribe(result =>
            {
                if (result.IsSuccess)
                {
                    var compilation = result.Value;
                    IsCompiled = compilation.IsSuccess;
                    CompilationError = compilation.IsSuccess ? null : compilation.ErrorMessage;
                }
                else
                {
                    IsCompiled = false;
                    CompilationError = result.Error;
                }
            });

        detectedUniforms = compilationResults
            .Select(r => r.IsSuccess && r.Value.IsSuccess ? r.Value.DetectedUniforms : (IReadOnlyList<UniformDefinition>)[])
            .ToProperty(this, x => x.DetectedUniforms, []);

        var canSave = this.WhenAnyValue(x => x.IsCompiled, x => x.EffectName,
            (compiled, name) => compiled && !string.IsNullOrWhiteSpace(name));

        SaveCommand = ReactiveCommand.CreateFromTask(SaveEffect, canSave);
        NewEffectCommand = ReactiveCommand.Create(NewEffect);
    }

    public void LoadEffect(Effect effect)
    {
        EditingEffectId = effect.Id;
        EffectName = effect.Name;
        Description = effect.Description;
        SkslCode = effect.SkslCode;
        Tags = string.Join(", ", effect.Tags);
    }

    public void NewEffect()
    {
        EditingEffectId = null;
        EffectName = "New Effect";
        Description = "";
        SkslCode = DefaultShader;
        Tags = "";
    }

    private async Task SaveEffect()
    {
        var effectId = EditingEffectId ?? Guid.NewGuid().ToString("N");
        var tagList = Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var effect = new Effect
        {
            Id = effectId,
            Name = EffectName,
            Description = Description,
            SkslCode = SkslCode,
            AuthorAlias = identity.CurrentAlias,
            Tags = tagList,
            Uniforms = DetectedUniforms,
            CreatedAt = EditingEffectId is null ? DateTimeOffset.UtcNow : DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await repository.Save(effect);
        EditingEffectId = effectId;
    }
}
