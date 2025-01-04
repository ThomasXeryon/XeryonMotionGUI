using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Controls;

namespace XeryonMotionGUI.Helpers
{
    public class LinearToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isLinear = (bool)value;
            return isLinear ? Symbol.Forward : Symbol.Rotate;  // Use RightArrow for linear and Rotate for non-linear
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
