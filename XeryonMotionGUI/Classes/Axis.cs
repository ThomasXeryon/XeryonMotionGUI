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
using System.Threading.Tasks;
using System.Diagnostics;
using OxyPlot.Series;
using OxyPlot;
using OxyPlot.Axes;
using System.Collections.Concurrent;
using OxyPlot.Legends;
using Microsoft.VisualBasic;
using System.Reflection.Metadata;
using Newtonsoft.Json.Linq;
using CommunityToolkit.WinUI;

namespace XeryonMotionGUI.Classes
{
    public class Axis : INotifyPropertyChanged
    {
        #region Fields
        private PlotModel _plotModel;
        private LineSeries _positionSeries;
        private ConcurrentQueue<(double EPOS, double SyncTime)> _dataQueue = new ConcurrentQueue<(double, double)>();
        private DispatcherTimer _updateTimer;
        private object _lock = new object();

        private double _minEpos = double.MaxValue;
        private double _maxEpos = double.MinValue;
        private DateTime _startTime = DateTime.MinValue;
        private bool _isLogging = false;
        private DateTime _endTime = DateTime.MinValue;
        private double _currentTime = 0;
        private double _startSyncTime = 0;
        // Fields for speed calculation
        private double _lastPosition = 0.0;        // store last EPOS value
        private double _lastTime = 0.0;            // store last time reading (in seconds)
        private bool _hasLastSample = false;
        private double _timeOffset = 0; // in seconds
        private int _prevTime = 0;



        public ObservableCollection<InfoBarMessage> InfoBarMessages { get; set; } = new ObservableCollection<InfoBarMessage>();
        private DispatcherQueue _dispatcherQueue;
        private double _PreviousEPOS;
        private DateTime _LastUpdateTime;
        private bool _suppressSliderUpdate = false;
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
            IndexCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(Index);
            ScanPositiveCommand = new RelayCommand(ScanPositive);
            ScanNegativeCommand = new RelayCommand(ScanNegative);
            IndexMinusCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(IndexMinus);
            IndexPlusCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(IndexPlus);

            if (Linear)
                _selectedUnit = Units.mm;   // default for linear axis
            else
                _selectedUnit = Units.deg;
            // Assign the ParentController to each Parameter
            foreach (var parameter in Parameters)
            {
                parameter.ParentAxis = this;
                parameter.ParentController = ParentController;
            }
            InitializePlot();
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

        #endregion

        #region EPOS Update Method

        public void OnEPOSUpdate(double epos, double dummy)
        {
            if (!_isLogging)
                return;

            double t;
            if (UseControllerTime)
            {
                // Add the accumulated offset to the current TIME value.
                double continuousTicks = _timeOffset + this.TIME;
                // Convert ticks to seconds.
                // If each tick is 1/10000th of a second, then:
                t = continuousTicks / 10000.0;
            }
            else
            {
                t = ParentController.GlobalTimeSeconds;
            }

            _dataQueue.Enqueue((epos, t));

            // Update min/max values as needed.
            if (epos < _minEpos) _minEpos = epos;
            if (epos > _maxEpos) _maxEpos = epos;


            // ----- Calculate speed (only if we have a prior sample) -----
            if (_hasLastSample)
            {
                double dt = t - _lastTime;
                if (dt > 1e-9)  // to avoid dividing by 0
                {
                    // raw difference in encoder counts
                    double rawEncDiff = epos - _lastPosition;

                    // If rotational, handle wrap-around
                    if (!Linear)
                    {
                        double fullRev = FullRevolutionEncoderUnits;
                        rawEncDiff = (rawEncDiff + fullRev / 2.0) % fullRev - (fullRev / 2.0);
                    }

                    // Convert rawEncDiff to physical distance or angle
                    double distanceOrAngle;
                    if (Linear)
                    {
                        // epos in counts => mm
                        // "Resolution" = nm per count
                        distanceOrAngle = rawEncDiff * (Resolution / 1_000_000.0); // mm
                    }
                    else
                    {
                        // epos in counts => deg
                        double degPerCount = 360.0 / FullRevolutionEncoderUnits;
                        distanceOrAngle = rawEncDiff * degPerCount; // deg
                    }

                    // speed = distanceOrAngle / dt
                    double speedValue = distanceOrAngle / dt;

                    SPEED = Math.Abs(speedValue);
                }
            }

            // ----- Store current sample as "last" for next iteration -----
            _lastTime = t;
            _lastPosition = epos;
            _hasLastSample = true;

            //  ...
            //  if you also do anything else with epos or logging
        }



        #endregion

        #region Plot Update Method

        private void UpdatePlotFromQueue(object sender, object e)
        {
            List<(double EPOS, double SyncTime)> pointsToPlot = new List<(double, double)>();
            while (_dataQueue.TryDequeue(out var point))
            {
                pointsToPlot.Add(point);
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var point in pointsToPlot)
                {
                    // Compute relative time: subtract the baseline.
                    double relativeTime = point.SyncTime - _startSyncTime;
                    _positionSeries.Points.Add(new DataPoint(relativeTime, point.EPOS));
                }

                AdjustAxesBasedOnData();
                _plotModel.InvalidatePlot(true);
            });
        }








        public ICommand StartManualLoggingCommand => new RelayCommand(() => StartManualLogging());
        public ICommand StopManualLoggingCommand => new RelayCommand(() => StopManualLogging());

        public void StartManualLogging()
        {
            // Here we simply set a flag that OnEPOSUpdate will check.
            // You might choose to log a debug message:
            Debug.WriteLine($"Manual logging started for Axis {AxisLetter}.");
            // Set your internal flag to true (or call a method that starts logging)
            _isLogging = true;
            ResetPlot();
            // Optionally, if you have a property for manual logging, you could set it here.
        }

        public void StopManualLogging()
        {
            Debug.WriteLine($"Manual logging stopped for Axis {AxisLetter}.");
            _isLogging = false;
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

            var xxAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
            var yyAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Left);

            if (xxAxis != null && yyAxis != null)
            {
                xxAxis.Reset();
                yyAxis.Reset();
            }
        }


        #region Plot Initialization

        private void InitializePlot()
        {
            // Create the PlotModel with a white background, black text
            _plotModel = new PlotModel
            {
                Title = "Axis Movement Over Time",
                Background = OxyColors.White,        // white chart area
                TextColor = OxyColors.Black,         // black text (titles, axis labels)
                PlotAreaBorderColor = OxyColors.Black
            };

            // Create the main series
            _positionSeries = new LineSeries
            {
                Title = "Position (mm)",
                MarkerType = MarkerType.Circle,
                MarkerSize = 2,
                MarkerStroke = OxyColors.Transparent,
                MarkerFill = OxyColor.Parse("#27b62d"),
                StrokeThickness = 1.5,
                Color = OxyColor.Parse("#27b62d"),
                LineStyle = LineStyle.Solid,
            };
            _plotModel.Series.Add(_positionSeries);

            // X Axis
            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Time (s)",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                AxislineColor = OxyColors.Black,
                TextColor = OxyColors.Black,
                TitleColor = OxyColors.Black,
                TicklineColor = OxyColors.Black
            };
            _plotModel.Axes.Add(xAxis);

            // Y Axis
            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Position (enc)",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                AxislineColor = OxyColors.Black,
                TextColor = OxyColors.Black,
                TitleColor = OxyColors.Black,
                TicklineColor = OxyColors.Black
            };
            _plotModel.Axes.Add(yAxis);

            _plotModel.IsLegendVisible = true;

            // Optional: detect axis changes for marker toggling
            xAxis.AxisChanged += OnAxisChanged;
        }

        private void OnAxisChanged(object sender, AxisChangedEventArgs e)
        {
            double zoomLevel = _plotModel.Axes[0].ActualMaximum - _plotModel.Axes[0].ActualMinimum;
            if (zoomLevel > 1)
            {
                _positionSeries.MarkerType = MarkerType.None;
            }
            else
            {
                _positionSeries.MarkerType = MarkerType.Circle;
            }
            _plotModel.InvalidatePlot(false);
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

        private Units _selectedUnit = Units.Encoder;
        public Units SelectedUnit
        {
            get => _selectedUnit;
            set
            {
                if (_selectedUnit != value)
                {
                    _selectedUnit = value;
                    OnPropertyChanged(nameof(SelectedUnit));
                    OnPropertyChanged(nameof(EPOSDisplay));
                    OnPropertyChanged(nameof(SPEEDDisplay));
                    OnPropertyChanged(nameof(MaxSpeedDisplay));
                    OnPropertyChanged(nameof(GraphYAxisTitle));
                    UpdateGraphYAxisTitle();
                }
            }
        }

        private bool _autoLogging = true;
        public bool AutoLogging
        {
            get => _autoLogging;
            set
            {
                if (_autoLogging != value)
                {
                    _autoLogging = value;
                    OnPropertyChanged(nameof(AutoLogging));
                    Debug.WriteLine($"AutoLogging changed to: {_autoLogging}");
                }
            }
        }

        public string GraphYAxisTitle
        {
            get
            {
                switch (SelectedUnit)
                {
                    case Units.Encoder: return "EPOS (Encoder Units)";
                    case Units.mm: return "EPOS (mm)";
                    case Units.mu: return "EPOS (mu)";
                    case Units.nm: return "EPOS (nm)";
                    case Units.inch: return "EPOS (inches)";
                    case Units.minch: return "EPOS (milli inches)";
                    case Units.rad: return "EPOS (radians)";
                    case Units.mrad: return "EPOS (mrad)";
                    case Units.deg: return "EPOS (degrees)";
                    default: return "EPOS";
                }
            }
        }

        private void UpdateGraphYAxisTitle()
        {
            if (_plotModel != null)
            {
                var yAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Left);
                if (yAxis != null)
                {
                    yAxis.Title = GraphYAxisTitle;
                    _plotModel.InvalidatePlot(false);
                }
            }
        }



        public double SPEEDDisplay
        {
            get
            {
                // We have stored SPEED internally as either mm/s (if Linear) or deg/s (if Rotational)
                if (Linear)
                {
                    // -------------- SPEED is in mm/s --------------
                    switch (SelectedUnit)
                    {
                        case Units.Encoder:
                            // Convert mm/s → counts/s
                            // 1 mm/s = (1_000_000 / Resolution) counts/s
                            return SPEED * (1_000_000.0 / Resolution);

                        case Units.mm:
                            return SPEED;  // mm/s
                        case Units.mu:
                            return SPEED * 1_000.0;  // µm/s
                        case Units.nm:
                            return SPEED * 1_000_000.0; // nm/s
                        case Units.inch:
                            return SPEED / 25.4; // in/s
                        case Units.minch:
                            return (SPEED / 25.4) * 1_000.0; // mils/s (thousandths of an inch)

                        // Rotational units are not meaningfully supported for a linear axis:
                        case Units.deg:
                        case Units.rad:
                        case Units.mrad:
                            return double.NaN;

                        default:
                            return SPEED;  // fallback: mm/s
                    }
                }
                else
                {
                    // -------------- SPEED is in deg/s --------------
                    switch (SelectedUnit)
                    {
                        case Units.deg:
                            return SPEED; // deg/s
                        case Units.rad:
                            // deg → rad: multiply by (π/180)
                            return SPEED * (Math.PI / 180.0);
                        case Units.mrad:
                            // deg → rad → mrad
                            return SPEED * (Math.PI / 180.0) * 1000.0;

                        case Units.Encoder:
                            // deg → encoder counts: 
                            // 1 deg = (FullRevolutionEncoderUnits / 360) counts
                            double countsPerDeg = FullRevolutionEncoderUnits / 360.0;
                            return SPEED * countsPerDeg; // counts/s

                        // Linear units are not meaningful for a purely rotational axis:
                        case Units.mm:
                        case Units.mu:
                        case Units.nm:
                        case Units.inch:
                        case Units.minch:
                            return double.NaN;

                        default:
                            return SPEED; // fallback: deg/s
                    }
                }
            }
        }

        public double MaxSpeedDisplay
        {
            get
            {
                // Same logic as SPEEDDisplay, except you start with “MaxSpeed”
                // which is also stored in base units (mm/s if linear, deg/s if rotational).
                double baseSpeed = MaxSpeed;

                if (Linear)
                {
                    // baseSpeed is mm/s
                    switch (SelectedUnit)
                    {
                        case Units.Encoder:
                            return baseSpeed * (1_000_000.0 / Resolution);
                        case Units.mm:
                            return baseSpeed;
                        case Units.mu:
                            return baseSpeed * 1_000.0;
                        case Units.nm:
                            return baseSpeed * 1_000_000.0;
                        case Units.inch:
                            return baseSpeed / 25.4;
                        case Units.minch:
                            return (baseSpeed / 25.4) * 1_000.0;
                        default:
                            // rotational units → not applicable
                            return double.NaN;
                    }
                }
                else
                {
                    // baseSpeed is deg/s
                    switch (SelectedUnit)
                    {
                        case Units.deg:
                            return baseSpeed;
                        case Units.rad:
                            return baseSpeed * (Math.PI / 180.0);
                        case Units.mrad:
                            return baseSpeed * (Math.PI / 180.0) * 1_000.0;
                        case Units.Encoder:
                            double countsPerDeg = FullRevolutionEncoderUnits / 360.0;
                            return baseSpeed * countsPerDeg;
                        default:
                            // linear units → not applicable
                            return double.NaN;
                    }
                }
            }
        }


        public IEnumerable<Units> UnitsList => Enum.GetValues(typeof(Units)).Cast<Units>();
        public double EPOSDisplay => UnitConversion.FromEncoder(EPOS, SelectedUnit, Resolution);


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
                // Always update the backing field and notify, even if value is the same.
                if (_DPOS != value)
                {
                    _DPOS = value;
                    OnPropertyChanged(nameof(DPOS));
                }
                else
                {
                    // Force notification even if the value is the same.
                    OnPropertyChanged(nameof(DPOS));
                }

                // Convert DPOS (encoder units) to slider value (in mm)
                double newSliderValue = _DPOS * Resolution / 1000000.0;
                // Update the slider property without triggering SetDPOS again.
                UpdateSliderValue(newSliderValue);
            }
        }

        // Use a helper method to update the slider's backing field directly.
        private void UpdateSliderValue(double newValue)
        {
            _suppressSliderUpdate = true;
            _SliderValue = newValue;
            OnPropertyChanged(nameof(SliderValue));
            _suppressSliderUpdate = false;
        }


        private bool _useControllerTime = true;
        public bool UseControllerTime
        {
            get => _useControllerTime;
            set
            {
                if (_useControllerTime != value)
                {
                    _useControllerTime = value;
                    OnPropertyChanged(nameof(UseControllerTime));
                    // When switching, send the appropriate command:
                    _hasLastSample = false;

                    SetTimeMode(_useControllerTime);
                }
            }
        }

      private async void SetTimeMode(bool useController)
        {
            // If using controller time, send INFO=4; otherwise, for system time send INFO=7.
            string command = useController ? "INFO=4" : "INFO=7";
            await ParentController.SendCommand(command, AxisLetter);
            Debug.WriteLine($"Time mode set to {(useController ? "Controller" : "System")}, sent command {command}");
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
                    OnPropertyChanged(nameof(EPOSDisplay));

                    CalculateSpeed(); // if you want

                    // Also update SliderValue so the UI matches actual hardware
                    if (Linear)
                    {
                        // Convert counts → mm
                        double mmPosition = (_EPOS * Resolution) / 1_000_000.0;
                        UpdateSliderValue(mmPosition);
                    }
                    else
                    {
                        // Convert counts → degrees
                        double degPerCount = 360.0 / FullRevolutionEncoderUnits;
                        double deg = _EPOS * degPerCount;

                        // Keep it in [0..360) if you want the radial gauge pointer
                        deg = (deg % 360.0 + 360.0) % 360.0;

                        UpdateSliderValue(deg);
                    }
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
                    // Check for wrap-around: if the new TIME is less than the previous value, a wrap occurred.
                    if (value < _prevTime)
                    {
                        // Increase the offset by the maximum value of TIME (65,536).
                        // (Make sure to adjust if your controller actually wraps at 65,536 or 65,535.)
                        _timeOffset += 65535;
                    }
                    _prevTime = value;
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
                        OnPropertyChanged(nameof(MaxSpeedDisplay));
                    }
                    OnPropertyChanged(nameof(SPEEDDisplay));
                }
            }
        }

        private bool _WasManualDposExecuted ;
        public bool WasManuaDposlExecuted
        {
            get => WasManuaDposlExecuted;
            set
            {
                if (WasManuaDposlExecuted != value)
                {
                    WasManuaDposlExecuted = value;
                    OnPropertyChanged(nameof(WasManuaDposlExecuted));
                }
            }
        }


        private TimeSpan _commandToPositionReachedDelay;
        public TimeSpan CommandToPositionReachedDelayValue
        {
            get => _commandToPositionReachedDelay;
            set
            {
                if (_commandToPositionReachedDelay != value)
                {
                    _commandToPositionReachedDelay = value;
                    OnPropertyChanged(nameof(CommandToPositionReachedDelayValue));
                    OnPropertyChanged(nameof(CommandToPositionReachedDelay));
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
                    OnPropertyChanged(nameof(MaxSpeedDisplay));
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
                    // Notify that the available units have changed.
                    OnPropertyChanged(nameof(AvailableUnits));
                    // Optionally, reset the selected unit if it is not valid for the new type.
                    if (!AvailableUnits.Contains(SelectedUnit))
                    {
                        if (Linear)
                            _selectedUnit = Units.mm;   // default for linear axis
                        else
                            _selectedUnit = Units.deg;
                    }
                }
            }
        }

        public IEnumerable<Units> AvailableUnits =>
            Linear
                ? new Units[] { Units.Encoder, Units.mm, Units.mu, Units.nm, Units.inch, Units.minch }
                : new Units[] { Units.Encoder, Units.rad, Units.mrad, Units.deg };

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

        public double FullRevolutionEncoderUnits
        {
            get
            {
                if (!Linear)
                {
                    // Use your table lookup – assume AxisType holds a key such as "XRTU_25_109"
                    double counts = StageCountsTable.GetCounts(AxisType);
                    if (counts > 0)
                    {
                        return counts;
                    }
                    // If the lookup returns 0 (or key not found), fall back to a computed value.
                    return UnitConversion.ToEncoder(360.0, SelectedUnit, Resolution);
                }
                else
                {
                    return 0; // Not used for linear axes.
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
                // Simply set to true only if both the incoming value and the tolerance check are true.
                bool newValue = value;
                if (_PositionReached != newValue)
                {
                    _PositionReached = newValue;
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

        private double _SliderValue;
        public double SliderValue
        {
            get => _SliderValue;
            set
            {
                if (_suppressSliderUpdate)
                {
                    _SliderValue = value;
                    return;
                }
                _SliderValue = value;
                OnPropertyChanged(nameof(SliderValue));

                if (Linear)
                {
                    // SliderValue is in millimeters
                    double encCounts = value * (1_000_000.0 / Resolution);
                    SetDPOS(encCounts);
                }
                else
                {
                    // SliderValue is in degrees
                    double countsPerDeg = FullRevolutionEncoderUnits / 360.0;
                    double encCounts = value * countsPerDeg;  // deg → counts
                    SetDPOS(encCounts);
                }
            }
        }

        private void UpdateSliderWithoutCommand(double newValue)
        {
            _suppressSliderUpdate = true;
            SliderValue = newValue;  // This will update the backing field and fire OnPropertyChanged, but not call SetDPOS.
            _suppressSliderUpdate = false;
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

        public string CommandToPositionReachedDelay
        {
            get
            {
                int seconds = (int)_commandToPositionReachedDelay.TotalSeconds;
                int milliseconds = _commandToPositionReachedDelay.Milliseconds;
                int microseconds = (int)((_commandToPositionReachedDelay.Ticks % TimeSpan.TicksPerMillisecond) / 10);
                return $"{seconds:D2}:{milliseconds:D3}:{microseconds:D3}";
            }
            set
            {
                if (TimeSpan.TryParse(value, out TimeSpan newDelay))
                {
                    CommandToPositionReachedDelayValue = newDelay;
                }
                else
                {
                    Debug.WriteLine("Failed to parse CommandToPositionReachedDelay value.");
                }
            }
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
                    double timeDelta = (currentTime - _LastUpdateTime).TotalSeconds;
                    if (timeDelta > 0)
                    {
                        double rawEncDiff = _EPOS - _PreviousEPOS;

                        // If rotational, wrap the difference to handle crossing 0/max boundary cleanly.
                        if (!Linear)
                        {
                            double fullRev = FullRevolutionEncoderUnits; // e.g., 200000 for 360°
                            rawEncDiff = (rawEncDiff + fullRev / 2.0) % fullRev - (fullRev / 2.0);
                        }

                        double distanceOrAngleMoved;
                        if (Linear)
                        {
                            // Convert from encoder counts → mm
                            // "Resolution" means nm per encoder count, so rawEncDiff * (Resolution nm)
                            // then /1e6 => mm
                            distanceOrAngleMoved = rawEncDiff * (Resolution / 1_000_000.0); // mm
                        }
                        else
                        {
                            // Convert from encoder counts → deg
                            double degPerCount = 360.0 / FullRevolutionEncoderUnits;
                            distanceOrAngleMoved = rawEncDiff * degPerCount; // deg
                        }

                        double speedValue = distanceOrAngleMoved / timeDelta;

                        // Keep speed as absolute magnitude
                        SPEED = Math.Abs(speedValue);
                    }
                }

                _LastUpdateTime = currentTime;
            }
        }


        public void MoveNegative()
        {
            ParentController.SendCommand("MOVE=-1", AxisLetter);
            // Set slider to the left extreme (assuming NegativeRange is in the same unit as the slider, e.g. mm)
            UpdateSliderWithoutCommand(NegativeRange);
        }

        public void MovePositive()
        {
            ParentController.SendCommand("MOVE=1", AxisLetter);
            // Set slider to the right extreme.
            UpdateSliderWithoutCommand(PositiveRange);
        }

        public void ScanNegative()
        {
            ParentController.SendCommand("SCAN=-1", AxisLetter);
            UpdateSliderWithoutCommand(NegativeRange);
        }

        public void ScanPositive()
        {
            ParentController.SendCommand("SCAN=1", AxisLetter);
            UpdateSliderWithoutCommand(PositiveRange);
        }

        public void StepNegative()
        {
            TakeStep(-StepSize);
        }

        public async void Home()
        {
            UpdateSliderWithoutCommand(0);
            await SetDPOS(0);
        }

        public void StepPositive()
        {
            TakeStep(StepSize);
        }

        public void Stop()
        {
            ParentController.SendCommand("STOP", AxisLetter);
        }

        public Task Index()
        {
            ParentController.SendCommand("INDX=0", AxisLetter);
            return Task.CompletedTask;
        }

        public Task IndexPlus()
        {
            ParentController.SendCommand("INDX=1", AxisLetter);
            return Task.CompletedTask;

        }

        public Task IndexMinus()
        {
            ParentController.SendCommand("INDX=0", AxisLetter);
            return Task.CompletedTask;
        }

        public Task SetDPOS(double value)
        {
            if (!Linear)
            {
                // Use the table lookup value.
                double fullRevolution = FullRevolutionEncoderUnits;
                value = value % fullRevolution;
                if (value < 0)
                    value += fullRevolution;
            }



            // --- Clamp the value against the high/low limits ---
            if (Linear)
            {
                // For linear axes, assume HLIM and LLIM are given in mm.
                var hlimParam = Parameters.FirstOrDefault(p => p.Command == "HLIM");
                var llimParam = Parameters.FirstOrDefault(p => p.Command == "LLIM");
                double hlim_mm = hlimParam != null ? Convert.ToDouble(hlimParam.Value) : double.PositiveInfinity;
                double llim_mm = llimParam != null ? Convert.ToDouble(llimParam.Value) : double.NegativeInfinity;
                double hlimEnc = hlim_mm * 1e6 / Resolution;
                double llimEnc = llim_mm * 1e6 / Resolution;

                if (value > hlimEnc)
                {
                    Debug.WriteLine($"Value {value} is greater than HLIM (converted) {hlimEnc}, adjusting to HLIM.");
                    value = hlimEnc;
                }
                if (value < llimEnc)
                {
                    Debug.WriteLine($"Value {value} is less than LLIM (converted) {llimEnc}, adjusting to LLIM.");
                    value = llimEnc;
                }
            }
            else
            {
                // For rotational axes, assume HLIM and LLIM are provided in degrees.
                var hlimParam = Parameters.FirstOrDefault(p => p.Command == "HLIM");
                var llimParam = Parameters.FirstOrDefault(p => p.Command == "LLIM");
                // Default to a full circle (360°) if no HLIM is given and 0° for LLIM.
                double hlim_deg = hlimParam != null ? Convert.ToDouble(hlimParam.Value) : 360.0;
                double llim_deg = llimParam != null ? Convert.ToDouble(llimParam.Value) : 0.0;
                double hlimEnc = UnitConversion.ToEncoder(hlim_deg, SelectedUnit, Resolution);
                double llimEnc = UnitConversion.ToEncoder(llim_deg, SelectedUnit, Resolution);

                if (value > hlimEnc)
                {
                    Debug.WriteLine($"Value {value} is greater than HLIM (converted) {hlimEnc}, adjusting to HLIM.");
                    value = hlimEnc;
                }
                if (value < llimEnc)
                {
                    Debug.WriteLine($"Value {value} is less than LLIM (converted) {llimEnc}, adjusting to LLIM.");
                    value = llimEnc;
                }
            }

            // Optionally start logging or reset the plot if needed.
            if (AutoLogging)
            {
                _isLogging = true;
                ResetPlot();
            }

            // Update the local property and send the command.
            DPOS = value;
            DateTime commandSentTime = DateTime.Now;
            ParentController.SendCommand($"DPOS={value}", AxisLetter);

            // If the current encoder position is already within tolerance, finish immediately.
            if (IsWithinTolerance(value) && PositionReached)
            {
                var duration = DateTime.Now - commandSentTime;
                Debug.WriteLine($"SetDPOS executed in {duration.TotalMilliseconds} ms (immediate).");
                CommandToPositionReachedDelayValue = duration;
                return Task.CompletedTask;
            }

            // Otherwise, wait until the property changes and the axis reaches the target.
            var tcs = new TaskCompletionSource<bool>();
            PropertyChangedEventHandler handler = null;
            handler = (sender, args) =>
            {
                if (IsWithinTolerance(value) && PositionReached)
                {
                    this.PropertyChanged -= handler;
                    var duration = DateTime.Now - commandSentTime;
                    Debug.WriteLine($"SetDPOS executed in {duration.TotalMilliseconds} ms.");
                    CommandToPositionReachedDelayValue = duration;
                    UpdatePlotFromQueue(null, null);
                    SPEED = 0;
                    if (AutoLogging)
                    {
                        _isLogging = false;
                    }
                    tcs.TrySetResult(true);
                }
            };
            this.PropertyChanged += handler;
            return tcs.Task;
        }


        public async Task TakeStep(double value)
        {
            // Convert the step value (in the selected unit) to encoder units
            double stepEnc = UnitConversion.ToEncoder(value, SelectedUnit, Resolution);
            DPOS += stepEnc;
            await SetDPOS(DPOS);
        }

        #endregion

        #region Reset & Encoder Methods

        private async Task ResetAsync()
        {
            ParentController.LoadingSettings = true;
            ParentController.SendCommand("RSET", AxisLetter);
            await Task.Delay(100);
            await ParentController.LoadParametersFromController();
            ParentController.LoadingSettings = false;
        }

        private bool IsWithinTolerance(double dpos)
        {
            // Get the current encoder position from the EPOS property.
            double epos = (int)EPOS;
            double diff = Math.Abs(dpos - epos);

            // For rotary (non-linear) axes, account for wrap-around.
            if (!Linear)
            {
                // Convert 360° to encoder units using your UnitConversion helper.
                double encRev = UnitConversion.ToEncoder(360.0, Units.deg, Resolution);
                double iEncRev = (int)encRev;
                double wrapAdd = Math.Abs((dpos + iEncRev) - epos);
                double wrapSub = Math.Abs((dpos - iEncRev) - epos);
                diff = Math.Min(diff, Math.Min(wrapAdd, wrapSub));
            }

            // Get the tolerance from the PTO2 parameter in the Parameters collection.
            double pto2 = 10; // default value if not found
            var pto2Param = Parameters.FirstOrDefault(p => p.Command == "PTO2");
            if (pto2Param != null)
            {
                pto2 = pto2Param.Value;
            }
            return (diff <= pto2);
        }




        #endregion

        #region Plot Reset Method

        // Add a field to store the baseline (in seconds)
        private void ResetPlot()
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                // Clear the plot data.
                _positionSeries.Points.Clear();
                _minEpos = double.MaxValue;
                _maxEpos = double.MinValue;
                ParentController.Flush();

                // Reset the wrap-around tracking variables.
                _timeOffset = 0;
                _prevTime = this.TIME;  // Start tracking from the current TIME

                // Reset the time baseline based on the current time source.
                _startSyncTime = UseControllerTime ? (this.TIME / 10000.0) : ParentController.GlobalTimeSeconds;

                // Invalidate the plot to force a redraw.
                _plotModel.InvalidatePlot(true);
            });
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
                ParentController.SendCommand("ENCR", AxisLetter);

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
    }
}
        #endregion
