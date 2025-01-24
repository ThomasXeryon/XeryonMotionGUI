using Microsoft.UI.Xaml.Data;
using System;

namespace XeryonMotionGUI.Helpers
{
    public class NullToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value != null; // Returns true if value is not null, false otherwise
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException(); // Not needed for one-way binding
        }
    }
}