using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;

    }

    private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        ViewModel.ShellPageBackground = new SolidColorBrush(args.NewColor);
        System.Diagnostics.Debug.WriteLine($"ShellPageBackground updated to: {args.NewColor}");
    }
}
