﻿using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using XeryonMotionGUI.Classes;

namespace XeryonMotionGUI.ViewModels
{
    public partial class MotionViewModel : ObservableObject
    {
        private Axis _selectedAxis;
        private Controller _selectedController;

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

                    OnPropertyChanged(nameof(MoveNegativeCommand));
                    OnPropertyChanged(nameof(StepNegativeCommand));
                    OnPropertyChanged(nameof(HomeCommand));
                    OnPropertyChanged(nameof(StepPositiveCommand));
                    OnPropertyChanged(nameof(MovePositiveCommand));
                    OnPropertyChanged(nameof(StopCommand));
                }
            }
        }

        // Property to bind the selected controller
        public Controller SelectedController
        {
            get => _selectedController;
            set
            {
                if (SetProperty(ref _selectedController, value))
                {
                    // When a controller is selected, set the first axis to be selected
                    if (_selectedController?.Axes?.Count > 0)
                    {
                        SelectedAxis = _selectedController.Axes[0];
                    }
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

        public MotionViewModel()
        {
            // This is where you can initialize any collections or values needed
            // For instance, you might want to manually load running controllers into the RunningControllers collection
            // RunningControllers = new ObservableCollection<Controller>();
        }


    }
}
