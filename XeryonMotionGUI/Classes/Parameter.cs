using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using XeryonMotionGUI.Helpers;
using RelayCommand = XeryonMotionGUI.Helpers.RelayCommand;

namespace XeryonMotionGUI.Classes
{
    public class Parameter : INotifyPropertyChanged
    {

        public Axis ParentAxis
        {
            get; set;
        }

        public string Category
        {
            get; set;
        }      
        public string Explanation
        {
            get; set;
        }

        private double? _min;
        private double? _max;



        public double? Min
        {
            get => _min;
            set
            {
                if (_min != value)
                {
                    _min = value;
                    OnPropertyChanged(nameof(Min));
                    OnPropertyChanged(nameof(EffectiveMin));
                }
            }
        }

        public double? Max
        {
            get => _max;
            set
            {
                if (_max != value)
                {
                    _max = value;
                    OnPropertyChanged(nameof(Max));
                    OnPropertyChanged(nameof(EffectiveMax));
                }
            }
        }

        // Provide read-only "effective" doubles for binding to Slider
        public double EffectiveMin => Min ?? 0.0;
        public double EffectiveMax => Max ?? 100.0;

        public double Increment
        {
            get;
        }

        public string Command
        {
            get;
        }

        public string Name
        {
            get;
        }

        private Controller _parentController;
        public Controller ParentController
        {
            get => _parentController;
            set => _parentController = value;
        }

        public double _value;
        public double Value
        {
            get => _value;
            set
            {
                double clampedValue = Math.Clamp(value, Min ?? double.MinValue, Max ?? double.MaxValue);
                if (_value != clampedValue)
                {
                    _value = clampedValue;
                    OnPropertyChanged(nameof(Value));

                    if (ParentController != null && !ParentController.LoadingSettings)
                    {
                        // Retrieve the Resolution from ParentAxis
                        int resolution = ParentAxis?.Resolution ?? 1;
                        ParentController.SendSetting(Command, _value, resolution, ParentAxis.AxisLetter, ParentAxis.Linear);
                    }
                }
            }
        }


        public ICommand IncrementCommand
        {
            get;
        }
        public ICommand DecrementCommand
        {
            get;
        }

        public Parameter(double? min, double? max, double increment, double defaultValue,
                        string name = "", string command = null,
                        string category = null, string explanation = null)
        {
            Min = min;
            Max = max;
            Increment = increment;
            Value = defaultValue;
            Name = name;
            Command = command;
            Category = category;
            Explanation = explanation;

            IncrementCommand = new RelayCommand(_ => IncrementValue());
            DecrementCommand = new RelayCommand(_ => DecrementValue());
           
        }

        public void DecrementValue()
        {
            Value -= Increment;
        }

        public void IncrementValue()
        {
            Value += Increment;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
