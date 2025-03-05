using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized; // For CollectionChanged
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using OxyPlot;
using XeryonMotionGUI.Classes;

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

        // This is the collection of currently running controllers (taken from the static property on Controller)
        public ObservableCollection<Controller> RunningControllers => Controller.RunningControllers;

        // Optional convenience property
        public bool HasRunningControllers => RunningControllers != null && RunningControllers.Any();

        private Controller _selectedController;
        public Controller SelectedController
        {
            get => _selectedController;
            set
            {
                if (SetProperty(ref _selectedController, value))
                {
                    // If the newly selected Controller has axes, pick the first one unless
                    // the currently selected axis already belongs to it.
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

                    // Notify anything else bound to SelectedController or its properties
                    OnPropertyChanged(nameof(SelectedController));
                    OnPropertyChanged(nameof(SelectedController.LoadingSettings));
                }
            }
        }

        public Axis SelectedAxis
        {
            get => _selectedAxis;
            set
            {
                if (SetProperty(ref _selectedAxis, value))
                {
                    // Refresh any UI that depends on the axis
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

                    // If user picked an axis from another controller, sync the SelectedController property
                    if (_selectedAxis?.ParentController != null &&
                        _selectedAxis.ParentController != _selectedController)
                    {
                        SelectedController = _selectedAxis.ParentController;
                    }

                    // Also re-apply the chosen display mode if user changed axes
                    if (_selectedAxis != null)
                    {
                        UpdatePlotVisibility();
                    }
                }
            }
        }

        // Use this property in your XAML if you want to bind directly to the plot
        public PlotModel PlotModel => SelectedAxis?.PlotModel;

        // An ObservableCollection of strings that populate your ComboBox
        // (Both Speed and Position, Position Only, Speed Only)
        public ObservableCollection<string> DisplayModes
        {
            get; set;
        }

        private string _selectedDisplayMode;
        public string SelectedDisplayMode
        {
            get => _selectedDisplayMode;
            set
            {
                if (SetProperty(ref _selectedDisplayMode, value))
                {
                    // Whenever this changes, actually tell the Axis which mode we want
                    UpdatePlotVisibility();
                }
            }
        }

        // If your GUI uses a ToggleSwitch or something for auto/manual logging
        private bool _autoLogging = true;
        public bool AutoLogging
        {
            get => _autoLogging;
            set
            {
                if (SetProperty(ref _autoLogging, value))
                {
                    Debug.WriteLine($"AutoLogging changed to: {_autoLogging}");
                }
            }
        }

        // Map the Axis commands to top-level properties so the UI can bind:
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

        // Basic constructor
        public MotionViewModel()
        {
            // Initialize the ComboBox items
            DisplayModes = new ObservableCollection<string>
            {
                "Both Speed and Position",
                "Position Only",
                "Speed Only"
            };

            // Pick a default if you like
            _selectedDisplayMode = "Both Speed and Position";

            // Initialize the DispatcherQueue to handle UI updates from other threads
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // Listen for changes to the RunningControllers collection
            if (RunningControllers != null)
            {
                RunningControllers.CollectionChanged += OnRunningControllersChanged;
            }

            // Force an initial selection (if any exist)
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
            }
            else
            {
                SelectedController = null;
                SelectedAxis = null;
            }
        }

        // Called whenever SelectedDisplayMode or SelectedAxis changes
        private void UpdatePlotVisibility()
        {
            if (SelectedAxis == null) return;

            switch (SelectedDisplayMode)
            {
                case "Both Speed and Position":
                    SelectedAxis.PlotDisplayMode = Axis.PlotDisplayModeEnum.Both;
                    break;
                case "Position Only":
                    SelectedAxis.PlotDisplayMode = Axis.PlotDisplayModeEnum.PositionOnly;
                    break;
                case "Speed Only":
                    SelectedAxis.PlotDisplayMode = Axis.PlotDisplayModeEnum.SpeedOnly;
                    break;
                default:
                    // Fallback if somehow it's not recognized:
                    SelectedAxis.PlotDisplayMode = Axis.PlotDisplayModeEnum.Both;
                    break;
            }
        }

        // Just in case you need this for something else
        public IReadOnlyList<Axis> AllAxes
        {
            get
            {
                if (RunningControllers == null || RunningControllers.Count == 0)
                    return Array.Empty<Axis>();

                var axisList = new System.Collections.Generic.List<Axis>();
                foreach (var controller in RunningControllers)
                {
                    // Each Controller has an Axes collection
                    axisList.AddRange(controller.Axes);
                }
                return axisList;
            }
        }

        // Example InfoBar logic
        public bool IsInfoBarOpen
        {
            get => _isInfoBarOpen;
            set => SetProperty(ref _isInfoBarOpen, value);
        }
        public InfoBarSeverity InfoBarSeverity
        {
            get => _infoBarSeverity;
            set => SetProperty(ref _infoBarSeverity, value);
        }
        public string InfoBarTitle
        {
            get => _infoBarTitle;
            set => SetProperty(ref _infoBarTitle, value);
        }
        public string InfoBarMessage
        {
            get => _infoBarMessage;
            set => SetProperty(ref _infoBarMessage, value);
        }
    }
}
