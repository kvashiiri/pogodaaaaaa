using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WeatherApp.Helpers
{
    public class BoolToStarColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool fav && fav)
                return new SolidColorBrush(Colors.Gold);
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
