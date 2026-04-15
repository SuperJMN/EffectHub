using System.Text.RegularExpressions;
using Avalonia;
using Effector;
using SkiaSharp;

namespace EffectHub.Controls.EffectPreview;

public sealed partial class DynamicShaderEffectFactory :
    ISkiaEffectFactory<DynamicShaderEffect>,
    ISkiaShaderEffectFactory<DynamicShaderEffect>,
    ISkiaEffectValueFactory,
    ISkiaShaderEffectValueFactory
{
    private const int SkslCodeIndex = 0;
    private const int Uniform0Index = 1;
    private const int Uniform1Index = 2;
    private const int Uniform2Index = 3;
    private const int Uniform3Index = 4;
    private const int Uniform4Index = 5;

    public Thickness GetPadding(DynamicShaderEffect effect) => default;
    public SKImageFilter? CreateFilter(DynamicShaderEffect effect, SkiaEffectContext context) => null;
    public Thickness GetPadding(object[] values) => default;
    public SKImageFilter? CreateFilter(object[] values, SkiaEffectContext context) => null;

    public SkiaShaderEffect? CreateShaderEffect(DynamicShaderEffect effect, SkiaShaderEffectContext context) =>
        CreateShaderEffect([effect.SkslCode, effect.Uniform0, effect.Uniform1, effect.Uniform2, effect.Uniform3, effect.Uniform4], context);

    public SkiaShaderEffect? CreateShaderEffect(object[] values, SkiaShaderEffectContext context)
    {
        var skslCode = (string)values[SkslCodeIndex];
        if (string.IsNullOrWhiteSpace(skslCode)) return null;

        var uniformValues = new[]
        {
            (float)values[Uniform0Index],
            (float)values[Uniform1Index],
            (float)values[Uniform2Index],
            (float)values[Uniform3Index],
            (float)values[Uniform4Index]
        };

        try
        {
            return SkiaRuntimeShaderBuilder.Create(
                skslCode,
                context,
                uniforms =>
                {
                    var names = ParseUniformNames(skslCode);
                    for (var i = 0; i < Math.Min(names.Count, uniformValues.Length); i++)
                    {
                        uniforms.Add(names[i], uniformValues[i]);
                    }

                    // Auto-inject width/height if present
                    if (names.Contains("width"))
                        uniforms.Add("width", context.EffectBounds.Width);
                    if (names.Contains("height"))
                        uniforms.Add("height", context.EffectBounds.Height);
                });
        }
        catch
        {
            return null;
        }
    }

    private static List<string> ParseUniformNames(string sksl)
    {
        var names = new List<string>();
        foreach (Match match in UniformRegex().Matches(sksl))
        {
            var type = match.Groups["type"].Value;
            if (type is "shader" or "blender" or "colorFilter") continue;
            names.Add(match.Groups["name"].Value);
        }
        return names;
    }

    [GeneratedRegex(@"uniform\s+(?<type>\w+)\s+(?<name>\w+)\s*;")]
    private static partial Regex UniformRegex();
}
