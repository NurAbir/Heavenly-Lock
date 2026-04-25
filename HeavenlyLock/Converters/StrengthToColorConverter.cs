using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HeavenlyLock.Converters;

public class StrengthToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string? strength = value as string;
        return strength switch
        {
            "Weak" => new SolidColorBrush(Colors.Red),
            "Fair" => new SolidColorBrush(Colors.Orange),
            "Good" => new SolidColorBrush(Colors.Yellow),
            "Strong" => new SolidColorBrush(Colors.LightGreen),
            "Very Strong" => new SolidColorBrush(Colors.Green),
            _ => new SolidColorBrush(Colors.Gray)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
