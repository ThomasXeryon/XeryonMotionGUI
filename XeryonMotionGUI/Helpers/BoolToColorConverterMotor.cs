using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace XeryonMotionGUI.Helpers
{
    public class BoolToColorConverterMotor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                // If the boolean is true, return orange, otherwise transparant
                return boolValue ? new SolidColorBrush(Colors.Orange) : new SolidColorBrush(Colors.Transparent);
            }
            return new SolidColorBrush(Colors.Transparent); // Default color if not boolean
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // We don't need to implement ConvertBack for this case
            throw new NotImplementedException();
        }
    }
}
