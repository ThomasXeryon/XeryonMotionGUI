using System;
using Microsoft.UI.Xaml.Data;

namespace XeryonMotionGUI.Helpers;

public class TooltipValueFormatter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double doubleValue)
        {
            return $"{parameter}: {doubleValue:F2} ms";
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
