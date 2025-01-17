using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using XeryonMotionGUI.Classes;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace XeryonMotionGUI.ViewModels
{
    public partial class ParametersViewModel : ObservableObject
    {
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
                    OnPropertyChanged(nameof(SelectedController)); // Notify property change
                    SelectedAxis = null; // Clear selected axis when controller changes
                }
            }
        }

        private Axis _selectedAxis;
        public Axis SelectedAxis
        {
            get => _selectedAxis;
            set
            {
                if (_selectedAxis != value)
                {
                    _selectedAxis = value;
                    OnPropertyChanged(nameof(SelectedAxis)); // Notify property change
                }
            }
        }

        public ICommand SaveParametersCommand { get; }
        public ICommand IncrementCommand { get; }
        public ICommand DecrementCommand { get; }

        private ObservableCollection<Parameter> _parameters;
        public ObservableCollection<Parameter> Parameters
        {
            get => _parameters;
            set
            {
                _parameters = value;
                OnPropertyChanged(nameof(Parameters));
            }
        }



        public ParametersViewModel()
        {
            SaveParametersCommand = new RelayCommand(SaveParameters);
            IncrementCommand = new RelayCommand<string>(IncrementParameter);
            DecrementCommand = new RelayCommand<string>(DecrementParameter);

            // Automatically select the first controller and its first axis (if available)
            if (RunningControllers?.Any() == true)
            {
                SelectedController = RunningControllers.First();

                // Select the first axis of the first controller if available
                if (SelectedController.Axes?.Any() == true)
                {
                    SelectedAxis = SelectedController.Axes.First();
                }
            }
        }

        private void IncrementParameter(string parameterName)
        {
            if (SelectedAxis != null)
            {
                var property = SelectedAxis.GetType().GetProperty(parameterName);
                if (property != null)
                {
                    var parameter = property.GetValue(SelectedAxis) as Parameter;
                    if (parameter != null)
                    {
                        parameter.IncrementValue(); // Call IncrementValue method on the Parameter class
                        OnPropertyChanged(parameterName);
                    }
                }
            }
        }

        private void DecrementParameter(string parameterName)
        {
            if (SelectedAxis != null)
            {
                var property = SelectedAxis.GetType().GetProperty(parameterName);
                if (property != null)
                {
                    var parameter = property.GetValue(SelectedAxis) as Parameter;
                    if (parameter != null)
                    {
                        parameter.DecrementValue(); // Call DecrementValue method on the Parameter class
                        OnPropertyChanged(parameterName);
                    }
                }
            }
        }

        private void SaveParameters()
        {
            if (SelectedAxis != null)
            {
                // Implement logic to save the parameters of selected axis
            }
        }
    }
}
