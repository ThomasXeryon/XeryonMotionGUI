using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Linq;

namespace XeryonMotionGUI.Helpers
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // 1. If value is a bool, show Visible if true, Collapsed if false
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }

            // 2. If value is a string, compare it to the allowed tokens
            if (value is string text && parameter is string allowedTexts)
            {
                // Example: parameter="Step +|Step -" => tokens=["Step +","Step -"]
                var tokens = allowedTexts
                    .Split('|')
                    .Select(t => t.Trim()) // trim extra spaces
                    .ToArray();

                // Compare trimmed text
                text = text.Trim();

                // If the text matches any token, return Visible
                return tokens.Contains(text) ? Visibility.Visible : Visibility.Collapsed;
            }

            // Default to Collapsed if we can't handle the scenario
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
