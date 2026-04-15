using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace EffectHub.Behaviors;

public class TappedCommandBehavior : Behavior<Control>
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<TappedCommandBehavior, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<TappedCommandBehavior, object?>(nameof(CommandParameter));

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    protected override void OnAttachedToVisualTree()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.Tapped += OnTapped;
        }
    }

    protected override void OnDetachedFromVisualTree()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.Tapped -= OnTapped;
        }
    }

    private void OnTapped(object? sender, RoutedEventArgs e)
    {
        var command = Command;
        var parameter = CommandParameter;

        if (command is not null && command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }
    }
}
