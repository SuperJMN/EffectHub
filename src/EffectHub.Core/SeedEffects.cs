using EffectHub.Core.Models;

namespace EffectHub.Core;

public static class SeedEffects
{
    public static IReadOnlyList<Effect> GetAll() =>
    [
        new Effect
        {
            Id = "seed-scanline",
            Name = "Scanline Overlay",
            Description = "Classic CRT scanline effect with configurable spacing and strength.",
            SkslCode = """
                       uniform float spacing;
                       uniform float strength;

                       half4 main(float2 coord) {
                           float span = max(spacing, 1.0);
                           float local = fract(coord.y / span);
                           float alpha = local >= 0.5 ? strength : 0.0;
                           return half4(0.0, 0.0, 0.0, alpha);
                       }
                       """,
            AuthorAlias = "EffectHub",
            Tags = ["retro", "crt", "scanline"],
            Uniforms =
            [
                new UniformDefinition { Name = "spacing", Type = UniformType.Float, DefaultValue = 4.0, Min = 2.0, Max = 32.0 },
                new UniformDefinition { Name = "strength", Type = UniformType.Float, DefaultValue = 0.3, Min = 0.0, Max = 1.0 }
            ]
        },

        new Effect
        {
            Id = "seed-color-tint",
            Name = "Color Tint",
            Description = "Tints the visual with a configurable color and intensity.",
            SkslCode = """
                       uniform float intensity;
                       uniform float red;
                       uniform float green;
                       uniform float blue;

                       half4 main(float2 coord) {
                           half premul = half(intensity);
                           return half4(half(red) * premul, half(green) * premul, half(blue) * premul, premul);
                       }
                       """,
            AuthorAlias = "EffectHub",
            Tags = ["color", "tint", "overlay"],
            Uniforms =
            [
                new UniformDefinition { Name = "intensity", Type = UniformType.Float, DefaultValue = 0.3, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "red", Type = UniformType.Float, DefaultValue = 0.0, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "green", Type = UniformType.Float, DefaultValue = 0.7, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "blue", Type = UniformType.Float, DefaultValue = 1.0, Min = 0.0, Max = 1.0 }
            ]
        },

        new Effect
        {
            Id = "seed-grid",
            Name = "Grid Overlay",
            Description = "Draws a pixel grid overlay with configurable cell size and color.",
            SkslCode = """
                       uniform float cell;
                       uniform float strength;
                       uniform float red;
                       uniform float green;
                       uniform float blue;

                       half4 main(float2 coord) {
                           float span = max(cell, 1.0);
                           float gx = fract(coord.x / span);
                           float gy = fract(coord.y / span);
                           float alpha = (gx < 0.06 || gy < 0.06) ? strength : 0.0;
                           half premulAlpha = half(alpha);
                           return half4(half(red) * premulAlpha, half(green) * premulAlpha, half(blue) * premulAlpha, premulAlpha);
                       }
                       """,
            AuthorAlias = "EffectHub",
            Tags = ["grid", "pattern", "overlay"],
            Uniforms =
            [
                new UniformDefinition { Name = "cell", Type = UniformType.Float, DefaultValue = 20.0, Min = 4.0, Max = 64.0 },
                new UniformDefinition { Name = "strength", Type = UniformType.Float, DefaultValue = 0.5, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "red", Type = UniformType.Float, DefaultValue = 1.0, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "green", Type = UniformType.Float, DefaultValue = 1.0, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "blue", Type = UniformType.Float, DefaultValue = 1.0, Min = 0.0, Max = 1.0 }
            ]
        },

        new Effect
        {
            Id = "seed-vignette",
            Name = "Vignette",
            Description = "Darkens the edges of the visual to create a vignette effect.",
            SkslCode = """
                       uniform float width;
                       uniform float height;
                       uniform float strength;
                       uniform float radius;

                       half4 main(float2 coord) {
                           float2 uv = float2(coord.x / max(width, 1.0), coord.y / max(height, 1.0));
                           float2 center = float2(0.5, 0.5);
                           float dist = distance(uv, center);
                           float vignette = smoothstep(radius, radius - 0.25, dist);
                           float alpha = (1.0 - vignette) * strength;
                           return half4(0.0, 0.0, 0.0, half(alpha));
                       }
                       """,
            AuthorAlias = "EffectHub",
            Tags = ["vignette", "cinematic", "darkening"],
            Uniforms =
            [
                new UniformDefinition { Name = "width", Type = UniformType.Float, DefaultValue = 400, Min = 1, Max = 4096 },
                new UniformDefinition { Name = "height", Type = UniformType.Float, DefaultValue = 300, Min = 1, Max = 4096 },
                new UniformDefinition { Name = "strength", Type = UniformType.Float, DefaultValue = 0.8, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "radius", Type = UniformType.Float, DefaultValue = 0.75, Min = 0.1, Max = 1.5 }
            ]
        },

        new Effect
        {
            Id = "seed-noise",
            Name = "Noise Grain",
            Description = "Adds film-like noise grain to the visual.",
            SkslCode = """
                       uniform float strength;
                       uniform float seed;

                       float hash(float2 p) {
                           float h = dot(p, float2(127.1, 311.7));
                           return fract(sin(h) * 43758.5453123);
                       }

                       half4 main(float2 coord) {
                           float n = hash(coord + float2(seed, seed)) * 2.0 - 1.0;
                           float alpha = abs(n) * strength;
                           half val = n > 0.0 ? half(alpha) : half(0.0);
                           return half4(val, val, val, half(alpha));
                       }
                       """,
            AuthorAlias = "EffectHub",
            Tags = ["noise", "grain", "film", "texture"],
            Uniforms =
            [
                new UniformDefinition { Name = "strength", Type = UniformType.Float, DefaultValue = 0.15, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "seed", Type = UniformType.Float, DefaultValue = 0.0, Min = 0.0, Max = 1000.0 }
            ]
        }
    ];
}
