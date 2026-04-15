using System;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Media;
using Effector;
using SkiaSharp;

namespace EffectHub.Controls.EffectPreview;

public sealed partial class DynamicShaderEffectFactory :
    ISkiaEffectFactory<DynamicShaderEffect>,
    ISkiaShaderEffectFactory<DynamicShaderEffect>,
    ISkiaEffectValueFactory,
    ISkiaShaderEffectValueFactory
{
    // Value array layout: [SkslCode, Float0..7, Color0..1, Bool0..1, Int0..1]
    private const int SkslCodeIndex = 0;
    private const int Float0Index = 1;
    private const int Color0Index = 9;  // Float0Index + 8
    private const int Bool0Index = 11;  // Color0Index + 2
    private const int Int0Index = 13;   // Bool0Index + 2

    private static readonly HashSet<string> AutoInjectedUniforms = ["width", "height", "iResolution"];

    public Thickness GetPadding(DynamicShaderEffect effect) => default;
    public SKImageFilter? CreateFilter(DynamicShaderEffect effect, SkiaEffectContext context) => null;
    public Thickness GetPadding(object[] values) => default;
    public SKImageFilter? CreateFilter(object[] values, SkiaEffectContext context) => null;

    public SkiaShaderEffect? CreateShaderEffect(DynamicShaderEffect effect, SkiaShaderEffectContext context) =>
        CreateShaderEffect(
        [
            effect.SkslCode,
            effect.Float0, effect.Float1, effect.Float2, effect.Float3,
            effect.Float4, effect.Float5, effect.Float6, effect.Float7,
            effect.Color0, effect.Color1,
            effect.Bool0, effect.Bool1,
            effect.Int0, effect.Int1
        ], context);

    public SkiaShaderEffect? CreateShaderEffect(object[] values, SkiaShaderEffectContext context)
    {
        var skslCode = (string)values[SkslCodeIndex];
        if (string.IsNullOrWhiteSpace(skslCode)) return null;

        try
        {
            var parsedUniforms = ParseUniforms(skslCode);
            var userUniforms = parsedUniforms.Where(u => !AutoInjectedUniforms.Contains(u.Name)).ToList();
            var allNames = parsedUniforms.Select(u => u.Name).ToHashSet();
            var hasContent = skslCode.Contains("uniform shader content");

            int floatSlot = 0, colorSlot = 0, boolSlot = 0, intSlot = 0;
            var uniformBindings = new List<(string Name, Func<object> GetValue)>();

            foreach (var uniform in userUniforms)
            {
                switch (uniform.Type)
                {
                    case "float" or "half":
                        if (floatSlot < DynamicShaderEffect.MaxFloatUniforms)
                        {
                            var idx = Float0Index + floatSlot++;
                            uniformBindings.Add((uniform.Name, () => (float)values[idx]));
                        }
                        break;

                    case "float4" or "half4" or "vec4" when uniform.IsColor:
                        if (colorSlot < DynamicShaderEffect.MaxColorUniforms)
                        {
                            var idx = Color0Index + colorSlot++;
                            uniformBindings.Add((uniform.Name, () =>
                            {
                                var c = (Color)values[idx];
                                return new[] { c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f };
                            }));
                        }
                        break;

                    case "float2" or "half2" or "vec2":
                        // Map float2 as two consecutive float slots
                        if (floatSlot + 1 < DynamicShaderEffect.MaxFloatUniforms)
                        {
                            var idx1 = Float0Index + floatSlot++;
                            var idx2 = Float0Index + floatSlot++;
                            uniformBindings.Add((uniform.Name, () => new[] { (float)values[idx1], (float)values[idx2] }));
                        }
                        break;

                    case "float3" or "half3" or "vec3":
                        // Map float3 as three consecutive float slots
                        if (floatSlot + 2 < DynamicShaderEffect.MaxFloatUniforms)
                        {
                            var idx1 = Float0Index + floatSlot++;
                            var idx2 = Float0Index + floatSlot++;
                            var idx3 = Float0Index + floatSlot++;
                            uniformBindings.Add((uniform.Name, () => new[] { (float)values[idx1], (float)values[idx2], (float)values[idx3] }));
                        }
                        break;

                    case "float4" or "half4" or "vec4":
                        // Non-color float4: use four float slots
                        if (floatSlot + 3 < DynamicShaderEffect.MaxFloatUniforms)
                        {
                            var idx1 = Float0Index + floatSlot++;
                            var idx2 = Float0Index + floatSlot++;
                            var idx3 = Float0Index + floatSlot++;
                            var idx4 = Float0Index + floatSlot++;
                            uniformBindings.Add((uniform.Name, () => new[] { (float)values[idx1], (float)values[idx2], (float)values[idx3], (float)values[idx4] }));
                        }
                        break;

                    case "bool":
                        if (boolSlot < DynamicShaderEffect.MaxBoolUniforms)
                        {
                            var idx = Bool0Index + boolSlot++;
                            uniformBindings.Add((uniform.Name, () => (bool)values[idx] ? 1.0f : 0.0f));
                        }
                        break;

                    case "int":
                        if (intSlot < DynamicShaderEffect.MaxIntUniforms)
                        {
                            var idx = Int0Index + intSlot++;
                            uniformBindings.Add((uniform.Name, () => (float)(int)values[idx]));
                        }
                        break;
                }
            }

            return SkiaRuntimeShaderBuilder.Create(
                skslCode,
                context,
                contentChildName: hasContent ? "content" : null,
                configureUniforms: uniforms =>
                {
                    foreach (var binding in uniformBindings)
                    {
                        var val = binding.GetValue();
                        if (val is float[] arr)
                            uniforms.Add(binding.Name, arr);
                        else
                            uniforms.Add(binding.Name, (float)val);
                    }

                    if (allNames.Contains("width"))
                        uniforms.Add("width", context.EffectBounds.Width);
                    if (allNames.Contains("height"))
                        uniforms.Add("height", context.EffectBounds.Height);
                    if (allNames.Contains("iResolution"))
                        uniforms.Add("iResolution", new[] { context.EffectBounds.Width, context.EffectBounds.Height });
                },
                fallbackRenderer: (canvas, contentImage, destRect) =>
                {
                    try
                    {
                        var effect = SKRuntimeEffect.CreateShader(skslCode, out _);
                        if (effect is null)
                        {
                            canvas.DrawImage(contentImage, destRect);
                            return;
                        }

                        using (effect)
                        {
                            var skUniforms = new SKRuntimeEffectUniforms(effect);
                            foreach (var binding in uniformBindings)
                            {
                                var val = binding.GetValue();
                                if (val is float[] arr)
                                    skUniforms.Add(binding.Name, arr);
                                else
                                    skUniforms.Add(binding.Name, (float)val);
                            }

                            if (allNames.Contains("width"))
                                skUniforms.Add("width", destRect.Width);
                            if (allNames.Contains("height"))
                                skUniforms.Add("height", destRect.Height);
                            if (allNames.Contains("iResolution"))
                                skUniforms.Add("iResolution", new[] { destRect.Width, destRect.Height });

                            var children = new SKRuntimeEffectChildren(effect);
                            if (hasContent)
                            {
                                using var contentShader = contentImage.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
                                children.Add("content", contentShader);
                            }

                            using var shader = effect.ToShader(skUniforms, children);
                            if (shader is not null)
                            {
                                using var paint = new SKPaint { Shader = shader, BlendMode = SKBlendMode.SrcOver };
                                canvas.DrawRect(destRect, paint);
                            }
                            else
                            {
                                canvas.DrawImage(contentImage, destRect);
                            }
                        }
                    }
                    catch
                    {
                        canvas.DrawImage(contentImage, destRect);
                    }
                });
        }
        catch
        {
            return null;
        }
    }

    private static List<ParsedUniform> ParseUniforms(string sksl)
    {
        var result = new List<ParsedUniform>();
        foreach (Match match in UniformRegex().Matches(sksl))
        {
            var type = match.Groups["type"].Value;
            if (type is "shader" or "blender" or "colorFilter") continue;

            var name = match.Groups["name"].Value;
            var isColor = name.Contains("color", StringComparison.OrdinalIgnoreCase)
                          || name.Contains("tint", StringComparison.OrdinalIgnoreCase);
            result.Add(new ParsedUniform(name, type, isColor));
        }
        return result;
    }

    private record ParsedUniform(string Name, string Type, bool IsColor);

    [GeneratedRegex(@"uniform\s+(?<type>\w+)\s+(?<name>\w+)\s*;")]
    private static partial Regex UniformRegex();
}
