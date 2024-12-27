using Microsoft.UI.Xaml.Controls;

using XeryonMotionGUI.ViewModels;

namespace XeryonMotionGUI.Views;

public sealed partial class ParametersPage : Page
{
    public ParametersViewModel ViewModel
    {
        get;
    }

    public ParametersPage()
    {
        ViewModel = App.GetService<ParametersViewModel>();
        InitializeComponent();
    }
}
