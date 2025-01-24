using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Microsoft.UI.Dispatching;
using System.Windows.Input;
using XeryonMotionGUI.Helpers;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace XeryonMotionGUI.Classes
{
    public class Axis : INotifyPropertyChanged
    {
        public ObservableCollection<InfoBarMessage> InfoBarMessages { get; set; } = new ObservableCollection<InfoBarMessage>();

        public Axis(Controller controller, string axisType, string axisLetter)
        {
            ParentController = controller;
            AxisType = axisType;
            AxisLetter = axisLetter;

            InitializeParameters(axisType);

            // Initialize commands
            MoveNegativeCommand = new RelayCommand(MoveNegative);
            StepNegativeCommand = new RelayCommand(StepNegative);
            HomeCommand = new RelayCommand(Home);
            StepPositiveCommand = new RelayCommand(StepPositive);
            MovePositiveCommand = new RelayCommand(MovePositive);
            StopCommand = new RelayCommand(Stop);
            ResetCommand = new RelayCommand(async () => await ResetAsync());
            ResetEncoderCommand = new RelayCommand(async () => await ResetEncoderAsync());

            IndexCommand = new RelayCommand(Index);
            ScanPositiveCommand = new RelayCommand(ScanPositive);
            ScanNegativeCommand = new RelayCommand(ScanNegative);
            IndexMinusCommand = new RelayCommand(IndexMinus);
            IndexPlusCommand = new RelayCommand(IndexPlus);

            //Parameters = ParameterFactory.CreateParameters(ParentController.Type, axisType);

            // Assign the ParentController to each Parameter
            foreach (var parameter in Parameters)
            {
                parameter.ParentAxis = this; // Set the current axis as the parent
                parameter.ParentController = ParentController;
            }
        }

        public bool IsResetEnabled => !SuppressEncoderError;


        // Collection of parameters
        public ObservableCollection<Parameter> Parameters { get; set; } = new();

        // Dynamically initializes parameters
        private void InitializeParameters(string axisType)
        {
            Parameters.Clear();

            var newParameters = ParameterFactory.CreateParameters(ParentController.Type, axisType);

            foreach (var parameter in newParameters)
            {
                Parameters.Add(parameter);
            }

            OnPropertyChanged(nameof(Parameters));
        }

        // Add a parameter dynamically
        public void AddParameter(Parameter parameter)
        {
            if (Parameters.All(p => p.Name != parameter.Name))
            {
                Parameters.Add(parameter);
                OnPropertyChanged(nameof(Parameters));
            }
        }

        // Update an existing parameter
        public void UpdateParameter(string name, Action<Parameter> updateAction)
        {
            var parameter = Parameters.FirstOrDefault(p => p.Name == name);
            if (parameter != null)
            {
                updateAction(parameter);
                OnPropertyChanged(nameof(Parameters));
            }
        }

        // Get a specific parameter
        public Parameter GetParameter(string name)
        {
            return Parameters.FirstOrDefault(p => p.Name == name);
        }

        // Commands
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
        public ICommand ResetEncoderCommand
        {
            get;
        }
        public ICommand IndexCommand
        {
            get;
        }
        public ICommand ScanNegativeCommand
        {
            get;
        }
        public ICommand ScanPositiveCommand
        {
            get;
        }

        public ICommand IndexMinusCommand
        {
            get;
        }
        public ICommand IndexPlusCommand
        {
            get;
        }


        // Event for property changes
        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherQueue _dispatcherQueue;

        public void SetDispatcherQueue(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
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

        // Controller reference
        public Controller ParentController
        {
            get; set;
        }

        // Other properties (Name, Type, etc.)
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

        public string AxisType
        {
            get; set;
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
            if (!MotorOn)
            {
                SPEED = 0;
            }
            else
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
        }

        private bool _suppressEncoderError;
        public bool SuppressEncoderError
        {
            get => _suppressEncoderError;
            set
            {
                if (_suppressEncoderError != value)
                {
                    _suppressEncoderError = value;
                    OnPropertyChanged(nameof(SuppressEncoderError));
                    OnPropertyChanged(nameof(IsResetEnabled)); // Notify IsResetEnabled change
                }
            }
        }



        public string AxisTitle => AxisLetter != "None" ? $"Axis {AxisLetter}" : "Axis";

        // Status bits properties (Updated)
        private bool _AmplifiersEnabled;
        public bool AmplifiersEnabled
        {
            get => _AmplifiersEnabled; private set
            {
                if (_AmplifiersEnabled != value) { _AmplifiersEnabled = value; OnPropertyChanged(nameof(AmplifiersEnabled)); }
            }
        }

        private bool _EndStop;
        public bool EndStop
        {
            get => _EndStop; private set
            {
                if (_EndStop != value) { _EndStop = value; OnPropertyChanged(nameof(EndStop)); }
            }
        }

        private bool _ThermalProtection1;
        public bool ThermalProtection1
        {
            get => _ThermalProtection1; private set
            {
                if (_ThermalProtection1 != value) { _ThermalProtection1 = value; OnPropertyChanged(nameof(ThermalProtection1)); }
            }
        }

        private bool _ThermalProtection2;
        public bool ThermalProtection2
        {
            get => _ThermalProtection2; private set
            {
                if (_ThermalProtection2 != value) { _ThermalProtection2 = value; OnPropertyChanged(nameof(ThermalProtection2)); }
            }
        }

        private bool _ForceZero;
        public bool ForceZero
        {
            get => _ForceZero; private set
            {
                if (_ForceZero != value) { _ForceZero = value; OnPropertyChanged(nameof(ForceZero)); }
            }
        }

        private bool _MotorOn;
        public bool MotorOn
        {
            get => _MotorOn; private set
            {
                if (_MotorOn != value) { bool wasMotorOn = _MotorOn; _MotorOn = value; OnPropertyChanged(nameof(MotorOn)); }
            }
        }

        private bool _ClosedLoop;
        public bool ClosedLoop
        {
            get => _ClosedLoop; private set
            {
                if (_ClosedLoop != value) { _ClosedLoop = value; OnPropertyChanged(nameof(ClosedLoop)); }
            }
        }

        private bool _EncoderAtIndex;
        public bool EncoderAtIndex
        {
            get => _EncoderAtIndex; private set
            {
                if (_EncoderAtIndex != value) { _EncoderAtIndex = value; OnPropertyChanged(nameof(EncoderAtIndex)); }
            }
        }

        private bool _EncoderValid;
        public bool EncoderValid
        {
            get => _EncoderValid; private set
            {
                if (_EncoderValid != value) { _EncoderValid = value; OnPropertyChanged(nameof(EncoderValid)); }
            }
        }

        private bool _SearchingIndex;
        public bool SearchingIndex
        {
            get => _SearchingIndex; private set
            {
                if (_SearchingIndex != value) { _SearchingIndex = value; OnPropertyChanged(nameof(SearchingIndex)); }
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
                    if (!_PositionReached && value)
                    {
                        if (_commandSentTime != default)
                        {
                            CommandToPositionReachedDelay = DateTime.Now - _commandSentTime;
                        }

                        if (_positionReachedLastFalseTime != default)
                            PositionReachedElapsedTime = DateTime.Now - _positionReachedLastFalseTime;

                        SPEED = 0;
                    }
                    else if (_PositionReached && !value)
                    {
                        _positionReachedLastFalseTime = DateTime.Now;
                    }

                    _PositionReached = value;
                    OnPropertyChanged(nameof(PositionReached));
                }
            }
        }


        private DateTime _positionReachedLastFalseTime;
        private TimeSpan _positionReachedElapsedTime;

        public TimeSpan PositionReachedElapsedTime
        {
            get => _positionReachedElapsedTime;
            private set
            {
                if (_positionReachedElapsedTime != value)
                {
                    _positionReachedElapsedTime = value;
                    OnPropertyChanged(nameof(PositionReachedElapsedTime));
                    OnPropertyChanged(nameof(PositionReachedElapsedMilliseconds)); // Notify for milliseconds
                }
            }
        }

        private DateTime _commandSentTime;
        private TimeSpan _commandToPositionReachedDelay;
        public TimeSpan CommandToPositionReachedDelay
        {
            get => _commandToPositionReachedDelay;
            private set
            {
                if (_commandToPositionReachedDelay != value)
                {
                    _commandToPositionReachedDelay = value;
                    OnPropertyChanged(nameof(CommandToPositionReachedDelay));
                    OnPropertyChanged(nameof(CommandToPositionReachedMilliseconds));
                }
            }
        }

        public double CommandToPositionReachedMilliseconds => CommandToPositionReachedDelay.TotalMilliseconds;

        public double PositionReachedElapsedMilliseconds
        {
            get => PositionReachedElapsedTime.TotalMilliseconds; // Return total time in milliseconds
        }

        private bool _ErrorCompensation;
        public bool ErrorCompensation
        {
            get => _ErrorCompensation; private set
            {
                if (_ErrorCompensation != value) { _ErrorCompensation = value; OnPropertyChanged(nameof(ErrorCompensation)); }
            }
        }

        private bool _EncoderError;
        public bool EncoderError
        {
            get => _EncoderError;
            private set
            {
                if (_suppressEncoderError)
                {
                    Debug.WriteLine("EncoderError update suppressed.");
                    return; // Ignore updates during suppression
                }

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
            get => _Scanning; private set
            {
                if (_Scanning != value) { _Scanning = value; OnPropertyChanged(nameof(Scanning)); }
            }
        }

        private bool _leftEndStop;
        public bool LeftEndStop
        {
            get => _leftEndStop;
            set
            {
                if (_leftEndStop != value)
                {
                    _leftEndStop = value;
                    OnPropertyChanged(nameof(LeftEndStop));
                    OnPropertyChanged(nameof(SliderBackground)); // Notify SliderBackground update
                }
            }
        }

        private bool _rightEndStop;
        public bool RightEndStop
        {
            get => _rightEndStop;
            set
            {
                if (_rightEndStop != value)
                {
                    _rightEndStop = value;
                    OnPropertyChanged(nameof(RightEndStop));
                    OnPropertyChanged(nameof(SliderBackground)); // Notify SliderBackground update
                }
            }
        }

        private bool _ErrorLimit;
        public bool ErrorLimitBit
        {
            get => _ErrorLimit; set
            {
                if (_ErrorLimit != value) { _ErrorLimit = value; OnPropertyChanged(nameof(ErrorLimitBit)); }
            }
        }

        private bool _SearchingOptimalFrequency;
        public bool SearchingOptimalFrequency
        {
            get => _SearchingOptimalFrequency; set
            {
                if (_SearchingOptimalFrequency != value) { _SearchingOptimalFrequency = value; OnPropertyChanged(nameof(SearchingOptimalFrequency)); }
            }
        }

        private bool _SafetyTimeoutTriggered;
        public bool SafetyTimeoutTriggered
        {
            get => _SafetyTimeoutTriggered; set
            {
                if (_SafetyTimeoutTriggered != value) { _SafetyTimeoutTriggered = value; OnPropertyChanged(nameof(SafetyTimeoutTriggered)); }
            }
        }

        private bool _EtherCATAcknowledge;
        public bool EtherCATAcknowledge
        {
            get => _EtherCATAcknowledge; set
            {
                if (_EtherCATAcknowledge != value) { _EtherCATAcknowledge = value; OnPropertyChanged(nameof(EtherCATAcknowledge)); }
            }
        }

        private bool _EmergencyStop;
        public bool EmergencyStop
        {
            get => _EmergencyStop; set
            {
                if (_EmergencyStop != value) { _EmergencyStop = value; OnPropertyChanged(nameof(EmergencyStop)); }
            }
        }

        private bool _PositionFail;
        public bool PositionFail
        {
            get => _PositionFail; set
            {
                if (_PositionFail != value) { _PositionFail = value; OnPropertyChanged(nameof(PositionFail)); }
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
            UpdateInfoBar();
        }

        public void UpdateInfoBar()
        {
            if (_dispatcherQueue == null)
            {
                throw new InvalidOperationException("DispatcherQueue is not set. Please call SetDispatcherQueue before updating InfoBarMessages.");
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                InfoBarMessages.Clear();

                if (ThermalProtection1 || ThermalProtection2)
                {
                    InfoBarMessages.Add(new InfoBarMessage
                    {
                        Severity = InfoBarSeverity.Error,
                        Title = "Thermal Protection Triggered",
                        Message = "Thermal Protection is active. Check for short circuit or overcurrent."
                    });
                }

                if (ErrorLimitBit)
                {
                    InfoBarMessages.Add(new InfoBarMessage
                    {
                        Severity = InfoBarSeverity.Warning,
                        Title = "Error Limit Exceeded",
                        Message = "An error limit has been reached. Immediate attention is required."
                    });
                }

                if (EncoderError)
                {
                    InfoBarMessages.Add(new InfoBarMessage
                    {
                        Severity = InfoBarSeverity.Error,
                        Title = "Encoder Error Detected",
                        Message = "An encoder error has been detected. Immediate attention is required."
                    });
                }

                if (SafetyTimeoutTriggered)
                {
                    InfoBarMessages.Add(new InfoBarMessage
                    {
                        Severity = InfoBarSeverity.Warning,
                        Title = "Safety Timeout Triggered",
                        Message = "Motor was on longer than allowed (TOU2)."
                    });
                }

                if (PositionFail)
                {
                    InfoBarMessages.Add(new InfoBarMessage
                    {
                        Severity = InfoBarSeverity.Error,
                        Title = "Position Fail Triggered",
                        Message = "Failed to position within set timeframe (TOU3)."
                    });
                }
            });
        }


        public Brush SliderBackground
        {
            get
            {
                if (LeftEndStop || RightEndStop)
                {
                    return new SolidColorBrush(Colors.Red);
                }
                var accentColor = (Color)Application.Current.Resources["SystemAccentColorLight1"];
        return new SolidColorBrush(accentColor);
            }
        }

        public  void MoveNegative()
        {
            ParentController.SendCommand("MOVE=-1");
        }

        public void StepNegative()
        {
            _commandSentTime = DateTime.Now;
            ParentController.SendCommand($"STEP={Math.Floor(-StepSize)}");
        }

        public void Home()
        {
            ParentController.SendCommand("HOME");
            DPOS= 0;
        }

        public void StepPositive()
        {
            _commandSentTime = DateTime.Now;
            ParentController.SendCommand($"STEP={Math.Floor(StepSize)}");
        }

        public void MovePositive()
        {
            ParentController.SendCommand("MOVE=1");
        }

        public void Stop()
        {
            ParentController.SendCommand("STOP");
        }

        public void Index()
        {
            ParentController.SendCommand("INDX=0");
        }

        public void ScanPositive()
        {
            ParentController.SendCommand("SCAN=1");
        }

        public void ScanNegative()
        {
            ParentController.SendCommand("SCAN=-1");
        }

        public void IndexPlus()
        {
            ParentController.SendCommand("INDX=1");
        }

        public void IndexMinus()
        {
            ParentController.SendCommand("INDX=0");
        }


        private async Task ResetAsync()
        {   
            ParentController.LoadingSettings = true;
            ParentController.SendCommand("RSET");
            await Task.Delay(100);
            await ParentController.LoadParametersFromController();
            ParentController.LoadingSettings = false;

        }

        public async Task ResetEncoderAsync()
        {
            try
            {
                // Start suppression
                SuppressEncoderError = true;

                // Clear the EncoderError flag
                EncoderError = false;

                // Send the reset command
                ParentController.SendCommand("ENCR");

                // Retrieve the PollingInterval from the Parameters
                var pollingIntervalParameter = Parameters.FirstOrDefault(p => p.Command == "POLI");
                int pollingInterval = pollingIntervalParameter != null ? (int)pollingIntervalParameter.Value : 25; // Default to 25ms if not found

                // Calculate suppression duration: PollingInterval + 100ms
                int suppressionDuration = pollingInterval + 500;

                // Wait for the transient period to complete
                await Task.Delay(suppressionDuration);

                // Stop suppression
                SuppressEncoderError = false;

                Debug.WriteLine($"Encoder reset completed, suppression lifted after {suppressionDuration}ms.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during encoder reset: {ex.Message}");
                InfoBarMessages.Add(new InfoBarMessage
                {
                    Severity = InfoBarSeverity.Error,
                    Title = "Encoder Reset Error",
                    Message = "An error occurred while resetting the encoder."
                });
            }
        }
    }
}
