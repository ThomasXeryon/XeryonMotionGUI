using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using XeryonMotionGUI.Classes;
using Microsoft.UI.Xaml.Controls.Primitives; // For RangeBaseValueChangedEventArgs
using XeryonMotionGUI.ViewModels;
using System.Diagnostics;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using System.Windows.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using WinRT.Interop; // for WindowNative, Win32Interop
using Windows.System;
using XeryonMotionGUI.Helpers;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using Windows.Media.Devices;
using Newtonsoft.Json.Linq;
using Microsoft.UI;
using WinRT.Interop;
using Microsoft.UI.Windowing;

namespace XeryonMotionGUI.Views;

public sealed partial class MotionPage : Page
{
    private bool _suppressSliderValueChanged = false;
    private PlotModel _plotModel;
    private LineSeries _positionSeries;
    private LinearAxis xAxis;
    private LinearAxis yAxis;
    public MotionPage()
    {
        InitializeComponent();
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

    }

    private void PositionSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_suppressSliderValueChanged)
            return;

        var viewModel = (MotionViewModel)this.DataContext;
        if (viewModel.SelectedAxis != null)
        {
            viewModel.SelectedAxis.SetDPOS((int)(e.NewValue * 1000000 / viewModel.SelectedAxis.Resolution));
        }
    }


    private void TextBox_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var viewModel = (MotionViewModel)this.DataContext;
        viewModel.SelectedAxis.MaxSpeed = 0;
    }

    private void Ellipse_ManipulationDelta(object sender, Microsoft.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
    {
        // Implement your logic for handling the manipulation of the Ellipse.
        // Example: Update the position or value of the circular slider.

        var ellipse = sender as Microsoft.UI.Xaml.Shapes.Ellipse;

        if (ellipse != null)
        {
            // Example: Update position or log the manipulation data
            Debug.WriteLine($"Translation: {e.Delta.Translation}");
            Debug.WriteLine($"Rotation: {e.Delta.Rotation}");
        }

        e.Handled = true; // Mark the event as handled
    }

    private async void EPOSTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Check if the user pressed Enter
        if (e.Key == VirtualKey.Enter)
        {
            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double displayValue))
            {
                // Get the view model and selected axis.
                if (this.DataContext is MotionViewModel vm && vm.SelectedAxis != null)
                {
                    // Convert the value from the current display unit to encoder units.
                    double encoderValue = UnitConversion.ToEncoder(displayValue, vm.SelectedAxis.SelectedUnit, vm.SelectedAxis.Resolution);
                    await vm.SelectedAxis.SetDPOS(encoderValue);
                }
            }
        }
    }

    private async void StepSizeNumberBox_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is NumberBox nb)
        {
            // Temporarily disable the NumberBox to force it to lose focus.
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
        if (this.DataContext is MotionViewModel vm && vm.SelectedAxis is not null)
        {
            // 1. Access the PlotModel to export
            PlotModel model = vm.SelectedAxis.PlotModel;
            if (model == null) return;

            // 2. Choose where to save the file (hard-coded or via a file picker)
            // For simplicity, we hard-code a path; adapt for your environment:
            string filename = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "MyAxisPlot.png");
            model.Background = OxyColors.White;

            // 3. Use the SkiaSharp PngExporter (width/height in pixels, default background = transparent)
            //    You can tweak the Resolution DPI as well.
            PngExporter.Export(model, filename, width: 1920, height: 1080);

            // 4. Open the PNG in the default image viewer
            //    For WinUI 3, we can do:
            var processInfo = new ProcessStartInfo
            {
                FileName = filename,
                UseShellExecute = true
            };
            Process.Start(processInfo);
            model.Background = OxyColors.Transparent;

        }
    }

    private void InitializePlot()
    {
        // Create the base PlotModel
        _plotModel = new PlotModel
        {
            Title = "Axis Movement Over Time",
            Background = OxyColors.Transparent
        };

        // Create the main series
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

        // X Axis
        xAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Time (s)",
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot,
            IsZoomEnabled = true,
            IsPanEnabled = true
        };

        // Y Axis
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

        // Apply colors based on current theme
        ApplyThemeToPlotModel();

        // Show legend
        _plotModel.IsLegendVisible = true;

        // Assign PlotModel to a plot control, if you have one in XAML, for example:
        // myPlotView.Model = _plotModel;
    }

    private void ApplyThemeToPlotModel()
    {
        var frame = App.AppTitlebar as FrameworkElement;
        if (frame != null && frame.ActualTheme == ElementTheme.Dark)
        {
            // Dark theme => use white
            xAxis.AxislineColor = OxyColors.White;
            xAxis.TextColor = OxyColors.White;
            xAxis.TitleColor = OxyColors.White;
            xAxis.TicklineColor = OxyColors.White;

            yAxis.AxislineColor = OxyColors.White;
            yAxis.TextColor = OxyColors.White;
            yAxis.TitleColor = OxyColors.White;
            yAxis.TicklineColor = OxyColors.White;

            _plotModel.TextColor = OxyColors.White;
        }
        else
        {
            // Light (or Default) theme => use black
            xAxis.AxislineColor = OxyColors.Black;
            xAxis.TextColor = OxyColors.Black;
            xAxis.TitleColor = OxyColors.Black;
            xAxis.TicklineColor = OxyColors.Black;

            yAxis.AxislineColor = OxyColors.Black;
            yAxis.TextColor = OxyColors.Black;
            yAxis.TitleColor = OxyColors.Black;
            yAxis.TicklineColor = OxyColors.Black;

            _plotModel.TextColor = OxyColors.Black;
        }

        // Force OxyPlot to redraw with the new colors
        _plotModel.InvalidatePlot(false);
    }

    // Called whenever the page's ActualTheme changes (WinUI)
    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        Debug.WriteLine("Updating colors");

        // Re-apply the theming logic to update the axis colors
        if (_plotModel != null)
        {
            ApplyThemeToPlotModel();
        }
    }

    private void RadialGauge_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        Debug.WriteLine("Starting drag");
        if (DataContext is MotionViewModel viewModel)
        {
            viewModel.SelectedAxis.IsUserDraggingSlider = true;
        }
    }

    private void RadialGauge_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        Debug.WriteLine("Stopped drag");
        if (DataContext is MotionViewModel viewModel)
        {
            viewModel.SelectedAxis.IsUserDraggingSlider = false;

            // Force an update of the slider position after dragging ends
            viewModel.SelectedAxis.UpdateSliderValue(viewModel.SelectedAxis.SliderValue);
        }
    }

    private void Slider_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        Debug.WriteLine("Starting drag");
        if (DataContext is MotionViewModel viewModel)
        {
            viewModel.SelectedAxis.IsUserDraggingSlider = true;
        }
    }

    private void Slider_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        Debug.WriteLine("Stopped drag");
        if (DataContext is MotionViewModel viewModel)
        {
            viewModel.SelectedAxis.IsUserDraggingSlider = false;

            // Force an update of the slider position after dragging ends
            viewModel.SelectedAxis.UpdateSliderValue(viewModel.SelectedAxis.SliderValue);
        }
    }

    private void OpenJoystickPageButton_Click(object sender, RoutedEventArgs e)
    {
        var newWindow = new Window();
        var navRootPage = new NavigationRootPage();

        // e.g. match theme
        if (this.ActualTheme == ElementTheme.Dark)
            navRootPage.RequestedTheme = ElementTheme.Dark;
        else
            navRootPage.RequestedTheme = ElementTheme.Light;

        newWindow.Content = navRootPage;
        newWindow.SetWindowSize(400, 470);
        newWindow.Activate();

        // 1) Get the AppWindow:
        IntPtr hWnd = WindowNative.GetWindowHandle(newWindow);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

        // 2) Cast Presenter to OverlappedPresenter
        if (appWindow.Presenter is OverlappedPresenter overlappedPresenter)
        {
            // 3) Enable always-on-top
            overlappedPresenter.IsAlwaysOnTop = true;
        }

        // Then navigate
        if (this.DataContext is MotionViewModel vm)
        {
            var axes = vm.AllAxes;
            navRootPage.Navigate(typeof(JoystickWindow), axes);
        }
    }

}
