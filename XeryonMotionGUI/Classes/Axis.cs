using System;
using System.ComponentModel;

namespace XeryonMotionGUI.Classes
{
    public class Axis : INotifyPropertyChanged
    {
        public Axis()
        {
            _zone1Size = new Parameter(0, 1, 0.01, 0.01);
            _zone2Size = new Parameter(0, 1, 0.01, 0.1);
            _zone1Freq = new Parameter(0, 200000, 1000, 85000);
            _zone2Freq = new Parameter(0, 200000, 1000, 83000);
            _zone1Proportional = new Parameter(0, 200, 5, 90);
            _zone2Proportional = new Parameter(0, 200, 5, 45);
            _positionTolerance = new Parameter(0, 200, 2, 4);
            _speed = new Parameter(0, 400, 5, 200);
            _acceleration = new Parameter(0, 64400, 1000, 32000);
            _mass = new Parameter(0, 1500, 100, 0);
            _amplitudeControl = new Parameter(0, 1, 1, 1);
            _leftSoftLimit = new Parameter(-200, 0, 1, -100);
            _rightSoftLimit = new Parameter(0, 200, 1, 100);
            _phaseCorrection = new Parameter(0, 1, 1, 1);
            _errorLimit = new Parameter(0, 1000, 1, 50);
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Parameters using the Parameter class
        private Parameter _zone1Size;
        public Parameter Zone1Size
        {
            get => _zone1Size;
            set
            {
                if (_zone1Size != value)
                {
                    _zone1Size = value;
                    OnPropertyChanged(nameof(Zone1Size));
                }
            }
        }

        private Parameter _zone2Size;
        public Parameter Zone2Size
        {
            get => _zone2Size;
            set
            {
                if (_zone2Size != value)
                {
                    _zone2Size = value;
                    OnPropertyChanged(nameof(Zone2Size));
                }
            }
        }

        private Parameter _zone1Freq;
        public Parameter Zone1Freq
        {
            get => _zone1Freq;
            set
            {
                if (_zone1Freq != value)
                {
                    _zone1Freq = value;
                    OnPropertyChanged(nameof(Zone1Freq));
                }
            }
        }

        private Parameter _zone2Freq;
        public Parameter Zone2Freq
        {
            get => _zone2Freq;
            set
            {
                if (_zone2Freq != value)
                {
                    _zone2Freq = value;
                    OnPropertyChanged(nameof(Zone2Freq));
                }
            }
        }

        private Parameter _zone1Proportional;
        public Parameter Zone1Proportional
        {
            get => _zone1Proportional;
            set
            {
                if (_zone1Proportional != value)
                {
                    _zone1Proportional = value;
                    OnPropertyChanged(nameof(Zone1Proportional));
                }
            }
        }

        private Parameter _zone2Proportional;
        public Parameter Zone2Proportional
        {
            get => _zone2Proportional;
            set
            {
                if (_zone2Proportional != value)
                {
                    _zone2Proportional = value;
                    OnPropertyChanged(nameof(Zone2Proportional));
                }
            }
        }

        private Parameter _positionTolerance;
        public Parameter PositionTolerance
        {
            get => _positionTolerance;
            set
            {
                if (_positionTolerance != value)
                {
                    _positionTolerance = value;
                    OnPropertyChanged(nameof(PositionTolerance));
                }
            }
        }

        private Parameter _speed;
        public Parameter Speed
        {
            get => _speed;
            set
            {
                if (_speed != value)
                {
                    _speed = value;
                    OnPropertyChanged(nameof(Speed));
                }
            }
        }

        private Parameter _acceleration;
        public Parameter Acceleration
        {
            get => _acceleration;
            set
            {
                if (_acceleration != value)
                {
                    _acceleration = value;
                    OnPropertyChanged(nameof(Acceleration));
                }
            }
        }

        private Parameter _mass;
        public Parameter Mass
        {
            get => _mass;
            set
            {
                if (_mass != value)
                {
                    _mass = value;
                    OnPropertyChanged(nameof(Mass));
                }
            }
        }

        private Parameter _amplitudeControl;
        public Parameter AmplitudeControl
        {
            get => _amplitudeControl;
            set
            {
                if (_amplitudeControl != value)
                {
                    _amplitudeControl = value;
                    OnPropertyChanged(nameof(AmplitudeControl));
                }
            }
        }

        private Parameter _leftSoftLimit;
        public Parameter LeftSoftLimit
        {
            get => _leftSoftLimit;
            set
            {
                if (_leftSoftLimit != value)
                {
                    _leftSoftLimit = value;
                    OnPropertyChanged(nameof(LeftSoftLimit));
                }
            }
        }

        private Parameter _rightSoftLimit;
        public Parameter RightSoftLimit
        {
            get => _rightSoftLimit;
            set
            {
                if (_rightSoftLimit != value)
                {
                    _rightSoftLimit = value;
                    OnPropertyChanged(nameof(RightSoftLimit));
                }
            }
        }

        private Parameter _phaseCorrection;
        public Parameter PhaseCorrection
        {
            get => _phaseCorrection;
            set
            {
                if (_phaseCorrection != value)
                {
                    _phaseCorrection = value;
                    OnPropertyChanged(nameof(PhaseCorrection));
                }
            }
        }

        private Parameter _errorLimit;
        public Parameter ErrorLimit
        {
            get => _errorLimit;
            set
            {
                if (_errorLimit != value)
                {
                    _errorLimit = value;
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