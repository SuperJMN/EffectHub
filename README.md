# EffectHub

A shader gallery and editor built on [Effector](https://github.com/SuperJMN/Effector) for Avalonia 12. Write SkSL shaders, tweak uniforms in real-time, and browse a community gallery.

## Architecture

### Uniform Editing via PropertyGrid

Shader uniforms are editable at runtime through a **standard MVVM bridge** — no Avalonia property system specialization needed.

```
PropertyGrid (Zafiro.Avalonia)
   ↕ IPropertyItem (Name, PropertyType, Value)
UniformPropertyItem (ReactiveObject + IPropertyItem)
   ↕ Action callback
EditorView code-behind
   ↕ Sets typed StyledProperty slots
DynamicShaderEffect (AvaloniaObject)
```

**`UniformPropertyItem`** maps each SkSL uniform to a CLR type that `PropertyEditorSelector` understands:

| SkSL type | CLR type | Editor control |
|-----------|----------|---------------|
| `float` / `half` | `System.Double` | Slider + NumericUpDown |
| `float4` with "color"/"tint" in name | `Avalonia.Media.Color` | ColorPicker |
| `bool` | `System.Boolean` | CheckBox |
| `int` | `System.Int32` | NumericUpDown |

### DynamicShaderEffect Slot Layout

Pre-allocated typed slots avoid runtime type emission:

- **8 float** slots (`Float0`–`Float7`)
- **2 Color** slots (`Color0`–`Color1`)
- **2 bool** slots (`Bool0`–`Bool1`)
- **2 int** slots (`Int0`–`Int1`)

`DynamicShaderEffectFactory` maps detected uniforms to slots by type at render time.

### Pattern for Particle / Animated Effects

When creating effects that have both user-configurable parameters and internal animation state:

| Concern | Property type | Example | Editable in PropertyGrid? |
|---------|--------------|---------|--------------------------|
| User configuration | `StyledProperty` | `ParticleCount`, `Gravity`, `BaseColor` | ✅ Yes — exposed via `UniformPropertyItem` |
| Animation state | `DirectProperty` | `CurrentTime`, `ParticlePositions` | ❌ No — internal, changes every frame |

**Guidelines:**

1. **`StyledProperty`** for anything the user should tweak. These trigger `AffectsRender` and work with data binding, styles, and animations.

2. **`DirectProperty`** for rapidly-changing internal state (particle positions, elapsed time). These are read-only from the outside and updated by a timer.

3. **Timer pattern**: Use `DispatcherTimer` at ~16ms interval (~60fps) to update `DirectProperty` values. The existing Effector effects already follow this pattern.

4. **Separation**: Keep configuration properties in the effect class (editable via PropertyGrid). Keep animation state in a separate internal model or as `DirectProperty` fields that the timer updates.

```csharp
// User-configurable (shows in PropertyGrid)
public static readonly StyledProperty<int> ParticleCountProperty =
    AvaloniaProperty.Register<MyEffect, int>(nameof(ParticleCount), 100);

public static readonly StyledProperty<float> GravityProperty =
    AvaloniaProperty.Register<MyEffect, float>(nameof(Gravity), 9.8f);

// Internal animation state (hidden from PropertyGrid)
private static readonly DirectProperty<MyEffect, float> ElapsedTimeProperty =
    AvaloniaProperty.RegisterDirect<MyEffect, float>(
        nameof(ElapsedTime), o => o.ElapsedTime);

static MyEffect()
{
    AffectsRender<MyEffect>(ParticleCountProperty, GravityProperty);
}
```

## Dependencies

- [Effector](https://github.com/SuperJMN/Effector) — Skia effect rendering for Avalonia
- [Zafiro.Avalonia](https://github.com/SuperJMN/Zafiro.Avalonia) ≥ 51.6.3 — PropertyGrid with Color editor
- [Avalonia](https://avaloniaui.net/) 12

## Separated deployment (API + frontend on different machines)

Backend (`EffectHub.Api`) and frontends (`EffectHub.Browser` WASM, `EffectHub.Desktop`) are independently deployable. Each frontend stores the backend URL in per-platform settings and lets you change it from the **Settings** tab without recompiling.

### Backend

```bash
dotnet publish src/EffectHub.Api -c Release -o publish/api
```

Configure allowed origins in `appsettings.json`:

```json
{
  "Cors": {
    "AllowedOrigins": [ "https://effecthub.example.com" ]
  }
}
```

If `Cors:AllowedOrigins` is empty, the API falls back to `AllowAnyOrigin` (convenient for local dev — restrict it in production).

### Frontends

```bash
# WASM static site
dotnet publish src/EffectHub.Browser -c Release -o publish/web
# Desktop
dotnet publish src/EffectHub.Desktop -c Release -o publish/desktop
```

URL resolution at startup:

1. `EFFECTHUB_API_URL` env var (Desktop only).
2. `ApiBaseUrl` from persisted settings (`Zafiro.Settings.ISettings<EffectHubSettings>`):
   - **Desktop / Mobile** → `IsolatedStorageSettingsStore` (`System.IO.IsolatedStorage`).
   - **WASM** → `LocalStorageSettingsStore` (`window.localStorage` via `[JSImport]`).
3. Default `http://localhost:5120` (Desktop only). On WASM the URL must be configured.

Open the **Settings** tab in the app to change the API URL and **Test connection** at any time. Changes apply hot — no restart.

### Mixed-content / HTTPS

If the WASM site is served over `https://`, the browser blocks plain `http://` API calls (mixed content). Either:

- Serve the API over HTTPS (recommended in production), or
- Serve both the WASM site and the API over plain HTTP during local testing.
