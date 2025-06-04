using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace XeryonMotionGUI.Helpers
{
    public class ControllerTypeToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Expecting the controller type as a string.
            if (value is string controllerType && !string.IsNullOrWhiteSpace(controllerType))
            {
                // Build the URI using the controller type.
                // For example, if controllerType is "XD-OEM", this will point to:
                // "ms-appx:///Assets/Images/Controllers/XD-OEM/default.png"
                string uriString = $"ms-appx:///Assets/Images/Controllers/{controllerType}/Default.png";
                try
                {
                    return new BitmapImage(new Uri(uriString));
                }
                catch (Exception)
                {
                    // Optionally log the error or return a fallback image.
                    return null;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
