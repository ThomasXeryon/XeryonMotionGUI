using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Diagnostics;

namespace XeryonMotionGUI.Helpers
{
    public class TypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                Debug.WriteLine("TypeToVisibilityConverter: Value is null.");
                return Visibility.Collapsed;
            }

            if (parameter == null)
            {
                Debug.WriteLine("TypeToVisibilityConverter: Parameter is null.");
                return Visibility.Collapsed;
            }

            string expectedTypeName = parameter.ToString();
            string actualTypeName = value.GetType().Name;

            Debug.WriteLine($"TypeToVisibilityConverter: ExpectedType={expectedTypeName}, ActualType={actualTypeName}");

            return actualTypeName == expectedTypeName ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
