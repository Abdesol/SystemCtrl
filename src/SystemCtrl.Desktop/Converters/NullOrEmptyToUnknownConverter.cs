using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SystemCtrl.Desktop.Converters;

public class NullOrEmptyToUnknownConverter : IValueConverter
{
    public string Placeholder { get; set; } = "Unknown";

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var str = value?.ToString();
        return string.IsNullOrWhiteSpace(str) ? Placeholder : str;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
