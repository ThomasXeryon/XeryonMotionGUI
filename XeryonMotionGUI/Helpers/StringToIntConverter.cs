using System;
using Microsoft.UI.Xaml.Data;

namespace XeryonMotionGUI.Helpers
{
    public class StringToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (int.TryParse(value as string, out int result))
            {
                return result;
            }
            return 0; // Default to 0 if conversion fails
        }
    }
}
