using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using XeryonMotionGUI.Classes;  // Make sure this points to your `Controller` and `Axis` classes

namespace XeryonMotionGUI.ViewModels
{
    public partial class MotionViewModel : ObservableObject
    {
        // Bind to the RunningControllers collection (from Controller class)
        public ObservableCollection<Controller> RunningControllers => Controller.RunningControllers;

        private Controller _selectedController;
        public Controller SelectedController
        {
            get => _selectedController;
            set
            {
                if (_selectedController != value)
                {
                    _selectedController = value;
                    OnPropertyChanged(nameof(SelectedController));  // Notify that the property has changed
                }
            }
        }
    }
}
