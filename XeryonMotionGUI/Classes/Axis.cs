using System;
using System.ComponentModel;

namespace XeryonMotionGUI.Classes
{
    public class Axis : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // New Parameters for adjustment
        private double _Zone1Size;
        public double Zone1Size
        {
            get => _Zone1Size;
            set
            {
                if (_Zone1Size != value)
                {
                    _Zone1Size = value;
                    OnPropertyChanged(nameof(Zone1Size));
                }
            }
        }

        private double _Zone2Size;
        public double Zone2Size
        {
            get => _Zone2Size;
            set
            {
                if (_Zone2Size != value)
                {
                    _Zone2Size = value;
                    OnPropertyChanged(nameof(Zone2Size));
                }
            }
        }

        private double _Zone1Freq;
        public double Zone1Freq
        {
            get => _Zone1Freq;
            set
            {
                if (_Zone1Freq != value)
                {
                    _Zone1Freq = value;
                    OnPropertyChanged(nameof(Zone1Freq));
                }
            }
        }

        private double _Zone2Freq;
        public double Zone2Freq
        {
            get => _Zone2Freq;
            set
            {
                if (_Zone2Freq != value)
                {
                    _Zone2Freq = value;
                    OnPropertyChanged(nameof(Zone2Freq));
                }
            }
        }

        private double _Zone1Proportional;
        public double Zone1Proportional
        {
            get => _Zone1Proportional;
            set
            {
                if (_Zone1Proportional != value)
                {
                    _Zone1Proportional = value;
                    OnPropertyChanged(nameof(Zone1Proportional));
                }
            }
        }

        private double _Zone2Proportional;
        public double Zone2Proportional
        {
            get => _Zone2Proportional;
            set
            {
                if (_Zone2Proportional != value)
                {
                    _Zone2Proportional = value;
                    OnPropertyChanged(nameof(Zone2Proportional));
                }
            }
        }

        private double _PositionTolerance;
        public double PositionTolerance
        {
            get => _PositionTolerance;
            set
            {
                if (_PositionTolerance != value)
                {
                    _PositionTolerance = value;
                    OnPropertyChanged(nameof(PositionTolerance));
                }
            }
        }

        private double _Speed;
        public double Speed
        {
            get => _Speed;
            set
            {
                if (_Speed != value)
                {
                    _Speed = value;
                    OnPropertyChanged(nameof(Speed));
                }
            }
        }

        private double _Acceleration;
        public double Acceleration
        {
            get => _Acceleration;
            set
            {
                if (_Acceleration != value)
                {
                    _Acceleration = value;
                    OnPropertyChanged(nameof(Acceleration));
                }
            }
        }

        private double _Mass;
        public double Mass
        {
            get => _Mass;
            set
            {
                if (_Mass != value)
                {
                    _Mass = value;
                    OnPropertyChanged(nameof(Mass));
                }
            }
        }

        private double _AmplitudeControl;
        public double AmplitudeControl
        {
            get => _AmplitudeControl;
            set
            {
                if (_AmplitudeControl != value)
                {
                    _AmplitudeControl = value;
                    OnPropertyChanged(nameof(AmplitudeControl));
                }
            }
        }

        private double _LeftSoftLimit;
        public double LeftSoftLimit
        {
            get => _LeftSoftLimit;
            set
            {
                if (_LeftSoftLimit != value)
                {
                    _LeftSoftLimit = value;
                    OnPropertyChanged(nameof(LeftSoftLimit));
                }
            }
        }

        private double _RightSoftLimit;
        public double RightSoftLimit
        {
            get => _RightSoftLimit;
            set
            {
                if (_RightSoftLimit != value)
                {
                    _RightSoftLimit = value;
                    OnPropertyChanged(nameof(RightSoftLimit));
                }
            }
        }

        private double _PhaseCorrection;
        public double PhaseCorrection
        {
            get => _PhaseCorrection;
            set
            {
                if (_PhaseCorrection != value)
                {
                    _PhaseCorrection = value;
                    OnPropertyChanged(nameof(PhaseCorrection));
                }
            }
        }

        private double _ErrorLimit;
        public double ErrorLimit
        {
            get => _ErrorLimit;
            set
            {
                if (_ErrorLimit != value)
                {
                    _ErrorLimit = value;
                    OnPropertyChanged(nameof(ErrorLimit));
                }
            }
        }

        // Method to save the Axis parameters (example, you can extend this)
        public void SaveParameters()
        {
            // Code to save the axis parameters to your data store (e.g., file, database)
        }

        private string _Name;
        public string Name
        {
            get => _Name;
            set
            {
                if (_Name != value)
                {
                    _Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        private string _Type;
        public string Type
        {
            get => _Type;
            set
            {
                if (_Type != value)
                {
                    _Type = value;
                    OnPropertyChanged(nameof(Type));
                }
            }
        }

        private int _Resolution;
        public int Resolution
        {
            get => _Resolution;
            set
            {
                if (_Resolution != value)
                {
                    _Resolution = value;
                    OnPropertyChanged(nameof(Resolution));
                }
            }
        }

        private string _FriendlyName;
        public string FriendlyName
        {
            get => _FriendlyName;
            set
            {
                if (_FriendlyName != value)
                {
                    _FriendlyName = value;
                    OnPropertyChanged(nameof(FriendlyName));
                }
            }
        }

        private double _Range;
        public double Range
        {
            get => _Range;
            set
            {
                if (_Range != value)
                {
                    _Range = value;
                    OnPropertyChanged(nameof(Range));
                }
            }
        }

        private string _AxisLetter;
        public string AxisLetter
        {
            get => _AxisLetter;
            set
            {
                if (_AxisLetter != value)
                {
                    _AxisLetter = value;
                    OnPropertyChanged(nameof(AxisLetter));
                }
            }
        }

        private double _DesiredPosition;
        public double DesiredPosition
        {
            get => _DesiredPosition;
            set
            {
                if (_DesiredPosition != value)
                {
                    _DesiredPosition = value;
                    OnPropertyChanged(nameof(DesiredPosition));
                }
            }
        }

        private double _ActualPosition;
        public double ActualPosition
        {
            get => _ActualPosition;
            set
            {
                if (_ActualPosition != value)
                {
                    _ActualPosition = value;
                    OnPropertyChanged(nameof(ActualPosition));
                }
            }
        }

        private double _StepSize;
        public double StepSize
        {
            get => _StepSize;
            set
            {
                if (_StepSize != value)
                {
                    _StepSize = value;
                    OnPropertyChanged(nameof(StepSize));
                }
            }
        }

        private double _DPOS;
        public double DPOS
        {
            get => _DPOS;
            set
            {
                if (_DPOS != value)
                {
                    _DPOS = value;
                    OnPropertyChanged(nameof(DPOS));
                }
            }
        }

        private double _EPOS;
        public double EPOS
        {
            get => _EPOS;
            set
            {
                if (_EPOS != value)
                {
                    _EPOS = value;
                    OnPropertyChanged(nameof(EPOS));
                }
            }
        }

        private int _STAT;
        public int STAT
        {
            get => _STAT;
            set
            {
                if (_STAT != value)
                {
                    _STAT = value;
                    OnPropertyChanged(nameof(STAT));
                    UpdateStatusBits();
                }
            }
        }

        private int _TIME;
        public int TIME
        {
            get => _TIME;
            set
            {
                if (_TIME != value)
                {
                    _TIME = value;
                    OnPropertyChanged(nameof(TIME));
                }
            }
        }

        public string AxisTitle => AxisLetter != "None" ? $"Axis {AxisLetter}" : "Axis";

        // Status bits properties
        private bool _AmplifiersEnabled;
        public bool AmplifiersEnabled
        {
            get => _AmplifiersEnabled;
            private set
            {
                if (_AmplifiersEnabled != value)
                {
                    _AmplifiersEnabled = value;
                    OnPropertyChanged(nameof(AmplifiersEnabled));
                }
            }
        }

        private bool _EndStop;
        public bool EndStop
        {
            get => _EndStop;
            private set
            {
                if (_EndStop != value)
                {
                    _EndStop = value;
                    OnPropertyChanged(nameof(EndStop));
                }
            }
        }

        // Repeat the same pattern for all other status bits (omitted for brevity).
        // ...

        public void UpdateStatusBits()
        {
            AmplifiersEnabled = (STAT & (1 << 0)) != 0;
            EndStop = (STAT & (1 << 1)) != 0;
            // Repeat for all other status bits.
        }
    }
}