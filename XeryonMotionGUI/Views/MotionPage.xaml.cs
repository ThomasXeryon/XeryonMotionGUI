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
using Windows.System;
using XeryonMotionGUI.Helpers;

namespace XeryonMotionGUI.Views;

public sealed partial class MotionPage : Page
{
    private bool _suppressSliderValueChanged = false;

    public ObservableCollection<Controller> RunningControllers => Controller.RunningControllers;

    public MotionPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        this.DataContext = new MotionViewModel();  // Set DataContext here

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
}
