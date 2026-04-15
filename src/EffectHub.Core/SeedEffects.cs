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
                       uniform shader content;
                       uniform float spacing;
                       uniform float strength;

                       half4 main(float2 coord) {
                           half4 c = content.eval(coord);
                           float span = max(spacing * 40.0, 3.0);
                           float line = step(0.5, fract(coord.y / span));
                           half3 result = c.rgb * half(1.0 - line * clamp(strength * 1.5, 0.0, 1.0));
                           return half4(result, c.a);
                       }
                       """,
            AuthorAlias = "EffectHub",
            Tags = ["retro", "crt", "scanline"],
            Uniforms =
            [
                new UniformDefinition { Name = "spacing", Type = UniformType.Float, DefaultValue = 0.3, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "strength", Type = UniformType.Float, DefaultValue = 0.5, Min = 0.0, Max = 1.0 }
            ]
        },

        new Effect
        {
            Id = "seed-color-tint",
            Name = "Color Tint",
            Description = "Tints the visual with a configurable color and intensity.",
            SkslCode = """
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
                       """,
            AuthorAlias = "EffectHub",
            Tags = ["color", "tint", "overlay"],
            Uniforms =
            [
                new UniformDefinition { Name = "intensity", Type = UniformType.Float, DefaultValue = 0.5, Min = 0.0, Max = 1.0 },
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
                       uniform shader content;
                       uniform float cell;
                       uniform float strength;

                       half4 main(float2 coord) {
                           half4 c = content.eval(coord);
                           float span = max(cell * 60.0, 4.0);
                           float gx = fract(coord.x / span);
                           float gy = fract(coord.y / span);
                           float grid = (gx < 0.04 || gy < 0.04) ? 1.0 : 0.0;
                           half3 result = mix(c.rgb, half3(1.0, 1.0, 1.0) * c.a, half(grid * strength));
                           return half4(result, c.a);
                       }
                       """,
            AuthorAlias = "EffectHub",
            Tags = ["grid", "pattern", "overlay"],
            Uniforms =
            [
                new UniformDefinition { Name = "cell", Type = UniformType.Float, DefaultValue = 0.33, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "strength", Type = UniformType.Float, DefaultValue = 0.6, Min = 0.0, Max = 1.0 }
            ]
        },

        new Effect
        {
            Id = "seed-vignette",
            Name = "Vignette",
            Description = "Darkens the edges of the visual to create a vignette effect.",
            SkslCode = """
                       uniform shader content;
                       uniform float width;
                       uniform float height;
                       uniform float strength;
                       uniform float radius;

                       half4 main(float2 coord) {
                           half4 c = content.eval(coord);
                           float2 uv = float2(coord.x / max(width, 1.0), coord.y / max(height, 1.0));
                           float dist = distance(uv, float2(0.5, 0.5));
                           float vignette = smoothstep(radius * 0.6, max(radius * 0.6 - 0.3, 0.0), dist);
                           half3 result = c.rgb * half(mix(1.0 - strength, 1.0, vignette));
                           return half4(result, c.a);
                       }
                       """,
            AuthorAlias = "EffectHub",
            Tags = ["vignette", "cinematic", "darkening"],
            Uniforms =
            [
                new UniformDefinition { Name = "strength", Type = UniformType.Float, DefaultValue = 0.8, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "radius", Type = UniformType.Float, DefaultValue = 0.5, Min = 0.1, Max = 1.5 }
            ]
        },

        new Effect
        {
            Id = "seed-noise",
            Name = "Noise Grain",
            Description = "Adds film-like noise grain to the visual.",
            SkslCode = """
                       uniform shader content;
                       uniform float strength;
                       uniform float seed;

                       float hash(float2 p) {
                           float h = dot(p, float2(127.1, 311.7));
                           return fract(sin(h) * 43758.5453123);
                       }

                       half4 main(float2 coord) {
                           half4 c = content.eval(coord);
                           float n = hash(coord + float2(seed * 100.0, seed * 100.0)) * 2.0 - 1.0;
                           half3 noisy = c.rgb + half3(half(n * strength * 0.8));
                           return half4(clamp(noisy, half3(0.0), half3(c.a)), c.a);
                       }
                       """,
            AuthorAlias = "EffectHub",
            Tags = ["noise", "grain", "film", "texture"],
            Uniforms =
            [
                new UniformDefinition { Name = "strength", Type = UniformType.Float, DefaultValue = 0.5, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "seed", Type = UniformType.Float, DefaultValue = 0.42, Min = 0.0, Max = 1.0 }
            ]
        }
    ];
}
