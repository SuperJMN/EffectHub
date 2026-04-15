using System;
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

    private static readonly HashSet<string> AutoInjectedUniforms = ["width", "height", "iResolution"];

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
            var allNames = ParseUniformNames(skslCode);
            var userUniforms = allNames.Where(n => !AutoInjectedUniforms.Contains(n)).ToList();
            var hasContent = skslCode.Contains("uniform shader content");

            return SkiaRuntimeShaderBuilder.Create(
                skslCode,
                context,
                contentChildName: hasContent ? "content" : null,
                configureUniforms: uniforms =>
                {
                    for (var i = 0; i < Math.Min(userUniforms.Count, uniformValues.Length); i++)
                        uniforms.Add(userUniforms[i], uniformValues[i]);

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
                            var uniforms = new SKRuntimeEffectUniforms(effect);
                            for (var i = 0; i < Math.Min(userUniforms.Count, uniformValues.Length); i++)
                                uniforms.Add(userUniforms[i], uniformValues[i]);

                            if (allNames.Contains("width"))
                                uniforms.Add("width", destRect.Width);
                            if (allNames.Contains("height"))
                                uniforms.Add("height", destRect.Height);
                            if (allNames.Contains("iResolution"))
                                uniforms.Add("iResolution", new[] { destRect.Width, destRect.Height });

                            var children = new SKRuntimeEffectChildren(effect);
                            if (hasContent)
                            {
                                using var contentShader = contentImage.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
                                children.Add("content", contentShader);

                                using var shader = effect.ToShader(uniforms, children);
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
                            else
                            {
                                using var shader = effect.ToShader(uniforms, children);
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
