using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace XeryonMotionGUI.Helpers
{
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibilityValue)
            {
                return visibilityValue != Visibility.Visible;
            }
            return false;
        }
    }
}