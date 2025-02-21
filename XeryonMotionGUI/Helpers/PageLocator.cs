using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeryonMotionGUI.Views;

namespace XeryonMotionGUI.Helpers
{
    public static class PageLocator
    {
        // You can store the current DemoBuilderPage instance here.
        // Make sure that your DemoBuilderPage sets this property in its constructor or OnNavigatedTo.
        public static DemoBuilderPage CurrentDemoBuilderPage
        {
            get; set;
        }

        public static DemoBuilderPage GetDemoBuilderPage()
        {
            return CurrentDemoBuilderPage;
        }
    }
}

