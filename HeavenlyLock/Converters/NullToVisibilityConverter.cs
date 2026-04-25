using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HeavenlyLock.Converters;

/// <summary>Returns Visible when value is non-null, Collapsed when null.</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
