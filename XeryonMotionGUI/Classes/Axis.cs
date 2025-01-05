using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.UI.Core;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using System.Diagnostics;
using XeryonMotionGUI.Helpers;

namespace XeryonMotionGUI.Classes
{
    public class Axis : INotifyPropertyChanged
    {
        public Axis(Controller controller)
        {

            AxisLetter = AxisLetter;
            ParentController = controller;

            Parameters = new ObservableCollection<Parameter>
            {
                Zone1Size,
                Zone2Size,
                Zone1Freq,
                Zone2Freq,
                Zone1Proportional,
                Zone2Proportional,
                PositionTolerance,
                Speed,
                Acceleration,
                Mass,
                AmplitudeControl,
                LeftSoftLimit,
                RightSoftLimit,
                PhaseCorrection,
                ErrorLimit
            };

            MoveNegativeCommand = new Helpers.RelayCommand(MoveNegative);
            StepNegativeCommand = new Helpers.RelayCommand(StepNegative);
            HomeCommand = new Helpers.RelayCommand(Home);
            StepPositiveCommand = new Helpers.RelayCommand(StepPositive);
            MovePositiveCommand = new Helpers.RelayCommand(MovePositive);
            StopCommand = new Helpers.RelayCommand(Stop);
            ResetCommand = new Helpers.RelayCommand(Reset);
            IndexCommand = new Helpers.RelayCommand(Index);
        }

        public ICommand MoveNegativeCommand
        {
            get;
        }
        public ICommand StepNegativeCommand
        {
            get;
        }
        public ICommand HomeCommand
        {
            get;
        }
        public ICommand StepPositiveCommand
        {
            get;
        }
        public ICommand MovePositiveCommand
        {
            get;
        }
        public ICommand StopCommand
        {
            get;
        }
        public ICommand ResetCommand
        {
            get;
        }
        public ICommand IndexCommand
        {
            get;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherQueue _dispatcherQueue;

        public void SetDispatcherQueue(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            // Ensure PropertyChanged is called on UI thread using DispatcherQueue
            if (_dispatcherQueue != null)
            {
                if (_dispatcherQueue.HasThreadAccess)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }
                else
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                    });
                }
            }
        }

        // Collection of Parameters for easier iteration in UI
        public ObservableCollection<Parameter> Parameters
        {
            get;
        }

        // Parameters using the Parameter class with names set
        private Parameter _zone1Size = new(0, 1, 0.01, 0.01, "Zone 1 Size:");
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

        private Parameter _zone2Size = new(0, 1, 0.01, 0.1, "Zone 2 Size:");
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

        private Parameter _zone1Freq = new(0, 185000, 1000, 85000, "Zone 1 Frequency:");
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

        private Parameter _zone2Freq = new(0, 185000, 1000, 83000, "Zone 2 Frequency:");
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

        private Parameter _zone1Proportional = new(0, 200, 5, 90, "Zone 1 Proportional:");
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

        private Parameter _zone2Proportional = new(0, 200, 5, 45, "Zone 2 Proportional:");
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

        private Parameter _positionTolerance = new(0, 200, 2, 4, "Position Tolerance:");
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

        private Parameter _speed = new(0, 400, 5, 200, "Speed:");
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

        private Parameter _acceleration = new(0, 64400, 1000, 32000, "Acceleration:");
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

        private Parameter _mass = new(0, 1500, 100, 0, "Mass");
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

        private Parameter _amplitudeControl = new(0, 1, 1, 1, "Amplitude Control:");
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

        private Parameter _leftSoftLimit = new(-200, 0, 1, -100, "Left Soft Limit:");
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

        private Parameter _rightSoftLimit = new(0, 200, 1, 100, "Right Soft Limit:");
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

        private Parameter _phaseCorrection = new(0, 1, 1, 1, "Phase Correction:");
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

        private Parameter _errorLimit = new(0, 1000, 1, 50, "Error Limit:");
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

        public Controller ParentController
        {
            get; set;
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

        private bool _Linear;
        public bool Linear
        {
            get => _Linear;
            set
            {
                if (_Linear != value)
                {
                    _Linear = value;
                    OnPropertyChanged(nameof(Linear));
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

        private double _PositiveRange;
        public double PositiveRange
        {
            get => _PositiveRange;
            set
            {
                if (_PositiveRange != value)
                {
                    _PositiveRange = value;
                    OnPropertyChanged(nameof(PositiveRange));
                }
            }
        }

        private double _NegativeRange;
        public double NegativeRange
        {
            get => _NegativeRange;
            set
            {
                if (_NegativeRange != value)
                {
                    _NegativeRange = value;
                    OnPropertyChanged(nameof(NegativeRange));
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

                    // Dynamically calculate PositiveRange and NegativeRange
                    var (positiveHalf, negativeHalf) = RangeHelper.GetRangeHalves(_Range);
                    PositiveRange = positiveHalf;
                    NegativeRange = negativeHalf;
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
                    _PreviousEPOS = _EPOS;
                    _EPOS = value;
                    OnPropertyChanged(nameof(EPOS));
                    CalculateSpeed();
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



        private double _PreviousEPOS;
        private DateTime _LastUpdateTime;

        private double _SPEED;
        public double SPEED
        {
            get => _SPEED;
            set
            {
                if (_SPEED != value)
                {
                    _SPEED = value;
                    OnPropertyChanged(nameof(SPEED));

                    if (_SPEED > MaxSpeed)
                    {
                        MaxSpeed = _SPEED;
                    }
                }
            }
        }

        private double _MaxSpeed;
        public double MaxSpeed
        {
            get => _MaxSpeed;
            set
            {
                if (_MaxSpeed != value)
                {
                    _MaxSpeed = value;
                    OnPropertyChanged(nameof(MaxSpeed));
                }
            }
        }

        private void CalculateSpeed()
        {
            DateTime currentTime = DateTime.Now;
            if (_LastUpdateTime != default)
            {
                double timeDelta = (currentTime - _LastUpdateTime).TotalSeconds; // Time in seconds
                if (timeDelta > 0)
                {
                    // Convert nanometers to millimeters before calculating speed
                    SPEED = Math.Abs((_EPOS - _PreviousEPOS) / 1_000_000) / timeDelta * Resolution; // Speed in mm/s
                }
            }

            _LastUpdateTime = currentTime;
        }

        public string AxisTitle => AxisLetter != "None" ? $"Axis {AxisLetter}" : "Axis";

        // Status bits properties (Updated)
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

        private bool _ThermalProtection1;
        public bool ThermalProtection1
        {
            get => _ThermalProtection1;
            private set
            {
                if (_ThermalProtection1 != value)
                {
                    _ThermalProtection1 = value;
                    OnPropertyChanged(nameof(ThermalProtection1));
                }
            }
        }

        private bool _ThermalProtection2;
        public bool ThermalProtection2
        {
            get => _ThermalProtection2;
            private set
            {
                if (_ThermalProtection2 != value)
                {
                    _ThermalProtection2 = value;
                    OnPropertyChanged(nameof(ThermalProtection2));
                }
            }
        }

        private bool _ForceZero;
        public bool ForceZero
        {
            get => _ForceZero;
            private set
            {
                if (_ForceZero != value)
                {
                    _ForceZero = value;
                    OnPropertyChanged(nameof(ForceZero));
                }
            }
        }

        private bool _MotorOn;
        public bool MotorOn
        {
            get => _MotorOn;
            private set
            {
                if (_MotorOn != value)
                {
                    _MotorOn = value;
                    OnPropertyChanged(nameof(MotorOn));
                }
            }
        }

        private bool _ClosedLoop;
        public bool ClosedLoop
        {
            get => _ClosedLoop;
            private set
            {
                if (_ClosedLoop != value)
                {
                    _ClosedLoop = value;
                    OnPropertyChanged(nameof(ClosedLoop));
                }
            }
        }

        private bool _EncoderAtIndex;
        public bool EncoderAtIndex
        {
            get => _EncoderAtIndex;
            private set
            {
                if (_EncoderAtIndex != value)
                {
                    _EncoderAtIndex = value;
                    OnPropertyChanged(nameof(EncoderAtIndex));
                }
            }
        }

        private bool _EncoderValid;
        public bool EncoderValid
        {
            get => _EncoderValid;
            private set
            {
                if (_EncoderValid != value)
                {
                    _EncoderValid = value;
                    OnPropertyChanged(nameof(EncoderValid));
                }
            }
        }

        private bool _SearchingIndex;
        public bool SearchingIndex
        {
            get => _SearchingIndex;
            private set
            {
                if (_SearchingIndex != value)
                {
                    _SearchingIndex = value;
                    OnPropertyChanged(nameof(SearchingIndex));
                }
            }
        }

        private bool _PositionReached;
        public bool PositionReached
        {
            get => _PositionReached;
            private set
            {
                if (_PositionReached != value)
                {
                    _PositionReached = value;
                    OnPropertyChanged(nameof(PositionReached));
                }
            }
        }

        private bool _ErrorCompensation;
        public bool ErrorCompensation
        {
            get => _ErrorCompensation;
            private set
            {
                if (_ErrorCompensation != value)
                {
                    _ErrorCompensation = value;
                    OnPropertyChanged(nameof(ErrorCompensation));
                }
            }
        }

        private bool _EncoderError;
        public bool EncoderError
        {
            get => _EncoderError;
            private set
            {
                if (_EncoderError != value)
                {
                    _EncoderError = value;
                    OnPropertyChanged(nameof(EncoderError));
                }
            }
        }

        private bool _Scanning;
        public bool Scanning
        {
            get => _Scanning;
            private set
            {
                if (_Scanning != value)
                {
                    _Scanning = value;
                    OnPropertyChanged(nameof(Scanning));
                }
            }
        }

        private bool _LeftEndStop;
        public bool LeftEndStop
        {
            get => _LeftEndStop;
            private set
            {
                if (_LeftEndStop != value)
                {
                    _LeftEndStop = value;
                    OnPropertyChanged(nameof(LeftEndStop));
                }
            }
        }

        private bool _RightEndStop;
        public bool RightEndStop
        {
            get => _RightEndStop;
            private set
            {
                if (_RightEndStop != value)
                {
                    _RightEndStop = value;
                    OnPropertyChanged(nameof(RightEndStop));
                }
            }
        }

        private bool _ErrorLimit;
        public bool ErrorLimitBit
        {
            get => _ErrorLimit;
            private set
            {
                if (_ErrorLimit != value)
                {
                    _ErrorLimit = value;
                    OnPropertyChanged(nameof(ErrorLimitBit));
                }
            }
        }

        private bool _SearchingOptimalFrequency;
        public bool SearchingOptimalFrequency
        {
            get => _SearchingOptimalFrequency;
            private set
            {
                if (_SearchingOptimalFrequency != value)
                {
                    _SearchingOptimalFrequency = value;
                    OnPropertyChanged(nameof(SearchingOptimalFrequency));
                }
            }
        }

        private bool _SafetyTimeoutTriggered;
        public bool SafetyTimeoutTriggered
        {
            get => _SafetyTimeoutTriggered;
            private set
            {
                if (_SafetyTimeoutTriggered != value)
                {
                    _SafetyTimeoutTriggered = value;
                    OnPropertyChanged(nameof(SafetyTimeoutTriggered));
                }
            }
        }

        private bool _EtherCATAcknowledge;
        public bool EtherCATAcknowledge
        {
            get => _EtherCATAcknowledge;
            private set
            {
                if (_EtherCATAcknowledge != value)
                {
                    _EtherCATAcknowledge = value;
                    OnPropertyChanged(nameof(EtherCATAcknowledge));
                }
            }
        }

        private bool _EmergencyStop;
        public bool EmergencyStop
        {
            get => _EmergencyStop;
            private set
            {
                if (_EmergencyStop != value)
                {
                    _EmergencyStop = value;
                    OnPropertyChanged(nameof(EmergencyStop));
                }
            }
        }

        private bool _PositionFail;
        public bool PositionFail
        {
            get => _PositionFail;
            private set
            {
                if (_PositionFail != value)
                {
                    _PositionFail = value;
                    OnPropertyChanged(nameof(PositionFail));
                }
            }
        }

        public void UpdateStatusBits()
        {
            // Update all the status bits accordingly
            AmplifiersEnabled = (STAT & (1 << 0)) != 0;
            EndStop = (STAT & (1 << 1)) != 0;
            ThermalProtection1 = (STAT & (1 << 2)) != 0;
            ThermalProtection2 = (STAT & (1 << 3)) != 0;
            ForceZero = (STAT & (1 << 4)) != 0;
            MotorOn = (STAT & (1 << 5)) != 0;
            ClosedLoop = (STAT & (1 << 6)) != 0;
            EncoderAtIndex = (STAT & (1 << 7)) != 0;
            EncoderValid = (STAT & (1 << 8)) != 0;
            SearchingIndex = (STAT & (1 << 9)) != 0;
            PositionReached = (STAT & (1 << 10)) != 0;
            ErrorCompensation = (STAT & (1 << 11)) != 0;
            EncoderError = (STAT & (1 << 12)) != 0;
            Scanning = (STAT & (1 << 13)) != 0;
            LeftEndStop = (STAT & (1 << 14)) != 0;
            RightEndStop = (STAT & (1 << 15)) != 0;
            ErrorLimitBit = (STAT & (1 << 16)) != 0;
            SearchingOptimalFrequency = (STAT & (1 << 17)) != 0;
            SafetyTimeoutTriggered = (STAT & (1 << 18)) != 0;
            EtherCATAcknowledge = (STAT & (1 << 19)) != 0;
            EmergencyStop = (STAT & (1 << 20)) != 0;
            PositionFail = (STAT & (1 << 21)) != 0;
        }

        private  void MoveNegative()
        {
            ParentController.SendCommand("MOVE=-1");
        }

        private   void StepNegative()
        {
            ParentController.SendCommand($"STEP={Math.Floor(-StepSize)}");
        }

        private  void Home()
        {
            ParentController.SendCommand("HOME");
            DPOS= 0;
        }

        private void StepPositive()
        {
            ParentController.SendCommand($"STEP={Math.Floor(StepSize)}");
        }

        private  void MovePositive()
        {
            ParentController.SendCommand("MOVE=1");
        }

        private   void Stop()
        {
            ParentController.SendCommand("STOP");
        }

        private  void Index()
        {
            ParentController.SendCommand("INDX=0");
        }

        private void Reset()
        {
            ParentController.SendCommand("RSET");
        }
    }
}
