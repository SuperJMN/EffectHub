using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using EffectHub.Sections.Editor;

namespace EffectHub.Converters;

public static class SurfaceConverters
{
    public static readonly IValueConverter TestSurfaceToBrush = new TestSurfaceToBrushConverter();
}

public class TestSurfaceToBrushConverter : IValueConverter
{
    private static readonly IBrush WarmGradient = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.Parse("#FF6B35"), 0),
            new GradientStop(Color.Parse("#004E98"), 0.5),
            new GradientStop(Color.Parse("#1A936F"), 1),
        }
    };

    private static readonly IBrush CoolGradient = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.Parse("#00BCD4"), 0),
            new GradientStop(Color.Parse("#7B1FA2"), 0.5),
            new GradientStop(Color.Parse("#E91E63"), 1),
        }
    };

    private static readonly IBrush DarkSolid = new SolidColorBrush(Color.Parse("#1A1A2E"));
    private static readonly IBrush LightSolid = new SolidColorBrush(Color.Parse("#E0E0E0"));
    private static readonly IBrush WhiteSolid = new SolidColorBrush(Colors.White);
    private static readonly IBrush BlackSolid = new SolidColorBrush(Colors.Black);

    private static readonly IBrush Checkerboard = BuildCheckerboard(Color.Parse("#CCCCCC"), Colors.White);
    private static readonly IBrush TransparentCheckerboard = BuildCheckerboard(Color.Parse("#E8E8E8"), Color.Parse("#F8F8F8"));

    private static VisualBrush BuildCheckerboard(Color color1, Color color2)
    {
        var brush1 = new SolidColorBrush(color1);
        var brush2 = new SolidColorBrush(color2);

        var grid = new Avalonia.Controls.Canvas
        {
            Width = 20,
            Height = 20,
        };

        var topLeft = new Avalonia.Controls.Border { Width = 10, Height = 10, Background = brush1 };
        Avalonia.Controls.Canvas.SetLeft(topLeft, 0);
        Avalonia.Controls.Canvas.SetTop(topLeft, 0);

        var topRight = new Avalonia.Controls.Border { Width = 10, Height = 10, Background = brush2 };
        Avalonia.Controls.Canvas.SetLeft(topRight, 10);
        Avalonia.Controls.Canvas.SetTop(topRight, 0);

        var bottomLeft = new Avalonia.Controls.Border { Width = 10, Height = 10, Background = brush2 };
        Avalonia.Controls.Canvas.SetLeft(bottomLeft, 0);
        Avalonia.Controls.Canvas.SetTop(bottomLeft, 10);

        var bottomRight = new Avalonia.Controls.Border { Width = 10, Height = 10, Background = brush1 };
        Avalonia.Controls.Canvas.SetLeft(bottomRight, 10);
        Avalonia.Controls.Canvas.SetTop(bottomRight, 10);

        grid.Children.Add(topLeft);
        grid.Children.Add(topRight);
        grid.Children.Add(bottomLeft);
        grid.Children.Add(bottomRight);

        return new VisualBrush
        {
            Visual = grid,
            TileMode = TileMode.Tile,
            SourceRect = new RelativeRect(0, 0, 20, 20, RelativeUnit.Absolute),
            DestinationRect = new RelativeRect(0, 0, 20, 20, RelativeUnit.Absolute),
        };
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TestSurface surface)
            return WarmGradient;

        return surface switch
        {
            TestSurface.WarmGradient => WarmGradient,
            TestSurface.CoolGradient => CoolGradient,
            TestSurface.DarkSolid => DarkSolid,
            TestSurface.LightSolid => LightSolid,
            TestSurface.White => WhiteSolid,
            TestSurface.Black => BlackSolid,
            TestSurface.Checkerboard => Checkerboard,
            TestSurface.Transparent => TransparentCheckerboard,
            _ => WarmGradient,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
