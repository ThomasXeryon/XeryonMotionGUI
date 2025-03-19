using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using XeryonMotionGUI.Contracts.Services;
using XeryonMotionGUI.Helpers;
using XeryonMotionGUI.ViewModels;
using XeryonMotionGUI.Views;
using Windows.Storage;
using System.Runtime.InteropServices; // For P/Invoke

namespace XeryonMotionGUI.Views
{
    public sealed partial class ShellPage : Page
    {
        public ShellViewModel ViewModel
        {
            get;
        }
        private bool _hasShownIntroDialog = false; // Flag to prevent multiple dialogs

        // Win32 API declarations
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int SW_SHOWNORMAL = 1; // Show window in normal state
        private const int SW_MAXIMIZE = 3;   // Maximize window

        public ShellPage(ShellViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();

            ViewModel.NavigationService.Frame = NavigationFrame;
            ViewModel.NavigationViewService.Initialize(NavigationViewControl);

            // Title bar customization
            App.MainWindow.ExtendsContentIntoTitleBar = true;
            App.MainWindow.SetTitleBar(AppTitleBar);
            App.MainWindow.Activated += MainWindow_Activated;
            AppTitleBarText.Text = "AppDisplayName".GetLocalized();

            DataContext = ViewModel;

            Loaded += OnLoaded; // Hook up the Loaded event
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            TitleBarHelper.UpdateTitleBar(RequestedTheme);

            KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu));
            KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.GoBack));

            // Set the app to maximized and ensure it’s visible
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            ShowWindow(hWnd, SW_SHOWNORMAL); // First ensure the window is shown
            ShowWindow(hWnd, SW_MAXIMIZE);   // Then maximize it
            SetForegroundWindow(hWnd);       // Bring it to the foreground

            // Show intro dialog only once
            if (!_hasShownIntroDialog)
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                bool showIntro = !localSettings.Values.ContainsKey("ShowIntro") || (bool)localSettings.Values["ShowIntro"];

                if (showIntro)
                {
                    _hasShownIntroDialog = true; // Set flag to prevent re-showing
                    var introDialog = new IntroDialog
                    {
                        XamlRoot = this.XamlRoot
                    };
                    try
                    {
                        await introDialog.ShowAsync();
                    }
                    catch (System.Runtime.InteropServices.COMException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to show IntroDialog: {ex.Message}");
                    }
                }
            }
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            App.AppTitlebar = AppTitleBarText as UIElement;
        }

        private void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {
            AppTitleBar.Margin = new Thickness()
            {
                Left = sender.CompactPaneLength * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
                Top = AppTitleBar.Margin.Top,
                Right = AppTitleBar.Margin.Right,
                Bottom = AppTitleBar.Margin.Bottom
            };
        }

        private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
        {
            var keyboardAccelerator = new KeyboardAccelerator() { Key = key };

            if (modifiers.HasValue)
            {
                keyboardAccelerator.Modifiers = modifiers.Value;
            }

            keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;

            return keyboardAccelerator;
        }

        private static void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var navigationService = App.GetService<INavigationService>();
            var result = navigationService.GoBack();
            args.Handled = result;
        }

        private void PopOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationFrame.Content is Page currentPage)
            {
                var pageType = currentPage.GetType();
                var newWindow = new Window();
                var navRootPage = new NavigationRootPage();

                if (this.ActualTheme == ElementTheme.Dark)
                {
                    navRootPage.RequestedTheme = ElementTheme.Dark;
                }
                else
                {
                    navRootPage.RequestedTheme = ElementTheme.Light;
                }

                newWindow.Content = navRootPage;
                newWindow.Activate();
                navRootPage.Navigate(pageType, null);
            }
            else
            {
                ShowErrorDialog("The current content is not a valid page.");
            }
        }

        private async void ShowErrorDialog(string message)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            try
            {
                await errorDialog.ShowAsync();
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show ErrorDialog: {ex.Message}");
            }
        }
    }
}