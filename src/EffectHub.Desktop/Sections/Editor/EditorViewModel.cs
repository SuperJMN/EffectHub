using System.Reactive;
using System.Reactive.Linq;
using CSharpFunctionalExtensions;
using EffectHub.Core.Models;
using EffectHub.Core.Rendering;
using EffectHub.Core.Services;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.SourceGenerators;
using Zafiro.Avalonia.Controls.PropertyGrid.ViewModels;

namespace EffectHub.Sections.Editor;

public enum TestSurface
{
    WarmGradient,
    CoolGradient,
    DarkSolid,
    LightSolid,
    White,
    Black,
    Checkerboard,
    Transparent,
}

public partial class EditorViewModel : ReactiveObject
{
    private readonly IShaderCompiler compiler;
    private readonly ICpuFallbackCompiler cpuCompiler;
    private readonly IEffectRepository repository;
    private readonly IIdentityProvider identity;

    [Reactive] private string effectName = "New Effect";
    [Reactive] private string description = "";
    [Reactive] private string skslCode = DefaultShader;
    [Reactive] private string cpuFallbackCode = DefaultCpuFallback;
    [Reactive] private string xamlContent = DefaultXamlContent;
    [Reactive] private string? compilationError;
    [Reactive] private string? cpuCompilationError;
    [Reactive] private string? xamlParseError;
    [Reactive] private bool isCompiled;
    [Reactive] private bool isCpuCompiled;
    [Reactive] private bool isGpuRunning = true;
    [Reactive] private bool isCpuRunning;
    [Reactive] private string tags = "";
    [Reactive] private string? editingEffectId;
    [Reactive] private TestSurface selectedSurface = TestSurface.Black;

    public TestSurface[] AvailableSurfaces { get; } = Enum.GetValues<TestSurface>();

    private readonly ObservableAsPropertyHelper<IReadOnlyList<UniformDefinition>> detectedUniforms;
    public IReadOnlyList<UniformDefinition> DetectedUniforms => detectedUniforms.Value;

    private readonly ObservableAsPropertyHelper<IReadOnlyList<IPropertyItem>> uniformPropertyItems;
    public IReadOnlyList<IPropertyItem> UniformPropertyItems => uniformPropertyItems.Value;

    private readonly ObservableAsPropertyHelper<ICpuFallbackRenderer?> activeCpuRenderer;
    public ICpuFallbackRenderer? ActiveCpuRenderer => activeCpuRenderer.Value;

    private readonly Dictionary<string, object?> currentUniformValues = new();

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> NewEffectCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleGpuCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleCpuCommand { get; }

    public event Action<UniformPropertyItem>? UniformValueChanged;

    /// <summary>
    /// Fires when XAML content changes and is successfully parsed.
    /// The EditorView subscribes to update the preview container content.
    /// </summary>
    public event Action<string>? XamlContentParsed;

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

    private const string DefaultCpuFallback =
        """
        // CPU fallback using SKCanvas/SKPaint
        // Available parameters: canvas, contentImage, rect,
        //   width, height, time, floats[], colors[], bools[], ints[]

        if (contentImage != null)
        {
            using var paint = new SKPaint();
            canvas.DrawImage(contentImage, rect, paint);
        }

        // Example: simple color tint overlay
        var intensity = floats.Length > 0 ? floats[0] : 0.5f;
        var r = floats.Length > 1 ? floats[1] : 1f;
        var g = floats.Length > 2 ? floats[2] : 0f;
        var b = floats.Length > 3 ? floats[3] : 0f;
        var alpha = (byte)(intensity * 128);
        using var tintPaint = new SKPaint
        {
            Color = new SKColor((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), alpha),
            BlendMode = SKBlendMode.SrcOver
        };
        canvas.DrawRect(rect, tintPaint);
        """;

    private const string DefaultXamlContent =
        """
        <Button Content="Hello, World!" Padding="24" FontSize="20" HorizontalAlignment="Center" VerticalAlignment="Center" />
        """;

    public EditorViewModel(IShaderCompiler compiler, ICpuFallbackCompiler cpuCompiler, IEffectRepository repository, IIdentityProvider identity)
    {
        this.compiler = compiler;
        this.cpuCompiler = cpuCompiler;
        this.repository = repository;
        this.identity = identity;

        // GPU (SkSL) compilation pipeline
        var compilationResults = this.WhenAnyValue(x => x.SkslCode)
            .Throttle(TimeSpan.FromMilliseconds(500), AvaloniaScheduler.Instance)
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

        // CPU fallback compilation pipeline
        var cpuCompilationResults = this.WhenAnyValue(x => x.CpuFallbackCode)
            .Throttle(TimeSpan.FromMilliseconds(500), AvaloniaScheduler.Instance)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => cpuCompiler.Compile(code))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Publish()
            .RefCount();

        cpuCompilationResults
            .Subscribe(result =>
            {
                IsCpuCompiled = result.IsSuccess;
                CpuCompilationError = result.IsSuccess ? null : result.ErrorMessage;
            });

        activeCpuRenderer = cpuCompilationResults
            .Select(r => r.Renderer)
            .ToProperty(this, x => x.ActiveCpuRenderer);

        // XAML content change notification
        this.WhenAnyValue(x => x.XamlContent)
            .Throttle(TimeSpan.FromMilliseconds(300), AvaloniaScheduler.Instance)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(xaml => XamlContentParsed?.Invoke(xaml));

        var canSave = this.WhenAnyValue(x => x.IsCompiled, x => x.EffectName,
            (compiled, name) => compiled && !string.IsNullOrWhiteSpace(name));

        SaveCommand = ReactiveCommand.CreateFromTask(SaveEffect, canSave);
        NewEffectCommand = ReactiveCommand.Create(NewEffect);
        ToggleGpuCommand = ReactiveCommand.Create(() => { IsGpuRunning = !IsGpuRunning; });
        ToggleCpuCommand = ReactiveCommand.Create(() => { IsCpuRunning = !IsCpuRunning; });
    }

    private IReadOnlyList<IPropertyItem> CreatePropertyItems(IReadOnlyList<UniformDefinition> uniforms)
    {
        return uniforms
            .Select(u =>
            {
                var item = new UniformPropertyItem(u, OnUniformChanged);

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
        CpuFallbackCode = effect.CpuFallbackCode ?? DefaultCpuFallback;
        Tags = string.Join(", ", effect.Tags);

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
        CpuFallbackCode = DefaultCpuFallback;
        XamlContent = DefaultXamlContent;
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
            CpuFallbackCode = string.IsNullOrWhiteSpace(CpuFallbackCode) ? null : CpuFallbackCode,
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
