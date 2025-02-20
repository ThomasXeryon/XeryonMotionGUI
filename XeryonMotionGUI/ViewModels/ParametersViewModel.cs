using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using XeryonMotionGUI.Classes;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Collections.Generic;

namespace XeryonMotionGUI.ViewModels
{
    public partial class ParametersViewModel : ObservableObject
    {
        public ObservableCollection<Controller> RunningControllers => Controller.RunningControllers;

        // Expose a boolean to let the UI know if any controllers exist
        public bool HasRunningControllers => RunningControllers != null && RunningControllers.Any();

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

                    // If new controller is not null, pick its first axis (unless we already had an axis that belongs to it)
                    if (_selectedController?.Axes?.Any() == true)
                    {
                        // If current SelectedAxis doesn't belong to this controller, reset it
                        if (SelectedAxis == null || SelectedAxis.ParentController != _selectedController)
                        {
                            SelectedAxis = _selectedController.Axes.First();
                        }
                    }
                    else
                    {
                        SelectedAxis = null;
                    }
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

                    // If user picks an axis that belongs to a different controller, update SelectedController
                    if (_selectedAxis?.ParentController != null
                        && _selectedAxis.ParentController != _selectedController)
                    {
                        SelectedController = _selectedAxis.ParentController;
                    }
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

            // Subscribe to collection changes so we can keep the selection 
            // locked to the first controller (and its first axis)
            if (RunningControllers != null)
            {
                RunningControllers.CollectionChanged += (s, e) =>
                {
                    OnPropertyChanged(nameof(HasRunningControllers));
                    ForceSelectFirstControllerAndAxis();
                };
            }

            // Initial selection if we start with controllers
            ForceSelectFirstControllerAndAxis();
        }

        private void OnRunningControllersChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasRunningControllers));

            // If there's at least one controller, force selection to the first
            // Otherwise clear selection
            ForceSelectFirstControllerAndAxis();
        }

        private void ForceSelectFirstControllerAndAxis()
        {
            // If we have controllers, pick the first
            if (RunningControllers?.Any() == true)
            {
                SelectedController = RunningControllers.First();
            }
            else
            {
                SelectedController = null;
                SelectedAxis = null;
            }
        }

        private void UpdateGroupedParameters()
        {
            if (SelectedAxis?.Parameters != null)
            {
                // Example custom sorting order
                var categoryOrder = new Dictionary<string, int>
                {
                    { "Advanced tuning", 2 },
                    { "Motion", 1 },
                    { "Time outs and error handling", 3 },
                    { "GPIO", 4 },
                    { "Triggering", 5 }
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
                    parameter?.IncrementValue();
                    OnPropertyChanged(parameterName);
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
                    parameter?.DecrementValue();
                    OnPropertyChanged(parameterName);
                }
            }
        }

        private void SaveParameters()
        {
            if (SelectedAxis != null)
            {
                // Implement your logic to save parameters for the selected axis
            }
        }
    }
}
