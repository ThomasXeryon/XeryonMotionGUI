using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI; // For Microsoft.UI.Colors
using Windows.System;
using XeryonMotionGUI.Contracts.Services;
using XeryonMotionGUI.Helpers;
using XeryonMotionGUI.ViewModels;
using XeryonMotionGUI.Views;
using Windows.Storage;
using System.Runtime.InteropServices;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI;

namespace XeryonMotionGUI.Views
{
    public sealed partial class ShellPage : Page
    {
        public ShellViewModel ViewModel
        {
            get;
        }

        private bool _hasShownIntroDialog = false;

        // ParametersViewModel to access controllers, axes, and parameters
        private readonly ParametersViewModel _parametersViewModel;

        // Win32 API declarations
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int SW_SHOWNORMAL = 1;
        private const int SW_MAXIMIZE = 3;

        // Chat message history

        public ShellPage(ShellViewModel viewModel)
        {
            ViewModel = viewModel;
            // Initialize ParametersViewModel to access controllers, axes, and parameters
            _parametersViewModel = new ParametersViewModel();
            InitializeComponent();

            ViewModel.NavigationService.Frame = NavigationFrame;
            ViewModel.NavigationViewService.Initialize(NavigationViewControl);

            // Title bar customization
            App.MainWindow.ExtendsContentIntoTitleBar = true;
            App.MainWindow.SetTitleBar(AppTitleBar);
            App.MainWindow.Activated += MainWindow_Activated;
            AppTitleBarText.Text = "AppDisplayName".GetLocalized();

            DataContext = ViewModel;

            Loaded += OnLoaded;


        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            TitleBarHelper.UpdateTitleBar(RequestedTheme);

            KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu));
            KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.GoBack));

            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            //ShowWindow(hWnd, SW_SHOWNORMAL);
            //ShowWindow(hWnd, SW_MAXIMIZE);
            SetForegroundWindow(hWnd);

            //TEMPORARELY DISABLE
            _hasShownIntroDialog = true;

            if (!_hasShownIntroDialog)
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                bool showIntro = !localSettings.Values.ContainsKey("ShowIntro")
                                 || (bool)localSettings.Values["ShowIntro"];

                if (showIntro)
                {
                    _hasShownIntroDialog = true;
                    var introDialog = new IntroDialog { XamlRoot = this.XamlRoot };
                    try
                    {
                        await introDialog.ShowAsync();
                    }
                    catch (COMException ex)
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
            AppTitleBar.Margin = new Thickness
            {
                Left = sender.CompactPaneLength * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
                Top = AppTitleBar.Margin.Top,
                Right = AppTitleBar.Margin.Right,
                Bottom = AppTitleBar.Margin.Bottom
            };

            // Update chat visibility when display mode changes
        }



        private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
        {
            var keyboardAccelerator = new KeyboardAccelerator { Key = key };
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
            catch (COMException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show ErrorDialog: {ex.Message}");
            }
        }

        // Method to gather controller, axis, and parameter information
        private string GetSystemContext()
        {
            var context = new StringBuilder();
            context.AppendLine("This message comes from the Xeryon software. The following information about the current system state (controllers, axes, and parameters) is provided for informational purposes to give context to the user's message:\n");

            // Controllers
            if (_parametersViewModel.HasRunningControllers)
            {
                context.AppendLine("### Connected Controllers:");
                foreach (var controller in _parametersViewModel.RunningControllers)
                {
                    context.AppendLine($"- Controller: {controller.Name}");
                    if (controller.Axes != null && controller.Axes.Any())
                    {
                        context.AppendLine("  Axes:");
                        foreach (var axis in controller.Axes)
                        {
                            context.AppendLine($"    - Axis: {axis.Name}");
                        }
                    }
                    else
                    {
                        context.AppendLine("  No axes available.");
                    }
                }
            }
            else
            {
                context.AppendLine("### Connected Controllers: None");
            }

            // Selected Controller and Axis
            if (_parametersViewModel.SelectedController != null)
            {
                context.AppendLine($"\n### Selected Controller: {_parametersViewModel.SelectedController.Name}");
                if (_parametersViewModel.SelectedAxis != null)
                {
                    context.AppendLine($"### Selected Axis: {_parametersViewModel.SelectedAxis.Name}");
                }
                else
                {
                    context.AppendLine("### Selected Axis: None");
                }
            }
            else
            {
                context.AppendLine("\n### Selected Controller: None");
                context.AppendLine("### Selected Axis: None");
            }

            // Parameters
            if (_parametersViewModel.GroupedParameters != null && _parametersViewModel.GroupedParameters.Any())
            {
                context.AppendLine("\n### Parameters (Grouped by Category):");
                foreach (var group in _parametersViewModel.GroupedParameters)
                {
                    context.AppendLine($"#### Category: {group.Category}");
                    if (group.Parameters != null && group.Parameters.Any())
                    {
                        foreach (var parameter in group.Parameters)
                        {
                            context.AppendLine($"  - {parameter.Name}: {parameter.Value}");
                        }
                    }
                    else
                    {
                        context.AppendLine("  No parameters in this category.");
                    }
                }
            }
            else
            {
                context.AppendLine("\n### Parameters: None");
            }

            context.AppendLine("\n--- End of system context ---\n");
            return context.ToString();
        }


    }


}