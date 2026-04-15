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
        },

        // ── Imported from PokemonBattleEngine.Gui ────────────────

        new Effect
        {
            Id = "pokemon-flame-billboard",
            Name = "Flame Billboard",
            Description = "Per-particle flame shader. Distorts content upward into a flickering flame tongue with multi-octave turbulence.",
            SkslCode = """
                       uniform shader content;
                       uniform float width;
                       uniform float height;
                       uniform float progress;
                       uniform float intensity;
                       uniform float seed;
                       uniform float flameHeight;

                       half4 main(float2 coord) {
                           float safeWidth = max(width, 1.0);
                           float safeHeight = max(height, 1.0);
                           float2 uv = float2(coord.x / safeWidth, coord.y / safeHeight);
                           float t = clamp(progress, 0.0, 1.0);

                           float growth = smoothstep(0.0, 0.18, t);
                           float fade = 1.0 - smoothstep(0.76, 1.0, t);
                           float env = growth * fade * intensity;
                           if (env <= 0.001) return half4(0);

                           float centeredX = uv.x - 0.5;
                           float upward = clamp(1.0 - uv.y, 0.0, 1.0);
                           float phase = seed * 2.7 + t * 10.5;

                           float waveA = sin(centeredX * 14.0 + phase * 4.3 - uv.y * 10.0);
                           float waveB = sin(centeredX * 27.0 - phase * 6.1 + uv.y * 13.0);
                           float waveC = sin(centeredX * 36.0 + phase * 7.2);
                           float swirl = ((waveA * 0.5) + (waveB * 0.35) + (waveC * 0.2)) * 0.5 + 0.5;

                           float plumeWidth = mix(0.12, 0.32, upward) * (1.0 - t * 0.15);
                           float horizontalDrift = ((waveA * 0.020) + (waveB * 0.015)) * upward;
                           float plume = max(0.0, 1.0 - abs(centeredX - horizontalDrift) / max(plumeWidth, 0.02));

                           float2 displaced = coord;
                           displaced.x += ((waveA + waveB) * 0.5) * (4.0 + upward * 4.0) * env;
                           displaced.y += env * (5.0 + 18.0 * upward);

                           half4 base = content.eval(displaced);
                           float contentMask = base.a;

                           float flame = env * plume * swirl * contentMask;
                           float heightMaskT = clamp(upward / max(flameHeight, 0.001), 0.0, 1.0);
                           float heightMask = heightMaskT * heightMaskT * (3.0 - 2.0 * heightMaskT);
                           flame *= heightMask;

                           float glow = smoothstep(0.0, 1.0, flame) * (0.35 + upward * 0.35);

                           half3 ember = half3(1.0, 0.36, 0.08);
                           half3 core = half3(1.0, 0.95, 0.60);
                           float hotCore = clamp((flame * 1.85) - (upward * 0.6), 0.0, 1.0);
                           half3 flameColor = mix(ember, core, half(hotCore));

                           float alpha = clamp(flame * 0.95 + glow * 0.25, 0.0, 1.0);
                           half3 rgb = flameColor * half(alpha);

                           return half4(clamp(rgb, 0.0, 1.0), alpha);
                       }
                       """,
            AuthorAlias = "PokemonBattleEngine",
            Tags = ["fire", "flame", "particle", "combat"],
            Uniforms =
            [
                new UniformDefinition { Name = "progress", Type = UniformType.Float, DefaultValue = 0.5, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "intensity", Type = UniformType.Float, DefaultValue = 1.0, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "seed", Type = UniformType.Float, DefaultValue = 1.0, Min = 0.0, Max = 10.0 },
                new UniformDefinition { Name = "flameHeight", Type = UniformType.Float, DefaultValue = 0.85, Min = 0.35, Max = 1.25 }
            ]
        },

        new Effect
        {
            Id = "pokemon-flame-stream",
            Name = "Flame Stream",
            Description = "Turbulent fire stream rendered from origin to destination. Multi-octave FBM noise with white-hot core to red edges.",
            SkslCode = """
                       uniform float width;
                       uniform float height;
                       uniform float originX;
                       uniform float originY;
                       uniform float destX;
                       uniform float destY;
                       uniform float progress;
                       uniform float intensity;

                       float hash21(float2 p) {
                           float3 p3 = fract(float3(p.x, p.y, p.x) * float3(0.1031, 0.1030, 0.0973));
                           p3 += dot(p3, p3.yzx + 33.33);
                           return fract((p3.x + p3.y) * p3.z);
                       }

                       float vnoise(float2 p) {
                           float2 i = floor(p);
                           float2 f = fract(p);
                           f = f * f * (3.0 - 2.0 * f);
                           float a = hash21(i);
                           float b = hash21(i + float2(1.0, 0.0));
                           float c = hash21(i + float2(0.0, 1.0));
                           float d = hash21(i + float2(1.0, 1.0));
                           return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
                       }

                       float fbm(float2 p) {
                           float v = 0.0;
                           float a = 0.5;
                           for (int i = 0; i < 6; i++) {
                               v += a * vnoise(p);
                               p = float2(p.x * 1.6 + p.y * 1.2, -p.x * 1.2 + p.y * 1.6);
                               a *= 0.5;
                           }
                           return v;
                       }

                       half4 main(float2 coord) {
                           float widthRatio = 0.10;
                           float2 o = float2(originX * width, originY * height);
                           float2 dst = float2(destX * width, destY * height);
                           float2 beam = dst - o;
                           float beamLen = length(beam);
                           if (beamLen < 1.0) return half4(0);

                           float2 dir = beam / beamLen;
                           float2 perp = float2(-dir.y, dir.x);

                           float2 delta = coord - o;
                           float along = dot(delta, dir);
                           float across = dot(delta, perp);

                           float u = along / beamLen;
                           float v = across / beamLen;

                           float reach = min(progress / 0.18, 1.0);
                           float fadeOut = 1.0 - max((progress - 0.65) / 0.35, 0.0);
                           float env = reach * fadeOut * intensity;
                           if (env < 0.01) return half4(0);

                           float maxW = widthRatio * 2.0;
                           if (u < -0.05 || u > reach + 0.05 || abs(v) > maxW) return half4(0);

                           float halfW = widthRatio * (1.0 - 0.45 * u);
                           float time = progress * 12.0;

                           float2 nc1 = float2(u * 5.0 - time, v * 18.0);
                           float turb1 = fbm(nc1);
                           float2 nc2 = float2(u * 7.0 - time * 1.4 + 77.0, v * 14.0 + 33.0);
                           float turb2 = fbm(nc2);
                           float2 nc3 = float2(u * 4.0 - time * 0.8 + 200.0, v * 10.0);
                           float edgeTurb = fbm(nc3);
                           float turbulentV = v + (edgeTurb - 0.5) * widthRatio * 0.6;

                           float dist = abs(turbulentV);
                           float outer = smoothstep(halfW * 1.6, halfW * 0.25, dist);
                           float inner = smoothstep(halfW * 0.9, halfW * 0.08, dist);
                           float core  = smoothstep(halfW * 0.35, 0.0, dist);

                           float flicker = 0.65 + 0.35 * turb1;
                           float flicker2 = 0.8 + 0.2 * turb2;
                           outer *= flicker * flicker2;

                           float startTaper = smoothstep(-0.03, 0.06, u);
                           float endTaper = smoothstep(reach + 0.03, reach - 0.08, u);
                           float taper = startTaper * endTaper;
                           outer *= taper;
                           inner *= taper;
                           core *= taper;

                           half3 redOrange = half3(0.95, 0.18, 0.0);
                           half3 orange    = half3(1.0, 0.5, 0.0);
                           half3 yellow    = half3(1.0, 0.85, 0.25);
                           half3 white     = half3(1.0, 0.97, 0.85);

                           half3 col = redOrange * half(outer);
                           col = mix(col, orange, half(min(inner, 1.0)));
                           col = mix(col, yellow, half(inner * inner));
                           col = mix(col, white,  half(core));
                           col *= half(1.0 + core * 0.5);

                           float alpha = min(outer * env, 1.0);
                           return half4(col * half(alpha), half(alpha));
                       }
                       """,
            AuthorAlias = "PokemonBattleEngine",
            Tags = ["fire", "flame", "stream", "beam", "combat"],
            Uniforms =
            [
                new UniformDefinition { Name = "originX", Type = UniformType.Float, DefaultValue = 0.1, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "originY", Type = UniformType.Float, DefaultValue = 0.5, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "destX", Type = UniformType.Float, DefaultValue = 0.9, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "destY", Type = UniformType.Float, DefaultValue = 0.5, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "progress", Type = UniformType.Float, DefaultValue = 0.5, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "intensity", Type = UniformType.Float, DefaultValue = 1.0, Min = 0.0, Max = 1.0 }
            ]
        },

        new Effect
        {
            Id = "pokemon-hail",
            Name = "Hailstorm",
            Description = "Cinematic hailstorm with cold blue atmosphere, drifting snow dust, sparkling ice crystals, tumbling hailstones, and frost vignette.",
            SkslCode = """
                       uniform float width;
                       uniform float height;
                       uniform float time;
                       uniform float intensity;

                       float hash(float n) { return fract(sin(n) * 43758.5453); }

                       half4 main(float2 coord) {
                           float w = max(width, 1.0);
                           float h = max(height, 1.0);
                           float nx = coord.x / w;
                           float ny = coord.y / h;
                           float aspect = w / h;

                           float4 acc = float4(0.0);

                           float hdx = (nx - 0.6) * aspect;
                           float hdy = ny - 0.3;
                           float hd = hdx * hdx + hdy * hdy;
                           float washA = exp(-hd / 1.62) * 0.05 * intensity;
                           float3 washC = float3(0.78, 0.86, 1.0);
                           acc = float4(washC * washA, washA);

                           const int SNOW_N = 20;
                           for (int i = 0; i < SNOW_N; i++) {
                               float fi = float(i);
                               float seed = fi * 1.618034 + 0.1;
                               float speed = 0.3 + 0.4 * fract(seed * 47.13);
                               float sz = 1.0 + 2.5 * fract(seed * 23.71);
                               float xPhase = mod(time * speed * 30.0 + seed * w * 3.0, w * 1.3);
                               float yPhase = mod(time * speed * 50.0 + seed * h * 2.0, h * 1.3);
                               float px = w * 1.15 - xPhase + sin(time * 1.5 + seed * 3.0) * 12.0;
                               float py = -h * 0.15 + yPhase;
                               float d = length(coord - float2(px, py));
                               float pA = smoothstep(sz, 0.0, d)
                                        * (0.2 + 0.8 * fract(seed * 11.3))
                                        * 0.10 * intensity;
                               if (pA > 0.002) {
                                   float3 pC = float3(0.86, 0.92, 1.0);
                                   acc = float4(pC * pA, pA) + acc * (1.0 - pA);
                               }
                           }

                           const int CRYSTAL_N = 12;
                           for (int i = 0; i < CRYSTAL_N; i++) {
                               float fi = float(i);
                               float seed = fi * 2.236 + 0.5;
                               float speed = 0.2 + 0.3 * fract(seed * 37.91);
                               float sz = 2.5 + 4.0 * fract(seed * 19.37);
                               float xPhase = mod(time * speed * 40.0 + seed * w * 2.5, w * 1.4);
                               float yPhase = mod(time * speed * 65.0 + seed * h * 2.0, h * 1.4);
                               float px = w * 1.2 - xPhase + sin(time * 2.0 + seed * 4.1) * 8.0;
                               float py = -h * 0.2 + yPhase;
                               float sparkle = 0.5 + 0.5 * sin(time * 4.0 + seed * 7.3);
                               float rotation = time * (1.0 + seed * 0.8) + seed * 45.0;
                               float rad = rotation * 3.14159265 / 180.0;
                               float cosR = cos(rad);
                               float sinR = sin(rad);
                               float2 delta = coord - float2(px, py);
                               float2 rotD = float2(
                                   delta.x * cosR + delta.y * sinR,
                                   -delta.x * sinR + delta.y * cosR);
                               float diam = (abs(rotD.x) + abs(rotD.y)) / sz;
                               float pA = smoothstep(1.0, 0.3, diam)
                                        * (0.3 + 0.7 * sparkle)
                                        * 0.15 * intensity;
                               if (pA > 0.002) {
                                   float3 pC = float3(0.94, 0.97, 1.0);
                                   acc = float4(pC * pA, pA) + acc * (1.0 - pA);
                               }
                           }

                           const int HAIL_N = 18;
                           for (int i = 0; i < HAIL_N; i++) {
                               float fi = float(i);
                               float seed = fi * 1.414 + 0.3;
                               float speed = 0.6 + 0.8 * fract(seed * 59.17);
                               float sz = 3.0 + 6.0 * fract(seed * 31.73);
                               float xDrift = 0.15 * speed;
                               float xPhase = mod(time * speed * xDrift * 120.0 + seed * w * 2.0, w * 1.5);
                               float yPhase = mod(time * speed * 200.0 + seed * h * 3.0, h * 1.4);
                               float px = w * 1.0 - xPhase;
                               float py = -h * 0.2 + yPhase;
                               float aspectR = 0.6 + 0.4 * fract(seed * 13.7);
                               float rotation = time * (3.0 + seed * 2.0) * 57.3;
                               float rad = rotation * 3.14159265 / 180.0;
                               float cosR = cos(rad);
                               float sinR = sin(rad);
                               float2 delta = coord - float2(px, py);
                               float2 rotD = float2(
                                   delta.x * cosR + delta.y * sinR,
                                   -delta.x * sinR + delta.y * cosR);
                               float d = length(float2(rotD.x / sz, rotD.y / (sz * aspectR)));
                               float pA = smoothstep(1.0, 0.0, d)
                                        * (0.4 + 0.6 * fract(seed * 17.9))
                                        * 0.25 * intensity;
                               if (pA > 0.002) {
                                   float highlight = smoothstep(0.6, 0.0, d) * 0.3;
                                   float3 pC = float3(
                                       0.82 + highlight,
                                       0.90 + highlight * 0.5,
                                       1.0);
                                   pC = min(pC, float3(1.0));
                                   acc = float4(pC * pA, pA) + acc * (1.0 - pA);
                               }
                           }

                           float vigA = 0.03 * intensity;
                           float topFrost = smoothstep(0.12, 0.0, ny) * vigA;
                           if (topFrost > 0.002) {
                               float3 fC = float3(0.78, 0.88, 1.0);
                               acc = float4(fC * topFrost, topFrost) + acc * (1.0 - topFrost);
                           }
                           float botFrost = smoothstep(0.12, 0.0, 1.0 - ny) * vigA;
                           if (botFrost > 0.002) {
                               float3 fC = float3(0.78, 0.88, 1.0);
                               acc = float4(fC * botFrost, botFrost) + acc * (1.0 - botFrost);
                           }

                           float outA = acc.a;
                           if (outA < 0.002) return half4(0.0);
                           float3 outC = acc.rgb / outA;
                           return half4(half3(outC), half(outA));
                       }
                       """,
            AuthorAlias = "PokemonBattleEngine",
            Tags = ["weather", "hail", "ice", "snow", "particles"],
            Uniforms =
            [
                new UniformDefinition { Name = "time", Type = UniformType.Float, DefaultValue = 0.0, Min = 0.0, Max = 100.0 },
                new UniformDefinition { Name = "intensity", Type = UniformType.Float, DefaultValue = 1.0, Min = 0.0, Max = 1.0 }
            ]
        },

        new Effect
        {
            Id = "pokemon-heal-glare",
            Name = "Heal Glare",
            Description = "Animated star-shaped glints with staggered timing for a sparkling healing cascade. Uses additive blending.",
            SkslCode = """
                       uniform float width;
                       uniform float height;
                       uniform float progress;
                       uniform float intensity;
                       uniform float seed;
                       uniform float glareCount;
                       uniform float raySize;
                       uniform float red;

                       float hash(float a, float b) {
                           return fract(sin(a * 12.9898 + b * 78.233) * 43758.5453);
                       }

                       half4 main(float2 coord) {
                           float w = max(width, 1.0);
                           float h = max(height, 1.0);
                           float nx = coord.x / w;
                           float ny = coord.y / h;
                           float aspect = w / h;

                           float green = 1.0 - red;
                           float blue = 0.8;

                           float totalGlow = 0.0;
                           int count = int(clamp(glareCount, 1.0, 12.0));

                           for (int i = 0; i < count; i++) {
                               float fi = float(i);
                               float px = mix(0.15, 0.85, hash(seed + fi * 1.17, fi * 0.73));
                               float py = mix(0.15, 0.85, hash(seed + fi * 0.53, fi * 1.91));
                               float stagger = fi / max(float(count) - 1.0, 1.0);
                               float windowHalf = 0.22;
                               float center = mix(windowHalf, 1.0 - windowHalf, stagger);
                               float localT = clamp((1.0 - abs(progress - center) / windowHalf), 0.0, 1.0);
                               float fade = localT * localT;

                               float dx = (nx - px) * aspect;
                               float dy = ny - py;
                               float dist = sqrt(dx * dx + dy * dy);

                               float rot = hash(seed * 3.7 + fi * 2.31, fi * 0.47) * 3.14159;
                               float cosR = cos(rot);
                               float sinR = sin(rot);
                               float rx = dx * cosR - dy * sinR;
                               float ry = dx * sinR + dy * cosR;

                               float angle = atan(ry, rx);
                               float spike = pow(abs(cos(angle * 2.0)), 8.0);

                               float coreRadius = raySize * 0.18;
                               float spikeReach = raySize * (0.4 + spike * 3.5);
                               float core = exp(-dist * dist / max(coreRadius * coreRadius, 0.0001));
                               float ray  = exp(-dist / max(spikeReach, 0.001));

                               totalGlow += (ray * 0.4 + core * 0.6) * fade * intensity;
                           }

                           totalGlow = min(totalGlow, 1.0);
                           half3 glare = half3(red * totalGlow, green * totalGlow, blue * totalGlow);
                           return half4(glare, totalGlow);
                       }
                       """,
            AuthorAlias = "PokemonBattleEngine",
            Tags = ["heal", "glare", "sparkle", "magic", "combat"],
            Uniforms =
            [
                new UniformDefinition { Name = "progress", Type = UniformType.Float, DefaultValue = 0.5, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "intensity", Type = UniformType.Float, DefaultValue = 0.95, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "seed", Type = UniformType.Float, DefaultValue = 1.0, Min = 0.0, Max = 10.0 },
                new UniformDefinition { Name = "glareCount", Type = UniformType.Float, DefaultValue = 10.0, Min = 1.0, Max = 12.0 },
                new UniformDefinition { Name = "raySize", Type = UniformType.Float, DefaultValue = 0.14, Min = 0.01, Max = 0.5 },
                new UniformDefinition { Name = "red", Type = UniformType.Float, DefaultValue = 0.67, Min = 0.0, Max = 1.0 }
            ]
        },

        new Effect
        {
            Id = "pokemon-rain",
            Name = "Rainstorm",
            Description = "Dense diagonal rain with dark blue-grey atmosphere, two drop layers, cyclic splashes, and mist vignette.",
            SkslCode = """
                       uniform float width;
                       uniform float height;
                       uniform float time;
                       uniform float intensity;

                       float hash(float n) { return fract(sin(n) * 43758.5453); }
                       float hash2(float2 p) { return fract(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

                       float sdSegment(float2 p, float2 a, float2 b) {
                           float2 pa = p - a;
                           float2 ba = b - a;
                           float t = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
                           return length(pa - ba * t);
                       }

                       float rainLayer(float2 coord, float w, float h,
                                       float colCount, float rowCount,
                                       float fallSpeed, float driftSpeed,
                                       float dropLen, float dropLenVar,
                                       float thickness, float thicknessVar,
                                       float rainAngle, float t) {
                           float cellW = w / colCount;
                           float cellH = h / rowCount;
                           float movingY = coord.y - t * fallSpeed;
                           float movingX = coord.x + t * driftSpeed;
                           float cx = floor(movingX / cellW);
                           float cy = floor(movingY / cellH);
                           float totalA = 0.0;
                           for (int dy = -1; dy <= 1; dy++) {
                               for (int dx = -1; dx <= 1; dx++) {
                                   float2 cell = float2(cx + float(dx), cy + float(dy));
                                   float seed = hash2(cell);
                                   float dpx = (cell.x + 0.1 + seed * 0.8) * cellW - t * driftSpeed;
                                   float dpy = (cell.y + 0.1 + hash(seed * 43.0) * 0.8) * cellH + t * fallSpeed;
                                   float dLen = dropLen + dropLenVar * hash(seed * 23.7);
                                   float dThick = thickness + thicknessVar * hash(seed * 7.3);
                                   float2 start = float2(dpx, dpy);
                                   float2 end = float2(dpx - dLen * rainAngle, dpy + dLen);
                                   float d = sdSegment(coord, start, end);
                                   float a = smoothstep(dThick, 0.0, d)
                                           * (0.2 + 0.8 * hash(seed * 11.3));
                                   totalA += a;
                               }
                           }
                           return min(totalA, 1.0);
                       }

                       half4 main(float2 coord) {
                           float w = max(width, 1.0);
                           float h = max(height, 1.0);
                           float nx = coord.x / w;
                           float ny = coord.y / h;
                           float aspect = w / h;

                           float4 acc = float4(0.0);

                           float hdx = (nx - 0.5) * aspect;
                           float hdy = ny - 0.4;
                           float hd = hdx * hdx + hdy * hdy;
                           float washA = exp(-hd / 1.62) * 0.35 * intensity;
                           float3 washC = float3(0.24, 0.29, 0.39);
                           acc = float4(washC * washA, washA);

                           float lightA = rainLayer(coord, w, h,
                               15.0, 10.0, 280.0, 60.0,
                               14.0, 22.0, 1.2, 0.8,
                               0.25, time)
                               * 0.5 * intensity;
                           if (lightA > 0.002) {
                               float3 lightC = float3(0.63, 0.75, 0.86);
                               acc = float4(lightC * lightA, lightA) + acc * (1.0 - lightA);
                           }

                           float heavyA = rainLayer(coord, w, h,
                               10.0, 8.0, 350.0, 45.0,
                               25.0, 40.0, 2.0, 2.5,
                               0.22, time + 17.3)
                               * 0.78 * intensity;
                           if (heavyA > 0.002) {
                               float3 heavyC = float3(0.55, 0.69, 0.82);
                               acc = float4(heavyC * heavyA, heavyA) + acc * (1.0 - heavyA);
                           }

                           const int SPLASH_N = 15;
                           for (int i = 0; i < SPLASH_N; i++) {
                               float fi = float(i);
                               float seed = fi * 1.414 + 0.5;
                               float speed = 0.5 + 0.5 * fract(seed * 41.97);
                               float xBase = fract(seed * 137.508) * w;
                               float yBase = h * (0.85 + 0.1 * fract(seed * 29.41));
                               float cycle = fract(time * speed * 2.5 + seed * 7.0);
                               if (cycle < 0.4) {
                                   float progress = cycle / 0.4;
                                   float sz = 5.0 + 8.0 * progress;
                                   float fade = 1.0 - progress;
                                   float2 delta = coord - float2(xBase, yBase);
                                   float d = length(float2(delta.x / sz, delta.y / (sz * 0.3)));
                                   float sA = smoothstep(1.0, 0.5, d)
                                            * fade * 0.39 * intensity;
                                   if (sA > 0.002) {
                                       float3 sC = float3(0.71, 0.80, 0.92);
                                       acc = float4(sC * sA, sA) + acc * (1.0 - sA);
                                   }
                               }
                           }

                           float vigA = 0.18 * intensity;
                           float botMist = smoothstep(0.18, 0.0, 1.0 - ny) * vigA;
                           if (botMist > 0.002) {
                               float3 mC = float3(0.47, 0.57, 0.69);
                               acc = float4(mC * botMist, botMist) + acc * (1.0 - botMist);
                           }
                           float topDark = smoothstep(0.15, 0.0, ny) * vigA * 0.67;
                           if (topDark > 0.002) {
                               float3 dC = float3(0.20, 0.24, 0.31);
                               acc = float4(dC * topDark, topDark) + acc * (1.0 - topDark);
                           }

                           float outA = acc.a;
                           if (outA < 0.002) return half4(0.0);
                           float3 outC = acc.rgb / outA;
                           return half4(half3(outC), half(outA));
                       }
                       """,
            AuthorAlias = "PokemonBattleEngine",
            Tags = ["weather", "rain", "storm", "water", "particles"],
            Uniforms =
            [
                new UniformDefinition { Name = "time", Type = UniformType.Float, DefaultValue = 0.0, Min = 0.0, Max = 100.0 },
                new UniformDefinition { Name = "intensity", Type = UniformType.Float, DefaultValue = 1.0, Min = 0.0, Max = 1.0 }
            ]
        },

        new Effect
        {
            Id = "pokemon-sandstorm",
            Name = "Sandstorm",
            Description = "Warm amber atmospheric haze with streaming sand particles, tumbling clumps, horizontal wind lines, and diagonal dust streaks.",
            SkslCode = """
                       uniform float width;
                       uniform float height;
                       uniform float time;
                       uniform float intensity;

                       float hash(float n) { return fract(sin(n) * 43758.5453); }

                       float sdSegment(float2 p, float2 a, float2 b) {
                           float2 pa = p - a;
                           float2 ba = b - a;
                           float t = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
                           return length(pa - ba * t);
                       }

                       half4 main(float2 coord) {
                           float w = max(width, 1.0);
                           float h = max(height, 1.0);
                           float nx = coord.x / w;
                           float ny = coord.y / h;
                           float aspect = w / h;

                           float4 acc = float4(0.0);

                           float hdx = (nx - 0.3) * aspect;
                           float hdy = ny - 0.4;
                           float hd = hdx * hdx + hdy * hdy;
                           float hazeA = exp(-hd / 1.28) * 0.06 * intensity;
                           float3 hazeC = float3(0.82, 0.71, 0.43);
                           acc = float4(hazeC * hazeA, hazeA);

                           const int SMALL_N = 30;
                           for (int i = 0; i < SMALL_N; i++) {
                               float fi = float(i);
                               float seed = fi * 1.618034 + 0.3;
                               float yBase = fract(seed * 137.508) * h;
                               float speed = 0.7 + 0.6 * fract(seed * 73.13);
                               float sz = 1.5 + 3.0 * fract(seed * 31.71);
                               float xPhase = mod(time * speed * 180.0 + seed * w * 3.0, w * 1.4);
                               float px = w * 1.2 - xPhase;
                               float py = yBase + sin(time * 2.5 + seed * 5.0) * 8.0;
                               float d = length(coord - float2(px, py));
                               float pA = smoothstep(sz, 0.0, d)
                                        * (0.3 + 0.7 * fract(seed * 19.1))
                                        * 0.18 * intensity;
                               if (pA > 0.002) {
                                   float3 pC = float3(
                                       0.71 + 0.16 * fract(seed * 11.3),
                                       0.57 + 0.14 * fract(seed * 7.7),
                                       0.27 + 0.16 * fract(seed * 3.3));
                                   acc = float4(pC * pA, pA) + acc * (1.0 - pA);
                               }
                           }

                           const int LARGE_N = 8;
                           for (int i = 0; i < LARGE_N; i++) {
                               float fi = float(i);
                               float seed = fi * 2.236 + 0.7;
                               float yBase = fract(seed * 83.29) * h;
                               float speed = 0.4 + 0.5 * fract(seed * 41.97);
                               float sz = 5.0 + 8.0 * fract(seed * 17.39);
                               float xPhase = mod(time * speed * 120.0 + seed * w * 2.0, w * 1.6);
                               float px = w * 1.3 - xPhase;
                               float py = yBase + sin(time * 1.8 + seed * 3.7) * 15.0;
                               float2 delta = coord - float2(px, py);
                               float d = length(float2(delta.x / sz, delta.y / (sz * 0.5)));
                               float pA = smoothstep(1.0, 0.0, d)
                                        * (0.4 + 0.6 * fract(seed * 23.7))
                                        * 0.12 * intensity;
                               if (pA > 0.002) {
                                   float3 pC = float3(0.76, 0.63, 0.35);
                                   acc = float4(pC * pA, pA) + acc * (1.0 - pA);
                               }
                           }

                           const int WIND_N = 12;
                           for (int i = 0; i < WIND_N; i++) {
                               float fi = float(i);
                               float seed = fi * 1.414 + 0.5;
                               float yBase = fract(seed * 97.31) * h;
                               float speed = 0.9 + 0.5 * fract(seed * 53.87);
                               float len = 30.0 + 60.0 * fract(seed * 29.41);
                               float xPhase = mod(time * speed * 220.0 + seed * w * 2.5, w * 1.5);
                               float lx = w * 1.25 - xPhase;
                               float ly = yBase + sin(time * 3.0 + seed * 4.0) * 5.0;
                               float angle = -0.08 + 0.04 * sin(time + seed);
                               float2 a = float2(lx, ly);
                               float2 b = float2(lx - len, ly + len * angle);
                               float d = sdSegment(coord, a, b);
                               float thickness = 1.0 + 1.5 * fract(seed * 7.3);
                               float lA = smoothstep(thickness, 0.0, d)
                                        * (0.3 + 0.7 * fract(seed * 13.9))
                                        * 0.05 * intensity;
                               if (lA > 0.002) {
                                   float3 lC = float3(0.82, 0.73, 0.47);
                                   acc = float4(lC * lA, lA) + acc * (1.0 - lA);
                               }
                           }

                           const int STREAK_N = 6;
                           for (int i = 0; i < STREAK_N; i++) {
                               float fi = float(i);
                               float yBase = h * (0.1 + 0.8 * fi / float(STREAK_N));
                               float shimmer = 0.7 + 0.3 * sin(time * 1.5 + fi * 2.1);
                               float2 a = float2(w * 1.1,
                                   yBase + sin(-0.15 + 0.05 * sin(time * 0.8 + fi)) * h * 0.1);
                               float2 b = float2(-w * 0.1,
                                   yBase - h * 0.08 + cos(time * 0.5 + fi) * 10.0);
                               float d = sdSegment(coord, a, b);
                               float thickness = 3.0 + 5.0 * shimmer;
                               float sA = smoothstep(thickness, 0.0, d)
                                        * shimmer * 0.03 * intensity;
                               if (sA > 0.002) {
                                   float3 sC = float3(0.78, 0.67, 0.39);
                                   acc = float4(sC * sA, sA) + acc * (1.0 - sA);
                               }
                           }

                           float outA = acc.a;
                           if (outA < 0.002) return half4(0.0);
                           float3 outC = acc.rgb / outA;
                           return half4(half3(outC), half(outA));
                       }
                       """,
            AuthorAlias = "PokemonBattleEngine",
            Tags = ["weather", "sandstorm", "sand", "wind", "particles"],
            Uniforms =
            [
                new UniformDefinition { Name = "time", Type = UniformType.Float, DefaultValue = 0.0, Min = 0.0, Max = 100.0 },
                new UniformDefinition { Name = "intensity", Type = UniformType.Float, DefaultValue = 1.0, Min = 0.0, Max = 1.0 }
            ]
        },

        new Effect
        {
            Id = "pokemon-starfield",
            Name = "Starfield Parallax",
            Description = "Deep twinkling starfield with 8-layer parallax scrolling, cross-shaped flares, and color variation.",
            SkslCode = """
                       uniform float width;
                       uniform float height;
                       uniform float time;
                       uniform float intensity;
                       uniform float speed;

                       float Hash21(float2 p) {
                           p = fract(p * float2(123.23, 456.34));
                           p += dot(p, p + 45.45);
                           return fract(p.x * p.y);
                       }

                       float Star(float2 uv, float flare) {
                           float d = length(uv);
                           float m = 0.02 / d;
                           float rays = max(0.0, 1.0 - abs(uv.x * uv.y * 10000.0));
                           m += rays * flare;
                           float2 ruv = float2(
                               uv.x * 0.70711 - uv.y * 0.70711,
                               uv.x * 0.70711 + uv.y * 0.70711);
                           rays = max(0.0, 1.0 - abs(ruv.x * ruv.y * 10000.0));
                           m += rays * 0.3 * flare;
                           m *= smoothstep(1.0, 0.2, d);
                           return m;
                       }

                       float3 StarLayer(float2 uv, float t) {
                           float3 col = float3(0.0);
                           float2 gv = fract(uv) - 0.5;
                           float2 id = floor(uv);
                           for (int y = -1; y <= 1; y++) {
                               for (int x = -1; x <= 1; x++) {
                                   float2 offset = float2(float(x), float(y));
                                   float n = Hash21(id + offset);
                                   float sz = fract(n * 534.0);
                                   float star = Star(
                                       gv - offset - float2(n - 0.5, fract(n * 165.0) - 0.5),
                                       smoothstep(0.9, 0.1, sz));
                                   float3 colors = sin(
                                       float3(0.2, 0.3, 0.9) * fract(n * 2434.0) * 123.0
                                   ) * 0.5 + 0.5;
                                   colors *= float3(1.0, 0.5, 1.0 + sz);
                                   star *= sin(t * 3.0 + n * 18.3) * 0.5 + 1.0;
                                   col += star * sz * colors;
                               }
                           }
                           return col;
                       }

                       half4 main(float2 coord) {
                           float w = max(width, 1.0);
                           float h = max(height, 1.0);
                           float2 uv = (coord - 0.5 * float2(w, h)) / h;
                           float t = time * speed;

                           float ct = cos(t);
                           float st = sin(t);
                           uv = float2(uv.x * ct - uv.y * st, uv.x * st + uv.y * ct);

                           float3 col = float3(0.0);
                           for (int li = 0; li < 8; li++) {
                               float i = float(li) / 8.0;
                               float depth = fract(i + t);
                               float scale = mix(20.0, 0.5, depth);
                               float fade = depth * smoothstep(1.0, 0.9, depth);
                               col += StarLayer(uv * scale + i * 343.0, time) * fade;
                           }

                           col *= intensity;
                           float a = clamp(max(col.x, max(col.y, col.z)), 0.0, 1.0);
                           if (a < 0.002) return half4(0.0);
                           col = min(col, float3(1.0));
                           return half4(half3(col), half(a));
                       }
                       """,
            AuthorAlias = "PokemonBattleEngine",
            Tags = ["space", "stars", "parallax", "night", "cosmic"],
            Uniforms =
            [
                new UniformDefinition { Name = "time", Type = UniformType.Float, DefaultValue = 0.0, Min = 0.0, Max = 100.0 },
                new UniformDefinition { Name = "intensity", Type = UniformType.Float, DefaultValue = 1.0, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "speed", Type = UniformType.Float, DefaultValue = 0.05, Min = 0.0, Max = 0.5 }
            ]
        },

        new Effect
        {
            Id = "pokemon-sun-glare",
            Name = "Sun Lens Glare",
            Description = "Multi-layer radial bloom, animated god-rays, lens flare artifacts, and warm atmospheric wash. Uses additive blending.",
            SkslCode = """
                       uniform float width;
                       uniform float height;
                       uniform float time;
                       uniform float intensity;

                       const float PI = 3.14159265;
                       const float SUN_X = 0.20;
                       const float SUN_Y = 0.14;
                       const int RAY_COUNT = 14;
                       const int FLARE_COUNT = 6;

                       half4 main(float2 coord) {
                           float w = max(width, 1.0);
                           float h = max(height, 1.0);
                           float nx = coord.x / w;
                           float ny = coord.y / h;
                           float aspect = w / h;

                           float dx = (nx - SUN_X) * aspect;
                           float dy = ny - SUN_Y;
                           float dist = sqrt(dx * dx + dy * dy);
                           float shimmer = 1.0 + 0.12 * sin(time * 2.5);

                           float outerR = 0.55 * shimmer;
                           float outerB = exp(-dist * dist / (outerR * outerR * 0.5)) * 0.35;
                           float midR = 0.22 * shimmer;
                           float midB = exp(-dist * dist / (midR * midR * 0.5)) * 0.65;
                           float coreR = 0.06 * shimmer;
                           float coreB = exp(-dist * dist / (coreR * coreR * 0.5)) * 1.0;
                           float bloom = outerB + midB + coreB;

                           float angle = atan(dy, dx);
                           float rayTotal = 0.0;
                           float rayDecay = exp(-dist * 1.1);
                           for (int i = 0; i < RAY_COUNT; i++) {
                               float fi = float(i);
                               float rayAngle = fi * PI * 2.0 / float(RAY_COUNT) + time * 0.18;
                               float diff = mod(angle - rayAngle + PI, PI * 2.0) - PI;
                               float lenVar = 0.7 + 0.3 * sin(time * 1.8 + fi * 1.7);
                               float widVar = 0.6 + 0.4 * sin(time * 2.2 + fi * 2.3);
                               float rayW = 0.038 * widVar;
                               rayTotal += exp(-diff * diff / (rayW * rayW)) * rayDecay * lenVar * 0.30;
                           }

                           float flareTotal = 0.0;
                           float fDirX = 0.30;
                           float fDirY = 0.36;
                           for (int j = 0; j < FLARE_COUNT; j++) {
                               float fj = float(j);
                               float ft = 0.35 + fj * 0.19;
                               float fsz = 0.035 + 0.020 * sin(fj * 2.1 + 0.5);
                               float fam = 0.50 + 0.20 * cos(fj * 1.3);
                               float fShim = 1.0 + 0.18 * sin(time * 3.0 + fj * 1.9);
                               float fcx = SUN_X + fDirX * ft;
                               float fcy = SUN_Y + fDirY * ft;
                               float fdx = (nx - fcx) * aspect;
                               float fdy = ny - fcy;
                               float fdist = sqrt(fdx * fdx + fdy * fdy);
                               float fr = fsz * fShim;
                               flareTotal += exp(-fdist * fdist / (fr * fr * 0.5)) * fam;
                           }

                           float wash = 0.18;
                           float total = (bloom + rayTotal + flareTotal + wash) * intensity;
                           total = min(total, 1.0);

                           half3 col = half3(total, total * 0.90, total * 0.63);
                           return half4(col, total);
                       }
                       """,
            AuthorAlias = "PokemonBattleEngine",
            Tags = ["weather", "sun", "glare", "lens", "bloom", "light"],
            Uniforms =
            [
                new UniformDefinition { Name = "time", Type = UniformType.Float, DefaultValue = 0.0, Min = 0.0, Max = 100.0 },
                new UniformDefinition { Name = "intensity", Type = UniformType.Float, DefaultValue = 1.0, Min = 0.0, Max = 1.0 }
            ]
        },

        new Effect
        {
            Id = "pokemon-vortex-orb",
            Name = "Vortex Orb",
            Description = "Swirling dark energy sphere with procedural FBM noise, polar-coordinate twist, breathing pulsation, and configurable tint color.",
            SkslCode = """
                       uniform float width;
                       uniform float height;
                       uniform float progress;
                       uniform float intensity;
                       uniform float twistStrength;
                       uniform float tintR;
                       uniform float tintG;

                       float hash21(float2 p) {
                           float3 p3 = fract(float3(p.x, p.y, p.x) * float3(0.1031, 0.1030, 0.0973));
                           p3 += dot(p3, p3.yzx + 33.33);
                           return fract((p3.x + p3.y) * p3.z);
                       }

                       float vnoise(float2 p) {
                           float2 i = floor(p);
                           float2 f = fract(p);
                           f = f * f * (3.0 - 2.0 * f);
                           float a = hash21(i);
                           float b = hash21(i + float2(1.0, 0.0));
                           float c = hash21(i + float2(0.0, 1.0));
                           float d = hash21(i + float2(1.0, 1.0));
                           return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
                       }

                       float fbm(float2 p) {
                           float v = 0.0;
                           float a = 0.5;
                           for (int i = 0; i < 5; i++) {
                               v += a * vnoise(p);
                               p = float2(p.x * 1.6 + p.y * 1.2, -p.x * 1.2 + p.y * 1.6);
                               a *= 0.5;
                           }
                           return v;
                       }

                       half4 main(float2 coord) {
                           float tintB = 1.0 - tintR;
                           float seed = 1.0;

                           float2 center = float2(width * 0.5, height * 0.5);
                           float radius = min(width, height) * 0.3;
                           float2 delta = coord - center;
                           float dist = length(delta);

                           if (dist > radius * 1.6) return half4(0);
                           if (intensity < 0.01) return half4(0);

                           float normDist = dist / max(radius, 1.0);
                           float angle = atan(delta.y, delta.x);
                           float time = progress * 10.0 + seed * 3.7;

                           float twistFactor = smoothstep(1.2, 0.0, normDist);
                           float pulsation = sin(time * 0.7);
                           float twistAngle = twistFactor * twistStrength * pulsation;

                           float rotation = time * 0.4;
                           float rotatedAngle = angle + twistAngle + rotation;

                           float breath = 1.0 + 0.06 * sin(time * 1.3);
                           float effectiveDist = normDist / breath;

                           float2 twisted = float2(cos(rotatedAngle), sin(rotatedAngle)) * effectiveDist;

                           float n1 = fbm(twisted * 4.5 + float2(time * 0.25, time * 0.15));
                           float n2 = fbm(twisted * 8.0 - float2(time * 0.35, -time * 0.2) + float2(77.0, 33.0));
                           float n3 = fbm(twisted * 2.5 + float2(-time * 0.1, time * 0.3) + float2(200.0, 150.0));

                           float pattern = n1 * 0.45 + n2 * 0.35 + n3 * 0.20;
                           float spiralBand = sin(rotatedAngle * 3.0 + effectiveDist * 12.0 - time * 2.0) * 0.5 + 0.5;
                           pattern = mix(pattern, spiralBand, 0.3 * twistFactor);

                           float edgeMask = smoothstep(1.1, 0.65, effectiveDist);
                           float core = smoothstep(0.55, 0.0, effectiveDist);
                           core *= core;
                           float ring = smoothstep(0.3, 0.6, effectiveDist) * smoothstep(1.0, 0.7, effectiveDist);
                           float outerGlow = smoothstep(1.5, 0.9, effectiveDist) * 0.25;

                           half3 darkBase = half3(half(tintR * 0.1), half(tintG * 0.1), half(tintB * 0.1));
                           half3 midColor = half3(half(tintR * 0.6), half(tintG * 0.6), half(tintB * 0.6));
                           half3 brightColor = half3(half(tintR), half(tintG), half(tintB));
                           half3 coreColor = half3(
                               half(min(tintR + 0.5, 1.0)),
                               half(min(tintG + 0.5, 1.0)),
                               half(min(tintB + 0.5, 1.0)));

                           half3 col = darkBase;
                           col = mix(col, midColor, half(pattern * edgeMask));
                           col = mix(col, brightColor, half(ring * pattern * 0.8));
                           col = mix(col, coreColor, half(core * 0.7));
                           col *= half(1.0 + core * 0.6);

                           float alpha = max(edgeMask * pattern * 0.85 + core * 0.5, outerGlow);
                           alpha *= intensity;
                           alpha = min(alpha, 1.0);

                           return half4(col * half(alpha), half(alpha));
                       }
                       """,
            AuthorAlias = "PokemonBattleEngine",
            Tags = ["energy", "vortex", "orb", "magic", "dark", "combat"],
            Uniforms =
            [
                new UniformDefinition { Name = "progress", Type = UniformType.Float, DefaultValue = 0.5, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "intensity", Type = UniformType.Float, DefaultValue = 1.0, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "twistStrength", Type = UniformType.Float, DefaultValue = 8.0, Min = 0.0, Max = 30.0 },
                new UniformDefinition { Name = "tintR", Type = UniformType.Float, DefaultValue = 0.4, Min = 0.0, Max = 1.0 },
                new UniformDefinition { Name = "tintG", Type = UniformType.Float, DefaultValue = 0.2, Min = 0.0, Max = 1.0 }
            ]
        }
    ];
}
