using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace XeryonMotionGUI.Helpers
{
    public class WaitBlockToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (value as string) == "Wait" ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}