using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using Microsoft.UI.Xaml.Data;

namespace XeryonMotionGUI.Helpers;
public class DoubleFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double d)
        {
            // If you pass "F3" as ConverterParameter, it will use 3 decimals
            string format = parameter as string ?? "F2";
            return d.ToString(format);
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        // Optional, if you want two-way usage
        if (value is string s && double.TryParse(s, out double d))
        {
            return d;
        }
        return 0.0;
    }
}
