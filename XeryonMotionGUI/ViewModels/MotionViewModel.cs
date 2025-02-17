﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
        private Controller _selectedController;
        private readonly DispatcherQueue _dispatcherQueue;

        public ObservableCollection<Controller> RunningControllers => Controller.RunningControllers;

        // Property to bind the selected axis
        public Axis SelectedAxis
        {
            get => _selectedAxis;
            set
            {
                if (_selectedAxis != value)
                {
                    SetProperty(ref _selectedAxis, value);
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
                }
            }
        }

        public PlotModel PlotModel => SelectedAxis?.PlotModel;


        // Property to bind the selected controller
        public Controller SelectedController
        {
            get => _selectedController;
            set
            {
                if (SetProperty(ref _selectedController, value))
                {
                    // Notify changes for UI updates
                    OnPropertyChanged(nameof(SelectedController));
                    OnPropertyChanged(nameof(SelectedController.LoadingSettings));

                    // Update SelectedAxis based on the new controller or clear it if none
                    if (_selectedController?.Axes?.Count > 0)
                    {
                        SelectedAxis = _selectedController.Axes[0];
                    }
                    else
                    {
                        SelectedAxis = null;
                    }
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


        // Expose the commands from the selected axis
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
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Suppose your view model has a property called Controllers, 
        // each with an Axes collection. Combine them into a single list:
        public IReadOnlyList<Axis> AllAxes
        {
            get
            {
                // If RunningControllers is null or empty, return empty.
                if (RunningControllers == null || RunningControllers.Count == 0)
                    return Array.Empty<Axis>();

                var axisList = new List<Axis>();
                foreach (var controller in RunningControllers)
                {
                    // Each Controller has an Axes collection
                    axisList.AddRange(controller.Axes);
                }
                return axisList;
            }
        }



    }
}
