using Microsoft.UI.Xaml.Controls;

using XeryonMotionGUI.ViewModels;

namespace XeryonMotionGUI.Views;

public sealed partial class MotionPage : Page
{
    public MotionViewModel ViewModel
    {
        get;
    }

    public MotionPage()
    {
        ViewModel = App.GetService<MotionViewModel>();
        InitializeComponent();
    }
}
