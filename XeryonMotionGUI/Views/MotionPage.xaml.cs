using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives; // For RangeBaseValueChangedEventArgs
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using WinRT.Interop; // For WindowNative, Win32Interop
using Newtonsoft.Json.Linq;

using XeryonMotionGUI.Classes;
using XeryonMotionGUI.Helpers;
using XeryonMotionGUI.ViewModels;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Windows.Input;
using OxyPlot.Wpf;
using System.Windows.Documents;

namespace XeryonMotionGUI.Views
{
    public sealed partial class MotionPage : Page
    {
        private bool _suppressSliderValueChanged = false;
        private PlotModel _plotModel;
        private LineSeries _positionSeries;
        private LinearAxis xAxis;
        private LinearAxis yAxis;

        // Fields for tear-out logic
        private Window tearOutWindow = null;
        // If you use a helper class for Win32 sizing:
        // private Win32WindowHelper win32WindowHelper;

        public MotionPage()
        {
            InitializeComponent();

            // Keep your existing constructor code
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
            this.DataContext = new MotionViewModel(); // Set DataContext here

            PositionSlider.AddHandler(
                UIElement.PointerPressedEvent,
                new PointerEventHandler(Slider_PointerPressed),
                handledEventsToo: true);

            PositionSlider.AddHandler(
                UIElement.PointerReleasedEvent,
                new PointerEventHandler(Slider_PointerReleased),
                handledEventsToo: true);

            this.ActualThemeChanged += OnActualThemeChanged;

            Debug.WriteLine($"MotionPage constructor. Current theme: {this.ActualTheme}");

            // If you want to do special window setup once the page is loaded:
            this.Loaded += MotionPage_Loaded;
            //Tabs.control
        }



        private void MotionPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Example: get the window that hosts this page

        }

        public void SetupWindowMinSize(Window window)
        {
            // If using a Win32WindowHelper class:
            // win32WindowHelper = new Win32WindowHelper(window);
            // win32WindowHelper.SetWindowMinMaxSize(new Win32WindowHelper.POINT { x = 500, y = 300 });
        }

        private MotionViewModel ViewModel => (MotionViewModel)DataContext;

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            // Create a new flyout
            var flyout = new Flyout
            {
                Placement = FlyoutPlacementMode.Bottom
            };

            // Create the content for the flyout
            var stackPanel = new StackPanel { Spacing = 20, Width = 400 };

            // LOGGING group
            var loggingGroup = new StackPanel();
            loggingGroup.Children.Add(new TextBlock
            {
                Text = "Logging Settings",
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 5)
            });

            var loggingToggleStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center
            };
            loggingToggleStack.Children.Add(new TextBlock
            {
                Text = "Logging Mode:",
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center
            });

            var loggingToggle = new ToggleSwitch
            {
                IsOn = ViewModel.SelectedAxis.AutoLogging,
                OnContent = "Auto",
                OffContent = "Manual"
            };
            loggingToggle.Toggled += (s, args) => {
                ViewModel.SelectedAxis.AutoLogging = loggingToggle.IsOn;
            };
            loggingToggleStack.Children.Add(loggingToggle);

            var manualLoggingStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 0, 0, 0),
                Visibility = ViewModel.SelectedAxis.AutoLogging ? Visibility.Collapsed : Visibility.Visible
            };
            manualLoggingStack.Children.Add(new Button
            {
                Content = "Start Logging",
                Command = ViewModel.SelectedAxis.StartManualLoggingCommand,
                Margin = new Thickness(0, 0, 5, 0)
            });
            manualLoggingStack.Children.Add(new Button
            {
                Content = "Stop Logging",
                Command = ViewModel.SelectedAxis.StopManualLoggingCommand
            });

            loggingToggleStack.Children.Add(manualLoggingStack);
            loggingGroup.Children.Add(loggingToggleStack);

            // TIME SOURCE group
            var timeSourceGroup = new StackPanel();
            timeSourceGroup.Children.Add(new TextBlock
            {
                Text = "Time Source",
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 5)
            });

            var timeSourceStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0)
            };
            timeSourceStack.Children.Add(new TextBlock
            {
                Text = "Use Controller Time:",
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center
            });

            var timeSourceToggle = new ToggleSwitch
            {
                IsOn = ViewModel.SelectedAxis.UseControllerTime,
                OnContent = "Controller",
                OffContent = "System"
            };
            timeSourceToggle.Toggled += (s, args) => {
                ViewModel.SelectedAxis.UseControllerTime = timeSourceToggle.IsOn;
            };
            timeSourceStack.Children.Add(timeSourceToggle);
            timeSourceGroup.Children.Add(timeSourceStack);

            // Add all groups to the main stack panel
            stackPanel.Children.Add(loggingGroup);
            stackPanel.Children.Add(timeSourceGroup);

            // Set the flyout content and show it
            flyout.Content = stackPanel;
            flyout.ShowAt((FrameworkElement)sender);
        }

        /// <summary>
        /// Finds the TabView that currently owns the given TabViewItem, if any.
        /// (Used to remove it from its old parent.)
        /// </summary>
        private TabView GetParentTabView(TabViewItem tab)
        {
            DependencyObject current = tab;
            while (current != null)
            {
                if (current is TabView tv) return tv;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        // =========================================
        // ========== Your Existing Code ===========
        // =========================================

        private void PositionSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_suppressSliderValueChanged)
                return;

            var viewModel = (MotionViewModel)this.DataContext;
            if (viewModel.SelectedAxis != null)
            {
                // Example: set DPOS
                viewModel.SelectedAxis.SetDPOS((int)(e.NewValue * 1000000 / viewModel.SelectedAxis.Resolution));
            }
        }

        private void TextBox_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var viewModel = (MotionViewModel)this.DataContext;
            viewModel.SelectedAxis.MaxSpeed = 0;
        }

        private void Ellipse_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // Example of handling ellipse manipulation
            var ellipse = sender as Microsoft.UI.Xaml.Shapes.Ellipse;
            if (ellipse != null)
            {
                Debug.WriteLine($"Translation: {e.Delta.Translation}");
                Debug.WriteLine($"Rotation: {e.Delta.Rotation}");
            }

            e.Handled = true;
        }

        private async void EPOSTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                var textBox = sender as TextBox;
                if (textBox != null && double.TryParse(textBox.Text, out double displayValue))
                {
                    if (this.DataContext is MotionViewModel vm && vm.SelectedAxis != null)
                    {
                        double encoderValue = UnitConversion.ToEncoder(
                            displayValue,
                            vm.SelectedAxis.SelectedUnit,
                            vm.SelectedAxis.Resolution);
                        await vm.SelectedAxis.SetDPOS(encoderValue);
                    }
                }
            }
        }

        private async void StepSizeNumberBox_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is NumberBox nb)
            {
                nb.IsEnabled = false;
                await Task.Delay(50);
                nb.IsEnabled = true;
            }
        }

        private async void StepSizeNumberBox_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is NumberBox nb)
            {
                nb.IsEnabled = false;
                await Task.Delay(50);
                nb.IsEnabled = true;
            }
        }

        private void ExportGraph_Click(object sender, RoutedEventArgs e)
        {
            // 1) Access your ViewModel + PlotModel
            if (this.DataContext is MotionViewModel vm && vm.SelectedAxis is not null)
            {
                PlotModel model = vm.SelectedAxis.PlotModel;
                if (model == null) return;

                // 2) Choose a file name (e.g. in TEMP folder)
                string filename = Path.Combine(
                    Path.GetTempPath(),
                    "MyAxisPlot.png");




                // 3) Perform export
                PngExporter.Export(model, filename, width: 1920, height: 1080);

                // 4) Open the PNG
                var processInfo = new ProcessStartInfo
                {
                    FileName = filename,
                    UseShellExecute = true
                };
                Process.Start(processInfo);




                // Optionally re-render the plot in your UI
               // model.InvalidatePlot(false);
            }
        }


        private void InitializePlot()
        {
            ApplyThemeToPlotModel();
            _plotModel = new PlotModel
            {
                Title = "Axis Movement Over Time",
                Background = OxyColors.Transparent
            };

            _positionSeries = new LineSeries
            {
                Title = "Position (mm)",
                MarkerType = MarkerType.Circle,
                MarkerSize = 2,
                MarkerStroke = OxyColors.Transparent,
                StrokeThickness = 1.5,
                LineStyle = LineStyle.Solid,
            };
            _plotModel.Series.Add(_positionSeries);

            xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Time (s)",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true
            };

            yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Position (enc)",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true
            };

            _plotModel.Axes.Add(xAxis);
            _plotModel.Axes.Add(yAxis);

            _plotModel.IsLegendVisible = true;
            ApplyThemeToPlotModel();
        }



        private void ApplyThemeToPlotModel()
        {
            Debug.WriteLine("Applying theme to plot");
            var xAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
            var positionAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Left);
            var speedAxis = _plotModel.Axes.FirstOrDefault(a => a.Key == "SpeedAxis");
            // Example theme logic
            var frame = App.AppTitlebar as FrameworkElement;
            if (frame != null && frame.ActualTheme == ElementTheme.Dark)
            {
                // Dark theme => white lines
                xAxis.AxislineColor = OxyColors.White;
                xAxis.TextColor = OxyColors.White;
                xAxis.TitleColor = OxyColors.White;
                xAxis.TicklineColor = OxyColors.White;

                yAxis.AxislineColor = OxyColors.White;
                yAxis.TextColor = OxyColors.White;
                yAxis.TitleColor = OxyColors.White;
                yAxis.TicklineColor = OxyColors.White;

                _plotModel.TextColor = OxyColors.White;

                if (xAxis != null) xAxis.TextColor = OxyColors.White;
                if (positionAxis != null) positionAxis.TextColor = OxyColors.White;
                if (speedAxis != null) speedAxis.TextColor = OxyColors.White;

                _plotModel.TextColor = OxyColors.White;
                _plotModel.TitleColor = OxyColors.White;
            }
            else
            {
                // Light theme => black lines
                xAxis.AxislineColor = OxyColors.Black;
                xAxis.TextColor = OxyColors.Black;
                xAxis.TitleColor = OxyColors.Black;
                xAxis.TicklineColor = OxyColors.Black;

                yAxis.AxislineColor = OxyColors.Black;
                yAxis.TextColor = OxyColors.Black;
                yAxis.TitleColor = OxyColors.Black;
                yAxis.TicklineColor = OxyColors.Black;

                _plotModel.TextColor = OxyColors.Black;

                if (xAxis != null) xAxis.TextColor = OxyColors.Black;
                if (positionAxis != null) positionAxis.TextColor = OxyColors.Black;
                if (speedAxis != null) speedAxis.TextColor = OxyColors.Black;

                _plotModel.TextColor = OxyColors.Black;
                // If you want the title "Axis Movement and Speed Over Time" to remain black:
                _plotModel.TitleColor = OxyColors.Black;
            }

            _plotModel.InvalidatePlot(false);
        }

        private void OnActualThemeChanged(FrameworkElement sender, object args)
        {
            Debug.WriteLine("Updating colors");
            if (_plotModel != null)
            {
                ApplyThemeToPlotModel();
            }
        }

        private void RadialGauge_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine("Starting drag (RadialGauge)");
            if (DataContext is MotionViewModel viewModel)
            {
                viewModel.SelectedAxis.IsUserDraggingSlider = true;
            }
        }

        private void RadialGauge_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine("Stopped drag (RadialGauge)");
            if (DataContext is MotionViewModel viewModel)
            {
                viewModel.SelectedAxis.IsUserDraggingSlider = false;
                viewModel.SelectedAxis.UpdateSliderValue(viewModel.SelectedAxis.SliderValue);
            }
        }

        private void Slider_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine("Starting drag (Slider)");
            if (DataContext is MotionViewModel viewModel)
            {
                viewModel.SelectedAxis.IsUserDraggingSlider = true;
            }
        }

        private void Slider_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine("Stopped drag (Slider)");
            if (DataContext is MotionViewModel viewModel)
            {
                viewModel.SelectedAxis.IsUserDraggingSlider = false;
                viewModel.SelectedAxis.UpdateSliderValue(viewModel.SelectedAxis.SliderValue);
            }
        }

        private void OpenJoystickPageButton_Click(object sender, RoutedEventArgs e)
        {
            var newWindow = new Window();
            var navRootPage = new NavigationRootPage();

            // Example: match theme
            if (this.ActualTheme == ElementTheme.Dark)
                navRootPage.RequestedTheme = ElementTheme.Dark;
            else
                navRootPage.RequestedTheme = ElementTheme.Light;

            newWindow.Content = navRootPage;
            newWindow.SetWindowSize(400, 470);
            newWindow.Activate();

            // If you want always-on-top:
            IntPtr hWnd = WindowNative.GetWindowHandle(newWindow);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow.Presenter is OverlappedPresenter overlappedPresenter)
            {
                overlappedPresenter.IsAlwaysOnTop = true;
            }

            if (this.DataContext is MotionViewModel vm)
            {
                var axes = vm.AllAxes;
                navRootPage.Navigate(typeof(JoystickWindow), axes);
            }
        }
    }
}
