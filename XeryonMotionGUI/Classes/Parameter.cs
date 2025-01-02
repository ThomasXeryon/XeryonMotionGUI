using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeryonMotionGUI.Classes;
public class Parameter : INotifyPropertyChanged
{
    private double _value;
    private double _min;
    private double _max;
    private double _increment;

    public event PropertyChangedEventHandler PropertyChanged;

    public double Value
    {
        get => _value;
        set
        {
            if (_value != value && value >= Min && value <= Max)
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    public double Min
    {
        get => _min;
        set
        {
            if (_min != value)
            {
                _min = value;
                OnPropertyChanged(nameof(Min));
            }
        }
    }

    public double Max
    {
        get => _max;
        set
        {
            if (_max != value)
            {
                _max = value;
                OnPropertyChanged(nameof(Max));
            }
        }
    }

    public double Increment
    {
        get => _increment;
        set
        {
            if (_increment != value)
            {
                _increment = value;
                OnPropertyChanged(nameof(Increment));
            }
        }
    }

    public void IncrementValue()
    {
        if (_value + _increment <= _max)
        {
            Value += _increment;
        }
    }

    public void DecrementValue()
    {
        if (_value - _increment >= _min)
        {
            Value -= _increment;
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public Parameter(double min, double max, double increment, double initialValue)
    {
        _min = min;
        _max = max;
        _increment = increment;
        _value = initialValue;
    }
}