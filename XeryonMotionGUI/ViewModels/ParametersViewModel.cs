using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using XeryonMotionGUI.Classes;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using static XeryonMotionGUI.Views.ParametersPage;

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
                    OnPropertyChanged(nameof(SelectedController));
                    SelectedAxis = null; // clear selected axis when controller changes
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
                    OnPropertyChanged(nameof(SelectedAxis));
                    UpdateGroupedParameters();
                }
            }
        }

        private ObservableCollection<ParameterGroup> _groupedParameters;
        public ObservableCollection<ParameterGroup> GroupedParameters
        {
            get => _groupedParameters;
            set => SetProperty(ref _groupedParameters, value);
        }

        public ICommand SaveParametersCommand
        {
            get;
        }
        public ICommand IncrementCommand
        {
            get;
        }
        public ICommand DecrementCommand
        {
            get;
        }

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

                if (SelectedController.Axes?.Any() == true)
                {
                    SelectedAxis = SelectedController.Axes.First();
                }
            }
        }

        private void UpdateGroupedParameters()
        {
            if (SelectedAxis?.Parameters != null)
            {
                // Define your custom order here.
                var categoryOrder = new Dictionary<string, int>
        {
            { "Advanced tuning", 2 },
            { "Motion", 1 },
            { "Time outs and error handling", 3 },
            { "GPIO", 4 },
            { "Triggering", 5 }
            // Add any other categories and order as needed.
        };

                var groups = SelectedAxis.Parameters
                    .GroupBy(p => p.Category)
                    .OrderBy(g => categoryOrder.ContainsKey(g.Key) ? categoryOrder[g.Key] : int.MaxValue)
                    .Select(g => new ParameterGroup
                    {
                        Category = g.Key,
                        Parameters = new ObservableCollection<Parameter>(g)
                    });
                GroupedParameters = new ObservableCollection<ParameterGroup>(groups);
            }
            else
            {
                GroupedParameters = new ObservableCollection<ParameterGroup>();
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
                        parameter.IncrementValue();
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
                        parameter.DecrementValue();
                        OnPropertyChanged(parameterName);
                    }
                }
            }
        }

        private void SaveParameters()
        {
            if (SelectedAxis != null)
            {
                // Implement your logic to save the parameters for the selected axis.
            }
        }
    }
}
