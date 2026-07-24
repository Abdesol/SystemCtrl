using System;
using System.Collections;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SystemCtrl.Desktop.Converters;

public class CollectionEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ICollection col) return col.Count == 0;
        if (value is Array arr) return arr.Length == 0;
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class CollectionNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ICollection col) return col.Count > 0;
        if (value is Array arr) return arr.Length > 0;
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
