using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XeryonMotionGUI.Contracts.Services;

namespace XeryonMotionGUI.Views
{
    public sealed partial class NavigationRootPage : Page
    {
        private readonly IThemeSelectorService _themeSelectorService;

        public NavigationRootPage()
        {
            this.InitializeComponent();

            _themeSelectorService = App.GetService<IThemeSelectorService>();

            // Set the initial theme.
            this.RequestedTheme = _themeSelectorService.Theme;

            // Subscribe to theme changes.
            _themeSelectorService.ThemeChanged += OnThemeChanged;

            // Unsubscribe when unloaded to avoid memory leaks.
            this.Unloaded += (s, e) =>
            {
                _themeSelectorService.ThemeChanged -= OnThemeChanged;
            };
        }

        private void OnThemeChanged(object sender, ElementTheme newTheme)
        {
            // Update the RequestedTheme to reflect the new theme.
            this.RequestedTheme = newTheme;
        }

        public void Navigate(System.Type pageType, object parameter = null)
        {
            ContentFrame.Navigate(pageType, parameter);
        }
    }
}
