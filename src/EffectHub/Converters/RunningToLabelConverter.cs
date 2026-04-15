using System.Globalization;
using Avalonia.Data.Converters;

namespace EffectHub.Converters;

public static class RunningConverters
{
    public static readonly IValueConverter ToLabel = new RunningToLabelConverter();
}

public class RunningToLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "⏹ Stop" : "▶ Start";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
