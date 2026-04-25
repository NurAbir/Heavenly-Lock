using System.Globalization;
using System.Windows.Data;

namespace HeavenlyLock.Converters;

public class StatusToButtonTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string? status = value as string;
        if (status?.Contains("Create") == true || status?.Contains("create") == true)
            return "Create Vault";
        return "Unlock Vault";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
