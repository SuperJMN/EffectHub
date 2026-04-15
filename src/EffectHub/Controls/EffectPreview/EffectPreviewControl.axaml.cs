using Avalonia;
using Avalonia.Controls;

namespace EffectHub.Controls.EffectPreview;

public partial class EffectPreviewControl : UserControl
{
    public static readonly StyledProperty<string?> SkslCodeProperty =
        AvaloniaProperty.Register<EffectPreviewControl, string?>(nameof(SkslCode));

    public string? SkslCode
    {
        get => GetValue(SkslCodeProperty);
        set => SetValue(SkslCodeProperty, value);
    }

    public EffectPreviewControl()
    {
        InitializeComponent();
    }
}
