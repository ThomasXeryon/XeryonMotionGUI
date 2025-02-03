using Microsoft.UI.Xaml;

namespace XeryonMotionGUI.Contracts.Services
{
    public interface IThemeSelectorService
    {
        ElementTheme Theme
        {
            get;
        }

        // Add the ThemeChanged event.
        event EventHandler<ElementTheme> ThemeChanged;

        Task InitializeAsync();

        Task SetThemeAsync(ElementTheme theme);

        Task SetRequestedThemeAsync();
    }
}
