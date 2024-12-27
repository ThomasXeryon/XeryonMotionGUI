using Microsoft.UI.Xaml.Controls;

using XeryonMotionGUI.ViewModels;

namespace XeryonMotionGUI.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
    }
}
