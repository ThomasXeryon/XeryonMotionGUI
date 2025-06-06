// XeryonMotionGUI/Helpers/CountToInverseVisibilityConverter.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections; // Required for ICollection

namespace XeryonMotionGUI.Helpers
{
    public class CountToInverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int count)
            {
                return count > 0 ? Visibility.Collapsed : Visibility.Visible;
            }
            if (value is ICollection collection)
            {
                return collection.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return 1;
            //throw new NotImplementedException();
        }
    }
}