using System.Collections.ObjectModel;
using System.Collections.Specialized; // For CollectionChanged
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using OxyPlot;
using XeryonMotionGUI.Classes;
using System.Linq;
using System.Collections.Generic;

namespace XeryonMotionGUI.ViewModels
{
    public partial class MotionViewModel : ObservableObject
    {
        private bool _isInfoBarOpen;
        private InfoBarSeverity _infoBarSeverity;
        private string _infoBarTitle;
        private string _infoBarMessage;

        private Axis _selectedAxis;
        private readonly DispatcherQueue _dispatcherQueue;

        public ObservableCollection<Controller> RunningControllers => Controller.RunningControllers;

        // Optional convenience property (useful if you want to bind something to this in the UI)
        public bool HasRunningControllers => RunningControllers != null && RunningControllers.Any();

        public Axis SelectedAxis
        {
            get => _selectedAxis;
            set
            {
                if (SetProperty(ref _selectedAxis, value))
                {
                    // Refresh command properties, PlotModel, etc.
                    OnPropertyChanged(nameof(PlotModel));
                    OnPropertyChanged(nameof(MoveNegativeCommand));
                    OnPropertyChanged(nameof(StepNegativeCommand));
                    OnPropertyChanged(nameof(HomeCommand));
                    OnPropertyChanged(nameof(StepPositiveCommand));
                    OnPropertyChanged(nameof(MovePositiveCommand));
                    OnPropertyChanged(nameof(StopCommand));
                    OnPropertyChanged(nameof(IndexCommand));
                    OnPropertyChanged(nameof(ResetCommand));
                    OnPropertyChanged(nameof(ResetEncoderCommand));
                    OnPropertyChanged(nameof(ScanPositiveCommand));
                    OnPropertyChanged(nameof(ScanNegativeCommand));
                    OnPropertyChanged(nameof(IndexMinusCommand));
                    OnPropertyChanged(nameof(IndexPlusCommand));

                    // If user picks an axis from another controller, keep them in sync
                    if (_selectedAxis?.ParentController != null
                        && _selectedAxis.ParentController != _selectedController)
                    {
                        SelectedController = _selectedAxis.ParentController;
                    }
                }
            }
        }

        public PlotModel PlotModel => SelectedAxis?.PlotModel;

        private Controller _selectedController;
        public Controller SelectedController
        {
            get => _selectedController;
            set
            {
                if (SetProperty(ref _selectedController, value))
                {
                    // Always pick first axis from the newly selected controller (if any),
                    // unless the currently selected axis belongs to it
                    if (_selectedController?.Axes?.Any() == true)
                    {
                        if (SelectedAxis == null || SelectedAxis.ParentController != _selectedController)
                        {
                            SelectedAxis = _selectedController.Axes[0];
                        }
                    }
                    else
                    {
                        SelectedAxis = null;
                    }

                    // Optional UI notifications
                    OnPropertyChanged(nameof(SelectedController));
                    OnPropertyChanged(nameof(SelectedController.LoadingSettings));
                }
            }
        }

        private bool _autoLogging = true;
        public bool AutoLogging
        {
            get => _autoLogging;
            set
            {
                if (_autoLogging != value)
                {
                    _autoLogging = value;
                    OnPropertyChanged();
                    Debug.WriteLine($"AutoLogging changed to: {_autoLogging}");
                }
            }
        }

        // Expose the commands from the SelectedAxis
        public ICommand MoveNegativeCommand => SelectedAxis?.MoveNegativeCommand;
        public ICommand StepNegativeCommand => SelectedAxis?.StepNegativeCommand;
        public ICommand HomeCommand => SelectedAxis?.HomeCommand;
        public ICommand StepPositiveCommand => SelectedAxis?.StepPositiveCommand;
        public ICommand MovePositiveCommand => SelectedAxis?.MovePositiveCommand;
        public ICommand StopCommand => SelectedAxis?.StopCommand;
        public ICommand IndexCommand => SelectedAxis?.IndexCommand;
        public ICommand ResetCommand => SelectedAxis?.ResetCommand;
        public ICommand ResetEncoderCommand => SelectedAxis?.ResetEncoderCommand;
        public ICommand ScanPositiveCommand => SelectedAxis?.ScanPositiveCommand;
        public ICommand ScanNegativeCommand => SelectedAxis?.ScanNegativeCommand;
        public ICommand IndexMinusCommand => SelectedAxis?.IndexMinusCommand;
        public ICommand IndexPlusCommand => SelectedAxis?.IndexPlusCommand;

        public MotionViewModel()
        {
            // Initialize the DispatcherQueue to handle UI updates from other threads
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // Listen for changes to RunningControllers so we can maintain the "first controller" selection
            if (RunningControllers != null)
            {
                RunningControllers.CollectionChanged += (s, e) =>
                {
                    OnPropertyChanged(nameof(HasRunningControllers));
                    ForceSelectFirstControllerAndAxis();
                };
            }

            ForceSelectFirstControllerAndAxis();

        }

        private void OnRunningControllersChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasRunningControllers));
            ForceSelectFirstControllerAndAxis();
        }

        private void ForceSelectFirstControllerAndAxis()
        {
            if (RunningControllers?.Any() == true)
            {
                SelectedController = RunningControllers.First();
                // The SelectedController setter automatically picks its first axis
            }
            else
            {
                SelectedController = null;
                SelectedAxis = null;
            }
        }

        // If you need this for some UI binding:
        public IReadOnlyList<Axis> AllAxes
        {
            get
            {
                if (RunningControllers == null || RunningControllers.Count == 0)
                    return System.Array.Empty<Axis>();

                var axisList = new List<Axis>();
                foreach (var controller in RunningControllers)
                {
                    // Each Controller has an Axes collection
                    axisList.AddRange(controller.Axes);
                }
                return axisList;
            }
        }

        // InfoBar & logging or other logic

        // For INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
