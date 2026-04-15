using System.Diagnostics;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using SkiaSharp;

namespace EffectHub.Controls.EffectPreview;

/// <summary>
/// Renders a SkSL shader directly via SkiaSharp, without Effector IL weaving.
/// Works on all platforms including WASM.
/// </summary>
public sealed partial class SkiaShaderCanvas : Avalonia.Controls.Control
{
    private static readonly Stopwatch SharedClock = Stopwatch.StartNew();
    private static DispatcherTimer? s_animationTimer;
    private static readonly List<WeakReference<SkiaShaderCanvas>> s_instances = [];

    public static readonly StyledProperty<string?> SkslCodeProperty =
        AvaloniaProperty.Register<SkiaShaderCanvas, string?>(nameof(SkslCode));

    public static readonly StyledProperty<IReadOnlyDictionary<string, double>?> UniformDefaultsProperty =
        AvaloniaProperty.Register<SkiaShaderCanvas, IReadOnlyDictionary<string, double>?>(nameof(UniformDefaults));

    public string? SkslCode
    {
        get => GetValue(SkslCodeProperty);
        set => SetValue(SkslCodeProperty, value);
    }

    public IReadOnlyDictionary<string, double>? UniformDefaults
    {
        get => GetValue(UniformDefaultsProperty);
        set => SetValue(UniformDefaultsProperty, value);
    }

    private SKRuntimeEffect? _compiledEffect;
    private bool _hasTimeUniform;
    private bool _hasContentChild;

    static SkiaShaderCanvas()
    {
        AffectsRender<SkiaShaderCanvas>(SkslCodeProperty);
        AffectsRender<SkiaShaderCanvas>(UniformDefaultsProperty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SkslCodeProperty)
            RecompileShader();
    }

    private void RecompileShader()
    {
        _compiledEffect?.Dispose();
        _compiledEffect = null;
        _hasTimeUniform = false;
        _hasContentChild = false;

        var code = SkslCode;
        if (string.IsNullOrWhiteSpace(code)) return;

        _compiledEffect = SKRuntimeEffect.CreateShader(code, out _);
        if (_compiledEffect is null) return;

        _hasTimeUniform = TimeUniformRegex().IsMatch(code);
        _hasContentChild = _compiledEffect.Children.Contains("content");

        if (_hasTimeUniform)
            RegisterForAnimation(this);
    }

    public override void Render(DrawingContext context)
    {
        context.Custom(new ShaderDrawOperation(
            new Rect(Bounds.Size),
            _compiledEffect,
            _hasContentChild,
            UniformDefaults,
            _hasTimeUniform ? (float)SharedClock.Elapsed.TotalSeconds : 0f));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        RecompileShader();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _compiledEffect?.Dispose();
        _compiledEffect = null;
    }

    private static void RegisterForAnimation(SkiaShaderCanvas instance)
    {
        s_instances.Add(new WeakReference<SkiaShaderCanvas>(instance));

        if (s_animationTimer != null) return;

        s_animationTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(1000.0 / 30),
            DispatcherPriority.Render,
            OnAnimationTick);
        s_animationTimer.Start();
    }

    private static void OnAnimationTick(object? sender, EventArgs e)
    {
        for (var i = s_instances.Count - 1; i >= 0; i--)
        {
            if (s_instances[i].TryGetTarget(out var canvas) && canvas._hasTimeUniform)
                canvas.InvalidateVisual();
            else
                s_instances.RemoveAt(i);
        }

        if (s_instances.Count == 0)
        {
            s_animationTimer?.Stop();
            s_animationTimer = null;
        }
    }

    [GeneratedRegex(@"\buniform\s+\w+\s+time\s*;")]
    private static partial Regex TimeUniformRegex();

    private sealed class ShaderDrawOperation : ICustomDrawOperation
    {
        private readonly Rect _bounds;
        private readonly SKRuntimeEffect? _effect;
        private readonly bool _hasContentChild;
        private readonly IReadOnlyDictionary<string, double>? _uniformDefaults;
        private readonly float _time;

        public ShaderDrawOperation(Rect bounds, SKRuntimeEffect? effect, bool hasContentChild,
            IReadOnlyDictionary<string, double>? uniformDefaults, float time)
        {
            _bounds = bounds;
            _effect = effect;
            _hasContentChild = hasContentChild;
            _uniformDefaults = uniformDefaults;
            _time = time;
        }

        public Rect Bounds => _bounds;

        public void Dispose() { }

        public bool Equals(ICustomDrawOperation? other) => false;

        public bool HitTest(Point p) => _bounds.Contains(p);

        public void Render(ImmediateDrawingContext context)
        {
            if (_effect is null) return;

            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature is null) return;

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;

            var w = (float)_bounds.Width;
            var h = (float)_bounds.Height;
            if (w <= 0 || h <= 0) return;

            var uniforms = new SKRuntimeEffectUniforms(_effect);

            foreach (var name in _effect.Uniforms)
            {
                switch (name)
                {
                    case "iResolution":
                        uniforms.Add("iResolution", new[] { w, h });
                        break;
                    case "width":
                        uniforms.Add("width", w);
                        break;
                    case "height":
                        uniforms.Add("height", h);
                        break;
                    case "time":
                        uniforms.Add("time", _time);
                        break;
                    default:
                        if (_uniformDefaults != null && _uniformDefaults.TryGetValue(name, out var val))
                            uniforms.Add(name, (float)val);
                        else
                            uniforms.Add(name, 0.5f);
                        break;
                }
            }

            SKShader? contentShader = null;
            try
            {
                var children = new SKRuntimeEffectChildren(_effect);
                if (_hasContentChild)
                {
                    contentShader = CreateContentShader(w, h);
                    children.Add("content", contentShader);
                }

                using var shader = _effect.ToShader(uniforms, children);
                if (shader is null) return;

                using var paint = new SKPaint { Shader = shader };
                canvas.DrawRect(SKRect.Create(w, h), paint);
            }
            finally
            {
                contentShader?.Dispose();
            }
        }

        private static SKShader CreateContentShader(float w, float h)
        {
            return SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(w, h),
                [new SKColor(80, 140, 220), new SKColor(220, 120, 180), new SKColor(140, 200, 120)],
                [0f, 0.5f, 1f],
                SKShaderTileMode.Clamp);
        }
    }
}
