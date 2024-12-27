using Microsoft.UI.Xaml.Controls;

using XeryonMotionGUI.ViewModels;

namespace XeryonMotionGUI.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel
    {
        get;
    }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
    }
}
