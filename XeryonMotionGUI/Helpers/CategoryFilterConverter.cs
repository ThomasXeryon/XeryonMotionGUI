using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using XeryonMotionGUI.Classes;


namespace XeryonMotionGUI.Helpers
{
    public class CategoryFilterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // value is your full collection of parameters
            // parameter is the category string (e.g. "MotionTuning")
            if (value is IEnumerable<Parameter> allParams && parameter is string category)
            {
                return allParams.Where(p => p.Category == category);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
