using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using XeryonMotionGUI.Classes;
using XeryonMotionGUI.ViewModels;

namespace XeryonMotionGUI.Views;

public sealed partial class MotionPage : Page
{
    public ObservableCollection<Controller> RunningControllers => Controller.RunningControllers;
    public MotionPage()
    {
        InitializeComponent();
    }

}
    
    