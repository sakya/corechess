using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreChess.Converters
{
    public class CapturedPiecesToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            Settings.CapturedPiecesDisplay setting;
            if (Enum.TryParse<Settings.CapturedPiecesDisplay>((string)parameter, out setting))
                return (Settings.CapturedPiecesDisplay)value == setting;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
