using System.Globalization;
using Avalonia.Data.Converters;

namespace XemuHddImageEditor.Converters;

public class GreaterThanZeroToBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var iValue = (int)value!;
        return iValue > 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}