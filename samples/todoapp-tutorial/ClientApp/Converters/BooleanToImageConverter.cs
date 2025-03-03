using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ClientApp.Converters;

/// <summary>
/// A converter to convert the boolean for IsComplete into one of two images.
/// </summary>
public class BooleanToImageConverter : IValueConverter
{
    private const string baseUri = "pack://application:,,,/Images";

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool booleanValue)
        {
            return new BitmapImage(new Uri(booleanValue ? $"{baseUri}/completed.png" : $"{baseUri}/incomplete.png"));
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}