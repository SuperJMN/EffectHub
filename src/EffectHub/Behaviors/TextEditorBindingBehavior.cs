using Avalonia;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;

namespace EffectHub.Behaviors;

/// <summary>
/// Two-way binds a TextEditor's Text property to an AvaloniaProperty, since
/// AvaloniaEdit's TextEditor.Text is a CLR property (not an AvaloniaProperty)
/// and cannot be bound directly in XAML.
/// </summary>
public class TextEditorBindingBehavior : Behavior<TextEditor>
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<TextEditorBindingBehavior, string?>(nameof(Text));

    private bool updating;

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    protected override void OnAttachedToVisualTree()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.TextChanged += OnEditorTextChanged;
            if (Text is not null && AssociatedObject.Text != Text)
            {
                AssociatedObject.Text = Text;
            }
        }
    }

    protected override void OnDetachedFromVisualTree()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.TextChanged -= OnEditorTextChanged;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty && !updating && AssociatedObject is not null)
        {
            var newText = change.GetNewValue<string?>() ?? "";
            if (AssociatedObject.Text != newText)
            {
                updating = true;
                AssociatedObject.Text = newText;
                updating = false;
            }
        }
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (updating || AssociatedObject is null) return;

        updating = true;
        Text = AssociatedObject.Text;
        updating = false;
    }
}
