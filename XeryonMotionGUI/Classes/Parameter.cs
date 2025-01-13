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
        private double? _min;
        private double? _max;
        private double _value;

        // Optional minimum value
        public double? Min
        {
            get => _min;
            set
            {
                if (_min != value)
                {
                    _min = value;
                    OnPropertyChanged(nameof(Min));

                    // Revalidate value within updated bounds
                    Value = Math.Clamp(_value, _min ?? double.MinValue, _max ?? double.MaxValue);
                }
            }
        }

        // Optional maximum value
        public double? Max
        {
            get => _max;
            set
            {
                if (_max != value)
                {
                    _max = value;
                    OnPropertyChanged(nameof(Max));

                    // Revalidate value within updated bounds
                    Value = Math.Clamp(_value, _min ?? double.MinValue, _max ?? double.MaxValue);
                }
            }
        }

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
                    ParentController?.SendSetting(Command, _value);

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

        public Parameter(double? min, double? max, double increment, double defaultValue, string name = "", string command = null)
        {
            Min = min;
            Max = max;
            Increment = increment;
            Value = defaultValue;
            Name = name;
            Command = command;

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
