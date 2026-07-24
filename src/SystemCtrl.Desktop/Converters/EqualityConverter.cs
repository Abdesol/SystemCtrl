using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SystemCtrl.Desktop.Converters;

public class EqualityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null && parameter == null) return true;
        if (value == null || parameter == null) return false;

        if (parameter is string paramString && value.GetType().IsEnum)
        {
            try
            {
                var parsedParam = Enum.Parse(value.GetType(), paramString, true);
                return value.Equals(parsedParam);
            }
            catch
            {
                return false;
            }
        }

        if (parameter is string s && value is IConvertible convertible)
        {
            try
            {
                var converted = System.Convert.ChangeType(s, value.GetType(), culture);
                return value.Equals(converted);
            }
            catch { }
        }

        return value.Equals(parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
