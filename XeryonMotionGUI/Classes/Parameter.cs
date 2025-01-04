using System;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using XeryonMotionGUI.Helpers;


namespace XeryonMotionGUI.Classes
{
    public class Parameter : INotifyPropertyChanged
    {
        private double _value;

        public double Min
        {
            get;
        }
        public double Max
        {
            get;
        }
        public double Increment
        {
            get;
        }
        public string Name
        {
            get;
        }

        public double Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = Math.Clamp(value, Min, Max); // Ensure within bounds
                    OnPropertyChanged(nameof(Value));
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

        public Parameter(double min, double max, double increment, double defaultValue, string name = "")
        {
            Min = min;
            Max = max;
            Increment = increment;
            Value = defaultValue;
            Name = name;

            IncrementCommand = new Helpers.RelayCommand(_ => IncrementValue());
            DecrementCommand = new Helpers.RelayCommand(_ => DecrementValue());
        }

        public void DecrementValue()
        {
            Value = Math.Max(Min, Value - Increment);
        }

        public void IncrementValue()
        {
            Value = Math.Min(Max, Value + Increment);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
