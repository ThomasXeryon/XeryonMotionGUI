using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Windows.UI;
using Windows.UI.ViewManagement;
using XeryonMotionGUI.Helpers;

namespace XeryonMotionGUI.Classes
{
    public class Axis : INotifyPropertyChanged
    {
        #region Fields
        private PlotModel _plotModel;
        private LineSeries _positionSeries;
        private Queue<double> _lastTwoSpeeds = new Queue<double>(1);
        private LineSeries _speedSeries;
        private ConcurrentQueue<(double EPOS, double Speed, double SyncTime)> _dataQueue = new();
        private DispatcherTimer _updateTimer;
        private object _lock = new object();

        private double _minEpos = double.MaxValue;
        private double _maxEpos = double.MinValue;
        private double _minSpeed = double.MaxValue;
        private double _maxSpeed = double.MinValue;
        private DateTime _startTime = DateTime.MinValue;
        private bool _isLogging = false;
        private DateTime _endTime = DateTime.MinValue;
        private double _currentTime = 0;
        private double _startSyncTime = 0;
        private double _lastPosition = 0.0;
        private double _lastTime = 0.0;
        private bool _hasLastSample = false;
        private double _timeOffset = 0;
        private int _lastEncoder = 0;
        private double _lastSpeedTime = 0;
        private bool _hasLastEncoder = false;
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

            foreach (var parameter in Parameters)
            {
                parameter.ParentAxis = this;
                parameter.ParentController = ParentController;
            }
            InitializePlot();
            if (_selectedUnit == default(Units))
            {
                _selectedUnit = Linear ? Units.mm : Units.deg;
            }
        }
        #endregion

        #region PlotDisplayMode Definition (Single Instance)
        public enum PlotDisplayModeEnum
        {
            Both,
            PositionOnly,
            SpeedOnly
        }


        private PlotDisplayModeEnum _plotDisplayMode = PlotDisplayModeEnum.Both;
        public PlotDisplayModeEnum PlotDisplayMode
        {
            get => _plotDisplayMode;
            set
            {
                if (_plotDisplayMode != value)
                {
                    _plotDisplayMode = value;
                    OnPropertyChanged(nameof(PlotDisplayMode));
                    UpdateSeriesVisibility();
                }
            }
        }
        #endregion

        #region Parameter Management
        public ObservableCollection<Parameter> Parameters { get; set; } = new();

        private void InitializeParameters(string axisType)
        {
            Parameters.Clear();

            var newParameters = ParameterFactory.CreateParameters(ParentController.Type, axisType);
            foreach (var parameter in newParameters)
            {
                parameter.ParentAxis = this;
                parameter.ParentController = ParentController;
                parameter.PropertyChanged += OnParameterPropertyChanged;
                Parameters.Add(parameter);
            }

            OnPropertyChanged(nameof(Parameters));
        }

        private void OnParameterPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Parameter.Value))
                return;

            var param = (Parameter)sender;

            if (param.Command == "HLIM" || param.Command == "LLIM")
            {
                var hlimParam = Parameters.FirstOrDefault(p => p.Command == "HLIM");
                var llimParam = Parameters.FirstOrDefault(p => p.Command == "LLIM");
                if (hlimParam == null || llimParam == null)
                    return;

                double hlim = hlimParam.Value;
                double llim = llimParam.Value;
                Range = hlim - llim;
            }

            if (param.Command == "FREQ" || param.Command == "FRQ2")
            {
                FrequencyRangeHelper.UpdateFrequency(param);
            }
        }
        #endregion

        #region EPOS Update Method
        public void OnEPOSUpdate(double epos, double dummy)
        {
            if (!_isLogging) return;

            double tSeconds = UseControllerTime
                ? (_timeOffset + TIME) / 10000.0
                : ParentController.GlobalTimeSeconds;

            CalculateSpeed();
            _dataQueue.Enqueue((epos, SPEED, tSeconds));

            if (epos < _minEpos) _minEpos = epos;
            if (epos > _maxEpos) _maxEpos = epos;
            if (SPEED < _minSpeed) _minSpeed = SPEED;
            if (SPEED > _maxSpeed) _maxSpeed = SPEED;

            _ = UpdatePlotFromQueueAsync(null, null);
        }
        #endregion

        #region Plot Update Methods
        private async Task UpdatePlotFromQueueAsync(object sender, object e)
        {
            var pointsToPlot = new List<(double EPOS, double Speed, double SyncTime)>();
            while (_dataQueue.TryDequeue(out var point))
            {
                pointsToPlot.Add(point);
            }

            var tcs = new TaskCompletionSource<bool>();

            _dispatcherQueue.TryEnqueue(async () =>
            {
                foreach (var p in pointsToPlot)
                {
                    double relativeTime = p.SyncTime - _startSyncTime;
                    if (_plotDisplayMode == PlotDisplayModeEnum.Both || _plotDisplayMode == PlotDisplayModeEnum.PositionOnly)
                    {
                        _positionSeries.Points.Add(new DataPoint(relativeTime, p.EPOS));
                    }
                    if (_plotDisplayMode == PlotDisplayModeEnum.Both || _plotDisplayMode == PlotDisplayModeEnum.SpeedOnly)
                    {
                        _speedSeries.Points.Add(new DataPoint(relativeTime, p.Speed));
                    }
                }

                await AdjustAxesBasedOnDataAsync();
                _plotModel.InvalidatePlot(true);
                tcs.SetResult(true);
            });

            await tcs.Task;
        }

        public ICommand StartManualLoggingCommand => new RelayCommand(() => StartManualLogging());
        public ICommand StopManualLoggingCommand => new RelayCommand(() => StopManualLogging());

        public void StartManualLogging()
        {
            Debug.WriteLine($"Manual logging started for Axis {AxisLetter}.");
            _isLogging = true;

            if (UseControllerTime)
            {
                _startSyncTime = (_timeOffset + TIME) / 10000.0;
            }
            else
            {
                _startSyncTime = ParentController.GlobalTimeSeconds;
            }

            ResetPlot();
        }

        public void StopManualLogging()
        {
            Debug.WriteLine($"Manual logging stopped for Axis {AxisLetter}.");
            _isLogging = false;
        }

        private async Task AdjustAxesBasedOnDataAsync()
        {
            var tcs = new TaskCompletionSource<bool>();

            _dispatcherQueue.TryEnqueue(() =>
            {
                var xAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom) as LinearAxis;
                var positionAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Left) as LinearAxis;
                var speedAxis = _plotModel.Axes.FirstOrDefault(a => a.Key == "SpeedAxis") as LinearAxis;

                if (xAxis == null || positionAxis == null || speedAxis == null)
                {
                    tcs.SetResult(true);
                    return;
                }

                double minTime = double.MaxValue;
                double maxTime = double.MinValue;
                if (_positionSeries.Points.Any())
                {
                    minTime = Math.Min(minTime, _positionSeries.Points.Min(p => p.X));
                    maxTime = Math.Max(maxTime, _positionSeries.Points.Max(p => p.X));
                }
                if (_speedSeries.Points.Any())
                {
                    minTime = Math.Min(minTime, _speedSeries.Points.Min(p => p.X));
                    maxTime = Math.Max(maxTime, _speedSeries.Points.Max(p => p.X));
                }
                if (minTime == double.MaxValue)
                {
                    minTime = 0;
                    maxTime = 1.0;
                }
                if (minTime < 0) minTime = 0;
                if (maxTime <= minTime) maxTime = minTime + 1.0;
                xAxis.Minimum = minTime;
                xAxis.Maximum = maxTime;

                if (_plotDisplayMode == PlotDisplayModeEnum.Both || _plotDisplayMode == PlotDisplayModeEnum.PositionOnly)
                {
                    double minEpos = _positionSeries.Points.Any() ? _positionSeries.Points.Min(p => p.Y) : 0;
                    double maxEpos = _positionSeries.Points.Any() ? _positionSeries.Points.Max(p => p.Y) : 0;
                    double eposRange = maxEpos - minEpos;
                    if (eposRange < 1e-6)
                    {
                        minEpos -= 0.5;
                        maxEpos += 0.5;
                    }
                    else
                    {
                        double padding = eposRange * 0.05;
                        minEpos -= padding;
                        maxEpos += padding;
                    }
                    positionAxis.Minimum = minEpos;
                    positionAxis.Maximum = maxEpos;
                    positionAxis.IsAxisVisible = true;
                }
                else
                {
                    positionAxis.IsAxisVisible = false;
                }

                if (_plotDisplayMode == PlotDisplayModeEnum.Both || _plotDisplayMode == PlotDisplayModeEnum.SpeedOnly)
                {
                    double minSpeed = _speedSeries.Points.Any() ? _speedSeries.Points.Min(p => p.Y) : 0;
                    double maxSpeed = _speedSeries.Points.Any() ? _speedSeries.Points.Max(p => p.Y) : 0;
                    double speedRange = maxSpeed - minSpeed;
                    if (speedRange < 1e-6)
                    {
                        minSpeed = Math.Min(0, minSpeed - 0.5);
                        maxSpeed = maxSpeed + 0.5;
                    }
                    else
                    {
                        double padding = speedRange * 0.05;
                        minSpeed -= padding;
                        maxSpeed += padding;
                    }
                    speedAxis.Minimum = minSpeed;
                    speedAxis.Maximum = maxSpeed;
                    speedAxis.IsAxisVisible = true;
                }
                else
                {
                    speedAxis.IsAxisVisible = false;
                }

                xAxis.Reset();
                positionAxis.Reset();
                speedAxis.Reset();

                tcs.SetResult(true);
            });

            await tcs.Task;
        }

        private void ApplyThemeToPlot(ElementTheme theme)
        {
            // Grab references to your OxyPlot axes
            var xAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
            var positionAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Left);
            var speedAxis = _plotModel.Axes.FirstOrDefault(a => a.Key == "SpeedAxis");

            // Decide text color based on the theme
            if (theme == ElementTheme.Dark)
            {
                if (xAxis != null) xAxis.TextColor = OxyColors.Black;
                if (positionAxis != null) positionAxis.TextColor = OxyColors.Black;
                if (speedAxis != null) speedAxis.TextColor = OxyColors.Black;

                _plotModel.TextColor = OxyColors.Black;
                // If you want the title "Axis Movement and Speed Over Time" to remain black:
                _plotModel.TitleColor = OxyColors.Black;
                // Light theme => black lines
                xAxis.AxislineColor = OxyColors.Black;
                xAxis.TextColor = OxyColors.Black;
                xAxis.TitleColor = OxyColors.Black;
                xAxis.TicklineColor = OxyColors.Black;

                speedAxis.AxislineColor = OxyColors.Black;
                speedAxis.TextColor = OxyColors.Black;
                speedAxis.TitleColor = OxyColors.Black;
                speedAxis.TicklineColor = OxyColors.Black;

                positionAxis.AxislineColor = OxyColors.Black;
                positionAxis.TextColor = OxyColors.Black;
                positionAxis.TitleColor = OxyColors.Black;
                positionAxis.TicklineColor = OxyColors.Black;

                _plotModel.TextColor = OxyColors.Black;

                if (xAxis != null) xAxis.TextColor = OxyColors.Black;
                if (positionAxis != null) positionAxis.TextColor = OxyColors.Black;
                if (speedAxis != null) speedAxis.TextColor = OxyColors.Black;

                _plotModel.TextColor = OxyColors.Black;
                // If you want the title "Axis Movement and Speed Over Time" to remain black:
                _plotModel.TitleColor = OxyColors.Black;
            }
            if (theme == ElementTheme.Light) 
            {
                // For dark theme
                if (xAxis != null) xAxis.TextColor = OxyColors.White;
                if (positionAxis != null) positionAxis.TextColor = OxyColors.White;
                if (speedAxis != null) speedAxis.TextColor = OxyColors.White;

                _plotModel.TextColor = OxyColors.White;
                _plotModel.TitleColor = OxyColors.White;

                xAxis.AxislineColor = OxyColors.White;
                xAxis.TextColor = OxyColors.White;
                xAxis.TitleColor = OxyColors.White;
                xAxis.TicklineColor = OxyColors.White;

                speedAxis.AxislineColor = OxyColors.White;
                speedAxis.TextColor = OxyColors.White;
                speedAxis.TitleColor = OxyColors.White;
                speedAxis.TicklineColor = OxyColors.White;

                positionAxis.AxislineColor = OxyColors.White;
                positionAxis.TextColor = OxyColors.White;
                positionAxis.TitleColor = OxyColors.White;
                positionAxis.TicklineColor = OxyColors.White;
            }

            _plotModel.InvalidatePlot(false);
        }


private void InitializePlot()
        {
            var frame = App.AppTitlebar as FrameworkElement;

            _plotModel = new PlotModel
            {
                Title = "Axis Movement and Speed Over Time",
                Background = OxyColors.Transparent
            };

            _positionSeries = new LineSeries
            {
                Title = "Position",
                MarkerType = MarkerType.Circle,
                MarkerSize = 2,
                MarkerStroke = OxyColors.Transparent,
                StrokeThickness = 1.5,
                LineStyle = LineStyle.Solid,
                Color = OxyColors.Blue
            };
            _plotModel.Series.Add(_positionSeries);

            _speedSeries = new LineSeries
            {
                Title = "Speed",
                MarkerType = MarkerType.None,
                StrokeThickness = 1.5,
                LineStyle = LineStyle.Solid,
                Color = OxyColors.Red,
               // InterpolationAlgorithm = InterpolationAlgorithms.UniformCatmullRomSpline,

                YAxisKey = "SpeedAxis"
            };
            _plotModel.Series.Add(_speedSeries);

            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Time (s)",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true
            };
            _plotModel.Axes.Add(xAxis);

            var positionAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = GraphYAxisTitle,
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                IsZoomEnabled = true,
                IsPanEnabled = true
            };
            _plotModel.Axes.Add(positionAxis);

            var speedAxis = new LinearAxis
            {
                Position = AxisPosition.Right,
                Title = Linear ? "Speed (mm/s)" : "Speed (deg/s)",
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Key = "SpeedAxis"
            };
            _plotModel.Axes.Add(speedAxis);

            if (frame != null && frame.ActualTheme == ElementTheme.Light)
            {
                xAxis.TextColor = OxyColors.Black;
                positionAxis.TextColor = OxyColors.Black;
                speedAxis.TextColor = OxyColors.Black;
                _plotModel.TextColor = OxyColors.Black;
            }
            else
            {
                xAxis.TextColor = OxyColors.White;
                positionAxis.TextColor = OxyColors.White;
                speedAxis.TextColor = OxyColors.White;
                _plotModel.TextColor = OxyColors.White;
            }

            _plotModel.IsLegendVisible = true;
            xAxis.AxisChanged += OnAxisChanged;

            if (frame != null)
            {
                ApplyThemeToPlot(frame.ActualTheme);

                // (Optional) subscribe to theme changes so we can update text color again
                frame.ActualThemeChanged += (s, e) =>
                {
                    ApplyThemeToPlot(frame.ActualTheme);
                };
            }
            else
            {
                // If we can’t detect or don’t care about theme, default to black or white.
                ApplyThemeToPlot(ElementTheme.Dark); // or ElementTheme.Light
            }

            UpdateSeriesVisibility();

        }

        private void UpdateSeriesVisibility()
        {
            if (_positionSeries == null || _speedSeries == null) return;

            switch (_plotDisplayMode)
            {
                case PlotDisplayModeEnum.Both:
                    _positionSeries.IsVisible = true;
                    _speedSeries.IsVisible = true;
                    break;
                case PlotDisplayModeEnum.PositionOnly:
                    _positionSeries.IsVisible = true;
                    _speedSeries.IsVisible = false;
                    break;
                case PlotDisplayModeEnum.SpeedOnly:
                    _positionSeries.IsVisible = false;
                    _speedSeries.IsVisible = true;
                    break;
            }

            // Refresh the plot
            _plotModel.InvalidatePlot(true);
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
            double minDuration = 1.0;
            if (totalDuration < minDuration)
            {
                totalDuration = minDuration;
            }

            var xAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
            if (xAxis == null)
            {
                xAxis = new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "Time (s)",
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot
                };
                _plotModel.Axes.Add(xAxis);
            }

            xAxis.Minimum = 0;
            xAxis.Maximum = totalDuration;
        }
        #endregion

        #region Y-Axis Adjustment
        private void AdjustYAxis()
        {
            double padding = 0.05;
            double range = _maxEpos - _minEpos;
            double paddedRange = range * (1 + 2 * padding);
            double paddedMin = _minEpos - range * padding;
            double paddedMax = _maxEpos + range * padding;

            double minRange = 100;
            if (paddedMax - paddedMin < minRange)
            {
                double center = (_maxEpos + _minEpos) / 2;
                paddedMin = center - minRange / 2;
                paddedMax = center + minRange / 2;
            }

            var yAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Left);
            if (yAxis == null)
            {
                yAxis = new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "EPOS (mm)",
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot
                };
                _plotModel.Axes.Add(yAxis);
            }

            yAxis.Minimum = paddedMin;
            yAxis.Maximum = paddedMax;
        }
        #endregion

        #region PlotModel Property
        public PlotModel PlotModel
        {
            get => _plotModel;

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
        public Controller ParentController
        {
            get; set;
        }
        public string AxisType
        {
            get; set;
        }

        public Units _selectedUnit;
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

        private string _deviceSerial;
        public string DeviceSerial
        {
            get => _deviceSerial;
            set
            {
                if (_deviceSerial != value)
                {
                    _deviceSerial = value;
                    OnPropertyChanged(nameof(DeviceSerial));
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
                var positionAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Left);
                var speedAxis = _plotModel.Axes.FirstOrDefault(a => a.Key == "SpeedAxis");
                if (positionAxis != null)
                {
                    positionAxis.Title = GraphYAxisTitle;
                }
                if (speedAxis != null)
                {
                    speedAxis.Title = Linear ? "Speed (mm/s)" : "Speed (deg/s)";
                }
                _plotModel.InvalidatePlot(false);
            }
        }

        public double SPEEDDisplay
        {
            get
            {
                if (Linear)
                {
                    switch (SelectedUnit)
                    {
                        case Units.Encoder: return SPEED * (1_000_000.0 / Resolution);
                        case Units.mm: return SPEED;
                        case Units.mu: return SPEED * 1_000.0;
                        case Units.nm: return SPEED * 1_000_000.0;
                        case Units.inch: return SPEED / 25.4;
                        case Units.minch: return (SPEED / 25.4) * 1_000.0;
                        case Units.deg:
                        case Units.rad:
                        case Units.mrad: return double.NaN;
                        default: return SPEED;
                    }
                }
                else
                {
                    switch (SelectedUnit)
                    {
                        case Units.deg: return SPEED;
                        case Units.rad: return SPEED * (Math.PI / 180.0);
                        case Units.mrad: return SPEED * (Math.PI / 180.0) * 1000.0;
                        case Units.Encoder: return SPEED * (FullRevolutionEncoderUnits / 360.0);
                        case Units.mm:
                        case Units.mu:
                        case Units.nm:
                        case Units.inch:
                        case Units.minch: return double.NaN;
                        default: return SPEED;
                    }
                }
            }
        }

        public double MaxSpeedDisplay
        {
            get
            {
                double baseSpeed = MaxSpeed;
                if (Linear)
                {
                    switch (SelectedUnit)
                    {
                        case Units.Encoder: return baseSpeed * (1_000_000.0 / Resolution);
                        case Units.mm: return baseSpeed;
                        case Units.mu: return baseSpeed * 1_000.0;
                        case Units.nm: return baseSpeed * 1_000_000.0;
                        case Units.inch: return baseSpeed / 25.4;
                        case Units.minch: return (baseSpeed / 25.4) * 1_000.0;
                        default: return double.NaN;
                    }
                }
                else
                {
                    switch (SelectedUnit)
                    {
                        case Units.deg: return baseSpeed;
                        case Units.rad: return baseSpeed * (Math.PI / 180.0);
                        case Units.mrad: return baseSpeed * (Math.PI / 180.0) * 1_000.0;
                        case Units.Encoder: return baseSpeed * (FullRevolutionEncoderUnits / 360.0);
                        default: return double.NaN;
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
                if (_DPOS != value)
                {
                    _DPOS = value;
                    OnPropertyChanged(nameof(DPOS));
                }
                else
                {
                    OnPropertyChanged(nameof(DPOS));
                }
            }
        }

        public void UpdateSliderValue(double newValue)
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
                    _hasLastSample = false;
                    SetTimeMode(_useControllerTime);
                }
            }
        }

        private bool _isUserDraggingSlider = false;
        public bool IsUserDraggingSlider
        {
            get => _isUserDraggingSlider;
            set
            {
                if (_isUserDraggingSlider != value)
                {
                    _isUserDraggingSlider = value;
                    OnPropertyChanged(nameof(IsUserDraggingSlider));
                }
            }
        }

        private async void SetTimeMode(bool useController)
        {
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

                    CalculateSpeed();

                    if (!_isUserDraggingSlider)
                    {
                        if (Linear)
                        {
                            double mmPosition = (_EPOS * Resolution) / 1_000_000.0;
                            UpdateSliderValue(mmPosition);
                        }
                        else
                        {
                            double degPerCount = 360.0 / FullRevolutionEncoderUnits;
                            double deg = _EPOS * degPerCount;
                            deg = (deg % 360.0 + 360.0) % 360.0;
                            UpdateSliderValue(deg);
                        }
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
                    if (value < _prevTime)
                    {
                        _timeOffset += 65536;
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

        private bool _WasManualDposExecuted;
        public bool WasManuaDposlExecuted
        {
            get => _WasManualDposExecuted;
            set
            {
                if (_WasManualDposExecuted != value)
                {
                    _WasManualDposExecuted = value;
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
                    OnPropertyChanged(nameof(AvailableUnits));
                    UpdateGraphYAxisTitle();
                    if (!AvailableUnits.Contains(SelectedUnit))
                    {
                        Debug.WriteLine("Unknown unit selected");
                        _selectedUnit = Linear ? Units.mm : Units.deg;
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
                    double counts = StageCountsTable.GetCounts(AxisType);
                    if (counts > 0)
                    {
                        return counts;
                    }
                    return UnitConversion.ToEncoder(360.0, SelectedUnit, Resolution);
                }
                else
                {
                    return 0;
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
                bool newValue = value && IsWithinTolerance(DPOS);
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
                    return;
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
                    OnPropertyChanged(nameof(SliderBackground));
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
                    OnPropertyChanged(nameof(SliderBackground));
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
                    OnPropertyChanged(nameof(IsResetEnabled));
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
                    return;
                }

                _SliderValue = value;
                OnPropertyChanged(nameof(SliderValue));

                if (Linear)
                {
                    double encCounts = value * (1_000_000.0 / Resolution);
                    SetDPOS(encCounts);
                }
                else
                {
                    double countsPerDeg = FullRevolutionEncoderUnits / 360.0;
                    double encCounts = value * countsPerDeg;
                    SetDPOS(encCounts);
                }
            }
        }

        private void UpdateSliderWithoutCommand(double newValue)
        {
            _suppressSliderUpdate = true;
            SliderValue = newValue;
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
                // If motor is off, speed is zero
                SPEED = 0;
                // Also clear out the queue so we start fresh when motor resumes
                _lastTwoSpeeds.Clear();
            }
            else
            {
                double currentTime = UseControllerTime
                    ? (_timeOffset + TIME) / 10000.0
                    : ParentController.GlobalTimeSeconds;

                if (_hasLastSample)
                {
                    double timeDelta = currentTime - _lastTime;
                    if (timeDelta > 1e-9)
                    {
                        double rawEncDiff = EPOS - _lastPosition;

                        // Wrap-around logic for rotational axes:
                        if (!Linear)
                        {
                            int fullRev = (int)Math.Round(FullRevolutionEncoderUnits);
                            int lastPosInt = (int)Math.Round(_lastPosition);
                            int currPosInt = (int)Math.Round(EPOS);
                            int diff = currPosInt - lastPosInt;

                            int halfRev = fullRev / 2;
                            if (diff > halfRev) diff -= fullRev;
                            else if (diff < -halfRev) diff += fullRev;

                            rawEncDiff = diff;
                        }

                        // Convert encoder diff to distance/angle
                        double distanceOrAngle = Linear
                            ? rawEncDiff * (Resolution / 1_000_000.0)
                            : rawEncDiff * (360.0 / FullRevolutionEncoderUnits);

                        // We want speed to be always positive => take abs
                        double rawSpeed = Math.Abs(distanceOrAngle / timeDelta);

                        // Put this new raw speed in the queue:
                        if (_lastTwoSpeeds.Count == 2)
                        {
                            _lastTwoSpeeds.Dequeue();
                        }
                        _lastTwoSpeeds.Enqueue(rawSpeed);

                        // SPEED is the average of what's in the queue
                        SPEED = _lastTwoSpeeds.Average();
                    }
                }

                _lastPosition = EPOS;
                _lastTime = currentTime;
                _hasLastSample = true;
            }
        }


        public void MoveNegative()
        {
            ParentController.SendCommand("MOVE=-1", AxisLetter);
            UpdateSliderWithoutCommand(NegativeRange);
        }

        public void MovePositive()
        {
            ParentController.SendCommand("MOVE=1", AxisLetter);
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
            TakeStep(-StepSize, SelectedUnit, Resolution);
        }

        public async void Home()
        {
            UpdateSliderWithoutCommand(0);
            await SetDPOS(0);
        }

        public void StepPositive()
        {
            TakeStep(StepSize, SelectedUnit, Resolution);
        }

        public void Stop()
        {
            ParentController.SendCommand("STOP", AxisLetter);
            MaxSpeed = 0;
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
                double fullRevolution = FullRevolutionEncoderUnits;
                value = value % fullRevolution;
                if (value < 0)
                    value += fullRevolution;
            }

            if (Linear)
            {
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
                var hlimParam = Parameters.FirstOrDefault(p => p.Command == "HLIM");
                var llimParam = Parameters.FirstOrDefault(p => p.Command == "LLIM");
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

            if (AutoLogging)
            {
                _isLogging = true;
                ResetPlot();
            }

            DPOS = value;
            DateTime commandSentTime = DateTime.Now;
            ParentController.SendCommand($"DPOS={value}", AxisLetter);

            if (PositionReached && IsWithinTolerance(value))
            {
                var duration = DateTime.Now - commandSentTime;
                Debug.WriteLine($"SetDPOS executed in {duration.TotalMilliseconds} ms (immediate).");
                CommandToPositionReachedDelayValue = duration;
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>();
            PropertyChangedEventHandler handler = null;
            handler = async (sender, args) =>
            {
                if (PositionReached && IsWithinTolerance(value))
                {
                    this.PropertyChanged -= handler;
                    var duration = DateTime.Now - commandSentTime;
                    Debug.WriteLine($"SetDPOS executed in {duration.TotalMilliseconds} ms.");
                    CommandToPositionReachedDelayValue = duration;
                    await UpdatePlotFromQueueAsync(null, null);
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

        public async Task TakeStep(double stepValue, Units stepUnit, double stepResolution)
        {
            double stepEnc = UnitConversion.ToEncoder(stepValue, stepUnit, stepResolution);
            if (!IsWithinTolerance(DPOS))
            {
                DPOS = EPOS + stepEnc;
            }
            else
            {
                DPOS += stepEnc;
            }
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
            double epos = (int)EPOS;
            double diff = Math.Abs(dpos - epos);

            if (!Linear)
            {
                double encRev = UnitConversion.ToEncoder(360.0, Units.deg, Resolution);
                double iEncRev = (int)encRev;
                double wrapAdd = Math.Abs((dpos + iEncRev) - epos);
                double wrapSub = Math.Abs((dpos - iEncRev) - epos);
                diff = Math.Min(diff, Math.Min(wrapAdd, wrapSub));
            }

            double pto2 = 10;
            var pto2Param = Parameters.FirstOrDefault(p => p.Command == "PTO2");
            if (pto2Param != null)
            {
                pto2 = pto2Param.Value + 1;
            }
            return (diff <= pto2);
        }

        private void ResetPlot()
        {
            if (UseControllerTime)
            {
                _startSyncTime = (_timeOffset + TIME) / 10000.0;
            }
            else
            {
                _startSyncTime = ParentController.GlobalTimeSeconds;
            }

            while (_dataQueue.TryDequeue(out _)) { }
            _positionSeries.Points.Clear();
            _speedSeries.Points.Clear();
            _minEpos = double.MaxValue;
            _maxEpos = double.MinValue;
            _minSpeed = double.MaxValue;
            _maxSpeed = double.MinValue;
            SPEED = 0;
            _lastPosition = 0.0;
            _lastTime = 0.0;
            _hasLastSample = false;

            ParentController.Flush();
            _plotModel.InvalidatePlot(true);
        }

        public async Task ResetEncoderAsync()
        {
            try
            {
                SuppressEncoderError = true;
                EncoderError = false;
                ParentController.SendCommand("ENCR", AxisLetter);

                var pollingIntervalParameter = Parameters.FirstOrDefault(p => p.Command == "POLI");
                int pollingInterval = pollingIntervalParameter != null ? (int)pollingIntervalParameter.Value : 25;
                int suppressionDuration = pollingInterval + 500;

                await Task.Delay(suppressionDuration);
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