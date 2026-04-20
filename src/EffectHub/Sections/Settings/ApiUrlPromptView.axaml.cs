using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace EffectHub.Sections.Settings;

public partial class ApiUrlPromptView : UserControl
{
    public ApiUrlPromptView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
