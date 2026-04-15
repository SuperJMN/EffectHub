using System.Globalization;
using Avalonia.Data.Converters;

namespace EffectHub;

public static class IndexConverters
{
    public static readonly IValueConverter IsZero = new IndexEqualConverter(0);
    public static readonly IValueConverter IsOne = new IndexEqualConverter(1);
    public static readonly IValueConverter IsTwo = new IndexEqualConverter(2);

    private class IndexEqualConverter(int target) : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is int index && index == target;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
