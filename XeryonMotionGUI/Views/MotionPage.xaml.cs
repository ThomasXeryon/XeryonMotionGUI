using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using XeryonMotionGUI.Classes;
using Microsoft.UI.Xaml.Controls.Primitives; // For RangeBaseValueChangedEventArgs
using XeryonMotionGUI.ViewModels;
using System.Diagnostics;

namespace XeryonMotionGUI.Views;

public sealed partial class MotionPage : Page
{
    public ObservableCollection<Controller> RunningControllers => Controller.RunningControllers;

    public MotionPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        this.DataContext = new MotionViewModel();  // Set DataContext here
    }

    private void PositionSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var viewModel = (MotionViewModel)this.DataContext;

        if (viewModel.SelectedAxis != null)
        {
            viewModel.SelectedAxis.ParentController.SendCommand($"DPOS={(int)(e.NewValue*1000000/ viewModel.SelectedAxis.Resolution)}");
        }
    }

    private void TextBox_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var viewModel = (MotionViewModel)this.DataContext;
        viewModel.SelectedAxis.MaxSpeed = 0;
    }
}
