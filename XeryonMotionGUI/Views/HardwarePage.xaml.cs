using Microsoft.UI.Xaml.Controls;

using XeryonMotionGUI.ViewModels;

namespace XeryonMotionGUI.Views;

public sealed partial class HardwarePage : Page
{
    public HardwareViewModel ViewModel
    {
        get;
    }

    public HardwarePage()
    {
        ViewModel = App.GetService<HardwareViewModel>();
        InitializeComponent();
    }
}
