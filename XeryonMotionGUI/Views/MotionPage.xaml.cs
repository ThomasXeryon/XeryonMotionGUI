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


}
