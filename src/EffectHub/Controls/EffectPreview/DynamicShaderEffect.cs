using Avalonia;
using Avalonia.Media;
using Effector;

namespace EffectHub.Controls.EffectPreview;

[SkiaEffect(typeof(DynamicShaderEffectFactory))]
public sealed class DynamicShaderEffect : SkiaEffectBase
{
    public static readonly StyledProperty<string> SkslCodeProperty =
        AvaloniaProperty.Register<DynamicShaderEffect, string>(nameof(SkslCode), "half4 main(float2 coord) { return half4(0.0, 0.0, 0.0, 0.0); }");

    // Float uniform slots (8)
    public static readonly StyledProperty<float> Float0Property = AvaloniaProperty.Register<DynamicShaderEffect, float>(nameof(Float0), 0.5f);
    public static readonly StyledProperty<float> Float1Property = AvaloniaProperty.Register<DynamicShaderEffect, float>(nameof(Float1), 0.5f);
    public static readonly StyledProperty<float> Float2Property = AvaloniaProperty.Register<DynamicShaderEffect, float>(nameof(Float2), 0.5f);
    public static readonly StyledProperty<float> Float3Property = AvaloniaProperty.Register<DynamicShaderEffect, float>(nameof(Float3), 0.5f);
    public static readonly StyledProperty<float> Float4Property = AvaloniaProperty.Register<DynamicShaderEffect, float>(nameof(Float4), 0.5f);
    public static readonly StyledProperty<float> Float5Property = AvaloniaProperty.Register<DynamicShaderEffect, float>(nameof(Float5), 0.5f);
    public static readonly StyledProperty<float> Float6Property = AvaloniaProperty.Register<DynamicShaderEffect, float>(nameof(Float6), 0.5f);
    public static readonly StyledProperty<float> Float7Property = AvaloniaProperty.Register<DynamicShaderEffect, float>(nameof(Float7), 0.5f);

    // Color uniform slots (2)
    public static readonly StyledProperty<Color> Color0Property = AvaloniaProperty.Register<DynamicShaderEffect, Color>(nameof(Color0), Colors.White);
    public static readonly StyledProperty<Color> Color1Property = AvaloniaProperty.Register<DynamicShaderEffect, Color>(nameof(Color1), Colors.Black);

    // Bool uniform slots (2)
    public static readonly StyledProperty<bool> Bool0Property = AvaloniaProperty.Register<DynamicShaderEffect, bool>(nameof(Bool0));
    public static readonly StyledProperty<bool> Bool1Property = AvaloniaProperty.Register<DynamicShaderEffect, bool>(nameof(Bool1));

    // Int uniform slots (2)
    public static readonly StyledProperty<int> Int0Property = AvaloniaProperty.Register<DynamicShaderEffect, int>(nameof(Int0));
    public static readonly StyledProperty<int> Int1Property = AvaloniaProperty.Register<DynamicShaderEffect, int>(nameof(Int1));

    static DynamicShaderEffect()
    {
        AffectsRender<DynamicShaderEffect>(
            SkslCodeProperty,
            Float0Property, Float1Property, Float2Property, Float3Property,
            Float4Property, Float5Property, Float6Property, Float7Property,
            Color0Property, Color1Property,
            Bool0Property, Bool1Property,
            Int0Property, Int1Property);
    }

    public string SkslCode { get => GetValue(SkslCodeProperty); set => SetValue(SkslCodeProperty, value); }

    public float Float0 { get => GetValue(Float0Property); set => SetValue(Float0Property, value); }
    public float Float1 { get => GetValue(Float1Property); set => SetValue(Float1Property, value); }
    public float Float2 { get => GetValue(Float2Property); set => SetValue(Float2Property, value); }
    public float Float3 { get => GetValue(Float3Property); set => SetValue(Float3Property, value); }
    public float Float4 { get => GetValue(Float4Property); set => SetValue(Float4Property, value); }
    public float Float5 { get => GetValue(Float5Property); set => SetValue(Float5Property, value); }
    public float Float6 { get => GetValue(Float6Property); set => SetValue(Float6Property, value); }
    public float Float7 { get => GetValue(Float7Property); set => SetValue(Float7Property, value); }

    public Color Color0 { get => GetValue(Color0Property); set => SetValue(Color0Property, value); }
    public Color Color1 { get => GetValue(Color1Property); set => SetValue(Color1Property, value); }

    public bool Bool0 { get => GetValue(Bool0Property); set => SetValue(Bool0Property, value); }
    public bool Bool1 { get => GetValue(Bool1Property); set => SetValue(Bool1Property, value); }

    public int Int0 { get => GetValue(Int0Property); set => SetValue(Int0Property, value); }
    public int Int1 { get => GetValue(Int1Property); set => SetValue(Int1Property, value); }

    public const int MaxFloatUniforms = 8;
    public const int MaxColorUniforms = 2;
    public const int MaxBoolUniforms = 2;
    public const int MaxIntUniforms = 2;
}
