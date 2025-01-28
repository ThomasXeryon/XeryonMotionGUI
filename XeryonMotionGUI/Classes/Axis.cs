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
using OxyPlot.Series;
using OxyPlot;
using OxyPlot.Axes;
using System.Collections.Concurrent;

namespace XeryonMotionGUI.Classes
{
    public class Axis : INotifyPropertyChanged
    {
        #region Fields

        private PlotModel _plotModel;
        private LineSeries _positionSeries;
        private ConcurrentQueue<(double EPOS, DateTime Time)> _dataQueue = new ConcurrentQueue<(double, DateTime)>();
        private DispatcherTimer _updateTimer;
        private object _lock = new object();

        private double _minEpos = double.MaxValue;
        private double _maxEpos = double.MinValue;
        private DateTime _startTime = DateTime.MinValue;
        private bool _previousPositionReached = false;
        private bool _isLogging = false;
        private DateTime _endTime = DateTime.MinValue;

        private bool _positionReachedChanged = false;
        private double _currentTime = 0;


        // General collections
        public ObservableCollection<InfoBarMessage> InfoBarMessages { get; set; } = new ObservableCollection<InfoBarMessage>();

        // Dispatcher for thread-safe property change notifications
        private DispatcherQueue _dispatcherQueue;

        // Speed and position tracking
        private double _PreviousEPOS;
        private DateTime _LastUpdateTime;

        private DateTime _positionReachedLastFalseTime;
        private TimeSpan _positionReachedElapsedTime;

        private DateTime _commandSentTime;
        private TimeSpan _commandToPositionReachedDelay;

        #endregion

        #region Constructor

        public Axis(Controller controller, string axisType, string axisLetter)
        {
            ParentController = controller;
            AxisType = axisType;
            AxisLetter = axisLetter;

            InitializeParameters(axisType);
            this.SetDispatcherQueue(DispatcherQueue.GetForCurrentThread());


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

            // Assign the ParentController to each Parameter
            foreach (var parameter in Parameters)
            {
                parameter.ParentAxis = this;
                parameter.ParentController = ParentController;
            }
            InitializePlot();

            // Start the update timer
            /*_updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500) // Update every 50ms
            };
            _updateTimer.Tick += UpdatePlotFromQueue;
            _updateTimer.Start();
*/
            _previousPositionReached = PositionReached;
        }

        #endregion

        #region Parameter Management

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

        #endregion

        #region EPOS Update Method

        public void OnEPOSUpdate(double epos, double timeStamp)
        {
            if (!_isLogging)
                return;

            // Enqueue (EPOS, timeStamp) rather than using DateTime.Now
            _dataQueue.Enqueue((epos, DateTime.Now));
            // or if you prefer, store the double or the ticks. For instance:
            // _dataQueue.Enqueue((epos, timeStampAsDouble));

            // Update min/max logic, etc.
            if (epos < _minEpos) _minEpos = epos;
            if (epos > _maxEpos) _maxEpos = epos;
        }





        #endregion

        #region Plot Update Method

        private void UpdatePlotFromQueue(object sender, object e)
        {
            // Dequeue all data points from the queue
            List<(double EPOS, DateTime Time)> pointsToPlot = new List<(double, DateTime)>();
            while (_dataQueue.TryDequeue(out var point))
            {
                pointsToPlot.Add(point);
            }

            // Enqueue the UI update even if we got zero points
            _dispatcherQueue.TryEnqueue(() =>
            {
                // Plot new points (if any)
                foreach (var point in pointsToPlot)
                {
                    double relativeTime = (point.Time - _startTime).TotalSeconds;
                    _positionSeries.Points.Add(new DataPoint(relativeTime, point.EPOS));
                }

                // Always recalibrate axes
                AdjustAxesBasedOnData();

                // Refresh
                _plotModel.InvalidatePlot(true);
            });
        }



        private void AdjustAxesBasedOnData()
        {
            if (_positionSeries.Points.Count == 0)
                return;

            var xAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom) as LinearAxis;
            var yAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Left) as LinearAxis;
            if (xAxis == null || yAxis == null)
                return;

            double minTime = _positionSeries.Points.Min(p => p.X);
            double maxTime = _positionSeries.Points.Max(p => p.X);
            double minEpos = _positionSeries.Points.Min(p => p.Y);
            double maxEpos = _positionSeries.Points.Max(p => p.Y);

            // Force time to start at zero
            if (minTime < 0)
                minTime = 0;

            // If there's no range in time, give at least 1 second
            if (maxTime <= minTime)
                maxTime = minTime + 1.0;

            xAxis.Minimum = minTime;
            xAxis.Maximum = maxTime;

            double eposRange = maxEpos - minEpos;
            if (eposRange < 1e-6)
            {
                // If all EPOS values are nearly identical
                minEpos -= 0.5;
                maxEpos += 0.5;
            }
            else
            {
                // 5% padding
                double padding = eposRange * 0.05;
                minEpos -= padding;
                maxEpos += padding;
            }

            yAxis.Minimum = minEpos;
            yAxis.Maximum = maxEpos;
        }


        #region Plot Initialization

        private void InitializePlot()
        {
            _plotModel = new PlotModel { Title = "Axis Movement Over Time" };

            _positionSeries = new LineSeries
            {
                Title = "Position (mm)",
                // Turn off markers
                MarkerType = MarkerType.None,
                // Make line thinner
                StrokeThickness = 1.0,

                // If you want a smooth curve (optional)
                // InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline,

                Color = OxyColors.Blue
            };

            //_positionSeries.InterpolationAlgorithm = InterpolationAlgorithms.CatmullRomSpline;

            _plotModel.Series.Add(_positionSeries);

            _plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Time (s)"
            });

            _plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "EPOS (mm)"
            });
        }

        #endregion

        #region X-Axis Adjustment

        private void AdjustXAxis(double totalDuration)
        {
            // Define a minimum duration to prevent the axis from becoming too narrow
            double minDuration = 1.0; // You can adjust this value based on your needs

            if (totalDuration < minDuration)
            {
                totalDuration = minDuration;
            }

            // Get the current X-axis
            var xAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
            if (xAxis == null)
            {
                // If no X-axis exists, create one
                xAxis = new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "Time (s)",
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot
                };
                _plotModel.Axes.Add(xAxis);
            }

            // Update the X-axis range
            xAxis.Minimum = 0;
            xAxis.Maximum = totalDuration;
        }

        #endregion

        #region Y-Axis Adjustment

        private void AdjustYAxis()
        {
            double padding = 0.05; // 5% padding

            double range = _maxEpos - _minEpos;
            double paddedRange = range * (1 + 2 * padding); // Add padding on both sides
            double paddedMin = _minEpos - range * padding;
            double paddedMax = _maxEpos + range * padding;

            // Ensure that the range is at least a certain minimum
            double minRange = 100;
            if (paddedMax - paddedMin < minRange)
            {
                double center = (_maxEpos + _minEpos) / 2;
                paddedMin = center - minRange / 2;
                paddedMax = center + minRange / 2;
            }

            // Get the current Y-axis
            var yAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Left);
            if (yAxis == null)
            {
                // If no Y-axis exists, create one
                yAxis = new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "EPOS (mm)",
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot
                };
                _plotModel.Axes.Add(yAxis);
            }

            // Update the Y-axis range with padding
            yAxis.Minimum = paddedMin;
            yAxis.Maximum = paddedMax;
        }


        #endregion

        #region PlotModel Property
        public PlotModel PlotModel
        {
            get => _plotModel;
            //private set => SetProperty(ref _plotModel, value);
        }
        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

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

        #endregion

        #region Core Properties (Controller, Basic Axis Info, etc.)

        // Reference to the controller
        public Controller ParentController
        {
            get; set;
        }

        // Axis identification
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

        public string AxisTitle => AxisLetter != "None" ? $"Axis {AxisLetter}" : "Axis";

        #endregion

        #region Basic Movement & Position Properties

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

                    // Update the plot with the new EPOS and time
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

        #endregion

        #region Axis Configuration Properties

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

        #endregion

        #region Commands

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

        #endregion

        #region Status Bit & Error Handling Properties

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
                    bool wasMotorOn = _MotorOn;
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
                if (_PositionReached == value)
                    return; // No change, do nothing

                // PositionReached is changing
                if (_PositionReached && !value)
                {
                    // Transition from true → false (negative flank): movement started
                    _positionReachedLastFalseTime = DateTime.Now;

                    // (Optional) Reset the plot so each new movement starts fresh
                    ResetPlot();

                    // Start logging so new points go onto the graph
                    _isLogging = true;
                }
                else if (!_PositionReached && value)
                {
                    // Transition from false → true (positive flank): movement ended
                    if (_commandSentTime != default)
                        CommandToPositionReachedDelay = DateTime.Now - _commandSentTime;

                    if (_positionReachedLastFalseTime != default)
                        PositionReachedElapsedTime = DateTime.Now - _positionReachedLastFalseTime;

                    // Movement ended, speed is zero, and we stop logging
                    UpdatePlotFromQueue(null, null);
                    SPEED = 0;
                    _isLogging = false;
                }

                // Update the underlying field and raise change notification
                _PositionReached = value;
                OnPropertyChanged(nameof(PositionReached));
            }
        }


        public TimeSpan PositionReachedElapsedTime
        {
            get => _positionReachedElapsedTime;
            private set
            {
                if (_positionReachedElapsedTime != value)
                {
                    _positionReachedElapsedTime = value;
                    OnPropertyChanged(nameof(PositionReachedElapsedTime));
                    OnPropertyChanged(nameof(PositionReachedElapsedMilliseconds));
                }
            }
        }

        public double PositionReachedElapsedMilliseconds => PositionReachedElapsedTime.TotalMilliseconds;

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
            get => _ErrorLimit;
            set
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
            set
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
            set
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
            set
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
            set
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
            set
            {
                if (_PositionFail != value)
                {
                    _PositionFail = value;
                    OnPropertyChanged(nameof(PositionFail));
                }
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

        public bool IsResetEnabled => !SuppressEncoderError;

        #endregion

        #region UI Styling

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

        #endregion

        #region Status Bits & InfoBar Methods

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

        #endregion

        #region Movement & Command Execution

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

        public void MoveNegative()
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
            DPOS = 0;
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

        #endregion

        #region Reset & Encoder Methods

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

        #endregion

        #region Plot Reset Method

        private void ResetPlot()
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                // Reset the plot
                _positionSeries.Points.Clear();
                _minEpos = double.MaxValue;
                _maxEpos = double.MinValue;
                _startTime = DateTime.Now;
                _endTime = _startTime;
                _currentTime = 0;

                // Refresh the plot
                _plotModel.InvalidatePlot(true);
            });
        }

        #endregion
    }
}
        #endregion
