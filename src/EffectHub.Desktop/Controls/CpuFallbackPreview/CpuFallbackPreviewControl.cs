using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using EffectHub.Core.Rendering;
using SkiaSharp;

namespace EffectHub.Controls.CpuFallbackPreview;

/// <summary>
/// A control that renders the CPU fallback effect using the compiled ICpuFallbackRenderer.
/// It captures its visual child as an SKImage and passes it to the renderer.
/// </summary>
public class CpuFallbackPreviewControl : Decorator
{
    public static readonly StyledProperty<ICpuFallbackRenderer?> RendererProperty =
        AvaloniaProperty.Register<CpuFallbackPreviewControl, ICpuFallbackRenderer?>(nameof(Renderer));

    public static readonly StyledProperty<bool> IsRunningProperty =
        AvaloniaProperty.Register<CpuFallbackPreviewControl, bool>(nameof(IsRunning));

    public static readonly StyledProperty<float[]> FloatUniformsProperty =
        AvaloniaProperty.Register<CpuFallbackPreviewControl, float[]>(nameof(FloatUniforms), []);

    public static readonly StyledProperty<SKColor[]> ColorUniformsProperty =
        AvaloniaProperty.Register<CpuFallbackPreviewControl, SKColor[]>(nameof(ColorUniforms), []);

    public static readonly StyledProperty<bool[]> BoolUniformsProperty =
        AvaloniaProperty.Register<CpuFallbackPreviewControl, bool[]>(nameof(BoolUniforms), []);

    public static readonly StyledProperty<int[]> IntUniformsProperty =
        AvaloniaProperty.Register<CpuFallbackPreviewControl, int[]>(nameof(IntUniforms), []);

    private readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();

    static CpuFallbackPreviewControl()
    {
        AffectsRender<CpuFallbackPreviewControl>(
            RendererProperty, IsRunningProperty,
            FloatUniformsProperty, ColorUniformsProperty,
            BoolUniformsProperty, IntUniformsProperty);
    }

    public ICpuFallbackRenderer? Renderer
    {
        get => GetValue(RendererProperty);
        set => SetValue(RendererProperty, value);
    }

    public bool IsRunning
    {
        get => GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    public float[] FloatUniforms
    {
        get => GetValue(FloatUniformsProperty);
        set => SetValue(FloatUniformsProperty, value);
    }

    public SKColor[] ColorUniforms
    {
        get => GetValue(ColorUniformsProperty);
        set => SetValue(ColorUniformsProperty, value);
    }

    public bool[] BoolUniforms
    {
        get => GetValue(BoolUniformsProperty);
        set => SetValue(BoolUniformsProperty, value);
    }

    public int[] IntUniforms
    {
        get => GetValue(IntUniformsProperty);
        set => SetValue(IntUniformsProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (!IsRunning || Renderer is not { } renderer)
            return;

        var bounds = new Rect(Bounds.Size);
        if (bounds.Width < 1 || bounds.Height < 1)
            return;

        var time = (float)clock.Elapsed.TotalSeconds;
        var width = (float)bounds.Width;
        var height = (float)bounds.Height;

        context.Custom(new CpuFallbackDrawOperation(
            new Rect(0, 0, width, height),
            renderer,
            width, height, time,
            FloatUniforms, ColorUniforms, BoolUniforms, IntUniforms));
    }

    private sealed class CpuFallbackDrawOperation(
        Rect bounds,
        ICpuFallbackRenderer renderer,
        float width, float height, float time,
        float[] floats, SKColor[] colors, bool[] bools, int[] ints)
        : ICustomDrawOperation
    {
        public Rect Bounds => bounds;

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature is null) return;

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            var rect = new SKRect(0, 0, width, height);

            try
            {
                renderer.Render(canvas, null, rect, width, height, time, floats, colors, bools, ints);
            }
            catch
            {
                // Swallow rendering errors to avoid crashing the UI
            }
        }

        public bool HitTest(Point p) => bounds.Contains(p);
        public bool Equals(ICustomDrawOperation? other) => false;
        public void Dispose() { }
    }
}
