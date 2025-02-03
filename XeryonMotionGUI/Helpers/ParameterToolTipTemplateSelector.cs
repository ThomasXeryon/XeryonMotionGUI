using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XeryonMotionGUI.Classes;

namespace XeryonMotionGUI.Helpers
{
    public class ParameterToolTipTemplateSelector : DataTemplateSelector
    {
        public DataTemplate GraphTemplate
        {
            get; set;
        }
        public DataTemplate DefaultTemplate
        {
            get; set;
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is Parameter param && param.Command == "FREQ")
            {
                return GraphTemplate;
            }
            return DefaultTemplate;
        }
    }
}
