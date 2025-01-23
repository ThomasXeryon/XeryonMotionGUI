using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.UI;
using Windows.Storage;

using XeryonMotionGUI.Contracts.Services;
using XeryonMotionGUI.Views;

namespace XeryonMotionGUI.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    [ObservableProperty]
    private SolidColorBrush shellPageBackground;

    private const string ShellPageBackgroundColorKey = "ShellPageBackgroundColor";

    [ObservableProperty]
    private bool isBackEnabled;

    [ObservableProperty]
    private object? selected;

    public INavigationService NavigationService
    {
        get;
    }

    public INavigationViewService NavigationViewService
    {
        get;
    }

    public ShellViewModel(INavigationService navigationService, INavigationViewService navigationViewService)
    {
        // Load saved background color or set default
        LoadBackgroundColor();
        System.Diagnostics.Debug.WriteLine($"Initial color: {ShellPageBackground.Color}");
        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
        NavigationViewService = navigationViewService;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        IsBackEnabled = NavigationService.CanGoBack;

        if (e.SourcePageType == typeof(SettingsPage))
        {
            Selected = NavigationViewService.SettingsItem;
            return;
        }

        var selectedItem = NavigationViewService.GetSelectedItem(e.SourcePageType);
        if (selectedItem != null)
        {
            Selected = selectedItem;
        }
    }

    partial void OnShellPageBackgroundChanged(SolidColorBrush value)
    {
        SaveBackgroundColor(value.Color);
        System.Diagnostics.Debug.WriteLine($"Color saved: {value.Color}");
    }

    private void LoadBackgroundColor()
    {
        var localSettings = ApplicationData.Current.LocalSettings;

        if (localSettings.Values.TryGetValue(ShellPageBackgroundColorKey, out var savedColor))
        {
            // Parse the saved color (stored as a string)
            if (TryParseColor((string)savedColor, out var color))
            {
                ShellPageBackground = new SolidColorBrush(color);
                return;
            }
        }

        // Default color if no saved value exists
        ShellPageBackground = new SolidColorBrush(Color.FromArgb(0x33, 0x00, 0x80, 0x02));
    }

    private void SaveBackgroundColor(Windows.UI.Color color)
    {
        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        // Save the color as a string in ARGB format
        var colorString = $"{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        localSettings.Values[ShellPageBackgroundColorKey] = colorString;

        System.Diagnostics.Debug.WriteLine($"Color saved to settings: {colorString}");
    }

    private bool TryParseColor(string colorString, out Color color)
    {
        if (!string.IsNullOrEmpty(colorString) && colorString.Length == 8 &&
            byte.TryParse(colorString.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out var a) &&
            byte.TryParse(colorString.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var r) &&
            byte.TryParse(colorString.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var g) &&
            byte.TryParse(colorString.Substring(6, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            color = Color.FromArgb(a, r, g, b);
            return true;
        }

        color = default;
        return false;
    }
}
