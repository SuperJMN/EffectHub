using Avalonia;
using Effector;

namespace EffectHub.Controls.EffectPreview;

[SkiaEffect(typeof(DynamicShaderEffectFactory))]
public sealed class DynamicShaderEffect : SkiaEffectBase
{
    public static readonly StyledProperty<string> SkslCodeProperty =
        AvaloniaProperty.Register<DynamicShaderEffect, string>(nameof(SkslCode), "half4 main(float2 coord) { return half4(0.0, 0.0, 0.0, 0.0); }");

    public static readonly StyledProperty<float> Uniform0Property =
        AvaloniaProperty.Register<DynamicShaderEffect, float>(nameof(Uniform0), 0.5f);

    public static readonly StyledProperty<float> Uniform1Property =
        AvaloniaProperty.Register<DynamicShaderEffect, float>(nameof(Uniform1), 0.5f);

    public static readonly StyledProperty<float> Uniform2Property =
        AvaloniaProperty.Register<DynamicShaderEffect, float>(nameof(Uniform2), 0.5f);

    public static readonly StyledProperty<float> Uniform3Property =
        AvaloniaProperty.Register<DynamicShaderEffect, float>(nameof(Uniform3), 0.5f);

    public static readonly StyledProperty<float> Uniform4Property =
        AvaloniaProperty.Register<DynamicShaderEffect, float>(nameof(Uniform4), 0.5f);

    static DynamicShaderEffect()
    {
        AffectsRender<DynamicShaderEffect>(
            SkslCodeProperty,
            Uniform0Property, Uniform1Property,
            Uniform2Property, Uniform3Property,
            Uniform4Property);
    }

    public string SkslCode
    {
        get => GetValue(SkslCodeProperty);
        set => SetValue(SkslCodeProperty, value);
    }

    public float Uniform0 { get => GetValue(Uniform0Property); set => SetValue(Uniform0Property, value); }
    public float Uniform1 { get => GetValue(Uniform1Property); set => SetValue(Uniform1Property, value); }
    public float Uniform2 { get => GetValue(Uniform2Property); set => SetValue(Uniform2Property, value); }
    public float Uniform3 { get => GetValue(Uniform3Property); set => SetValue(Uniform3Property, value); }
    public float Uniform4 { get => GetValue(Uniform4Property); set => SetValue(Uniform4Property, value); }
}
