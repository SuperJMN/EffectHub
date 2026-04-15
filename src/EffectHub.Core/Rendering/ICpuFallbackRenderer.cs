using SkiaSharp;

namespace EffectHub.Core.Rendering;

/// <summary>
/// Contract for CPU fallback renderers compiled at runtime from user C# code.
/// Mirrors the DynamicShaderEffect uniform slot layout so the same values
/// flow to both GPU and CPU paths.
/// </summary>
public interface ICpuFallbackRenderer
{
    void Render(
        SKCanvas canvas,
        SKImage? contentImage,
        SKRect rect,
        float width,
        float height,
        float time,
        float[] floats,
        SKColor[] colors,
        bool[] bools,
        int[] ints);
}
