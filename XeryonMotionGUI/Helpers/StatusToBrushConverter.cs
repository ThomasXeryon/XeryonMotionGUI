using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace XeryonMotionGUI.Helpers
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string status)
            {
                switch (status.ToLower())
                {
                    case "connect":
                        return new SolidColorBrush(Microsoft.UI.Colors.Green);
                    case "disconnect":
                        return new SolidColorBrush(Microsoft.UI.Colors.Red);
                    case "idle":
                        return new SolidColorBrush(Microsoft.UI.Colors.Yellow);
                    default:
                        return new SolidColorBrush(Microsoft.UI.Colors.Gray);
                }
            }
            return new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
