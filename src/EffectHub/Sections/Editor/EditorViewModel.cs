using System.Reactive;
using System.Reactive.Linq;
using CSharpFunctionalExtensions;
using EffectHub.Core.Models;
using EffectHub.Core.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Zafiro.Avalonia.Controls.PropertyGrid.ViewModels;

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

    private readonly ObservableAsPropertyHelper<IReadOnlyList<IPropertyItem>> uniformPropertyItems;
    public IReadOnlyList<IPropertyItem> UniformPropertyItems => uniformPropertyItems.Value;

    // Current uniform values keyed by name, for persistence and effect binding
    private readonly Dictionary<string, object?> currentUniformValues = new();

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> NewEffectCommand { get; }

    /// <summary>
    /// Fires whenever a uniform value is changed by the user in the PropertyGrid.
    /// The EditorView subscribes to push values into the DynamicShaderEffect.
    /// </summary>
    public event Action<UniformPropertyItem>? UniformValueChanged;

    private const string DefaultShader =
        """
        // EffectHub — Write your SkSL shader here
        // Use 'content.eval(coord)' to sample the underlying visual
        // Uniforms are auto-detected and shown as controls

        uniform shader content;
        uniform float intensity;
        uniform float red;
        uniform float green;
        uniform float blue;

        half4 main(float2 coord) {
            half4 c = content.eval(coord);
            half3 tint = half3(half(red), half(green), half(blue));
            half3 mixed = mix(c.rgb, tint * c.a, half(intensity));
            return half4(mixed, c.a);
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

        uniformPropertyItems = this.WhenAnyValue(x => x.DetectedUniforms)
            .Select(CreatePropertyItems)
            .ToProperty(this, x => x.UniformPropertyItems, []);

        var canSave = this.WhenAnyValue(x => x.IsCompiled, x => x.EffectName,
            (compiled, name) => compiled && !string.IsNullOrWhiteSpace(name));

        SaveCommand = ReactiveCommand.CreateFromTask(SaveEffect, canSave);
        NewEffectCommand = ReactiveCommand.Create(NewEffect);
    }

    private IReadOnlyList<IPropertyItem> CreatePropertyItems(IReadOnlyList<UniformDefinition> uniforms)
    {
        return uniforms
            .Select(u =>
            {
                var item = new UniformPropertyItem(u, OnUniformChanged);

                // Restore previously edited value if available
                if (currentUniformValues.TryGetValue(u.Name, out var savedValue) && savedValue is not null)
                {
                    item.Value = savedValue;
                }

                return (IPropertyItem)item;
            })
            .ToList();
    }

    private void OnUniformChanged(UniformPropertyItem item)
    {
        currentUniformValues[item.Name] = item.Value;
        UniformValueChanged?.Invoke(item);
    }

    public void LoadEffect(Effect effect)
    {
        EditingEffectId = effect.Id;
        EffectName = effect.Name;
        Description = effect.Description;
        SkslCode = effect.SkslCode;
        Tags = string.Join(", ", effect.Tags);

        // Restore persisted uniform values
        currentUniformValues.Clear();
        if (effect.UniformValues is not null)
        {
            foreach (var kvp in effect.UniformValues)
            {
                currentUniformValues[kvp.Key] = kvp.Value;
            }
        }
    }

    public void NewEffect()
    {
        EditingEffectId = null;
        EffectName = "New Effect";
        Description = "";
        SkslCode = DefaultShader;
        Tags = "";
        currentUniformValues.Clear();
    }

    private async Task SaveEffect()
    {
        var effectId = EditingEffectId ?? Guid.NewGuid().ToString("N");
        var tagList = Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var uniformValues = new Dictionary<string, double>();
        foreach (var item in UniformPropertyItems.OfType<UniformPropertyItem>())
        {
            if (item.Value is double d)
                uniformValues[item.Name] = d;
            else if (item.Value is int i)
                uniformValues[item.Name] = i;
            else if (item.Value is bool b)
                uniformValues[item.Name] = b ? 1.0 : 0.0;
        }

        var effect = new Effect
        {
            Id = effectId,
            Name = EffectName,
            Description = Description,
            SkslCode = SkslCode,
            AuthorAlias = identity.CurrentAlias,
            Tags = tagList,
            Uniforms = DetectedUniforms,
            UniformValues = uniformValues,
            CreatedAt = EditingEffectId is null ? DateTimeOffset.UtcNow : DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await repository.Save(effect);
        EditingEffectId = effectId;
    }
}
