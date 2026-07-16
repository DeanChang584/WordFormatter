using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace WordFormatterUI.Converters;

/// <summary>
/// Converts a boolean value to Visibility.
/// true → Visible, false → Collapsed
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility.Visible;
    }
}

/// <summary>
/// Inverts a boolean value to Visibility.
/// true → Collapsed, false → Visible
/// </summary>
public class InvertBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is not Visibility.Visible;
    }
}