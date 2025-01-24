using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace XeryonMotionGUI.Helpers
{
    public class RepeatBlockToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Check if the value is "Repeat" and return Visible; otherwise, return Collapsed
            if (value is string text && text == "Repeat")
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
