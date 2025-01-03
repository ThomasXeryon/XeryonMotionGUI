using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using XeryonMotionGUI.Classes;

namespace XeryonMotionGUI.Helpers;

public class AxisTypeToVisibilityConverter : IValueConverter
{
public object Convert(object value, Type targetType, object parameter, string language)
{
    if (value is Axis axis)
    {
        // Check axis type and return visibility accordingly
        // Replace these conditions with the actual axis types you're using
        if (axis.Type != "Invis")  // Replace "SpecificType" with your actual type condition
        {
            return Visibility.Visible;
        }
    }
    return Visibility.Collapsed;
}

public object ConvertBack(object value, Type targetType, object parameter, string language)
{
    throw new NotImplementedException();
}
}