using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace EffectHub.Controls.EffectPreview;

public partial class EffectPreviewControl : UserControl
{
    public static readonly StyledProperty<string?> SkslCodeProperty =
        AvaloniaProperty.Register<EffectPreviewControl, string?>(nameof(SkslCode));

    public static readonly StyledProperty<IReadOnlyDictionary<string, double>?> UniformDefaultsProperty =
        AvaloniaProperty.Register<EffectPreviewControl, IReadOnlyDictionary<string, double>?>(nameof(UniformDefaults));

    public string? SkslCode
    {
        get => GetValue(SkslCodeProperty);
        set => SetValue(SkslCodeProperty, value);
    }

    public IReadOnlyDictionary<string, double>? UniformDefaults
    {
        get => GetValue(UniformDefaultsProperty);
        set => SetValue(UniformDefaultsProperty, value);
    }

    public EffectPreviewControl()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var canvas = this.FindControl<SkiaShaderCanvas>("PART_ShaderCanvas");
        if (canvas is null) return;

        canvas.Bind(SkiaShaderCanvas.SkslCodeProperty,
            new Binding(nameof(SkslCode)) { Source = this });
        canvas.Bind(SkiaShaderCanvas.UniformDefaultsProperty,
            new Binding(nameof(UniformDefaults)) { Source = this });
    }
}
