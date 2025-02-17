using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using Windows.Foundation;
using XeryonMotionGUI.Classes;

namespace XeryonMotionGUI.Views
{
    public sealed partial class JoystickWindow : Page
    {
        private IEnumerable<Axis> _allAxes = Array.Empty<Axis>();

        private Axis _axisX;
        private Axis _axisY;
        private bool _invertX = false;
        private bool _invertY = false;

        private Point _center;
        private double _radius;
        private bool _isDragging = false;
        private double _deadZone = 0.1;
        private double _speedMultiplier = 1.0;

        private DateTimeOffset _lastCommandTime = DateTimeOffset.Now;
        private static readonly TimeSpan _sendInterval = TimeSpan.FromMilliseconds(50);
        private bool _isStopped = false;

        public JoystickWindow()
        {
            InitializeComponent();
            this.Loaded += OnPageLoaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // If the caller passed a list of Axes as e.Parameter:
            if (e.Parameter is IEnumerable<Axis> axes && axes.Any())
            {
                _allAxes = axes;
            }

            // Set the combo boxes
            ComboBoxAxisX.ItemsSource = _allAxes;
            ComboBoxAxisY.ItemsSource = _allAxes;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            // Now we can measure the Canvas size
            _center = new Point(CanvasArea.Width / 2, CanvasArea.Height / 2);
            _radius = (CanvasArea.Width / 2) - (Knob.Width / 2);
        }

        private void SpeedMultiplierSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            _speedMultiplier = e.NewValue;
            Debug.WriteLine($"Speed Multiplier set to {_speedMultiplier:F2}");
        }

        private void InvertXButton_Click(object sender, RoutedEventArgs e)
        {
            _invertX = !_invertX;
            Debug.WriteLine($"Invert X set to {_invertX}");
        }

        private void InvertYButton_Click(object sender, RoutedEventArgs e)
        {
            _invertY = !_invertY;
            Debug.WriteLine($"Invert Y set to {_invertY}");
        }

        private void ComboBoxAxisX_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _axisX = e.AddedItems?.FirstOrDefault() as Axis;
            Debug.WriteLine($"Selected X axis: {_axisX?.AxisLetter}");
        }

        private void ComboBoxAxisY_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _axisY = e.AddedItems?.FirstOrDefault() as Axis;
            Debug.WriteLine($"Selected Y axis: {_axisY?.AxisLetter}");
        }

        private void CanvasArea_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isDragging = true;
            CanvasArea.CapturePointer(e.Pointer);
        }

        private void CanvasArea_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging) return;

            var pos = e.GetCurrentPoint(CanvasArea).Position;
            double dx = pos.X - _center.X;
            double dy = pos.Y - _center.Y;

            double distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance > _radius)
            {
                double ratio = _radius / distance;
                dx *= ratio;
                dy *= ratio;
            }

            MoveKnobTo(_center.X + dx, _center.Y + dy);
            UpdateAxisMotion(dx, dy);
        }

        private void CanvasArea_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isDragging = false;
            CanvasArea.ReleasePointerCaptures();

            MoveKnobTo(_center.X, _center.Y);
            StopAxes();
            _isStopped = true;
        }

        private void MoveKnobTo(double x, double y)
        {
            Canvas.SetLeft(Knob, x - Knob.Width / 2);
            Canvas.SetTop(Knob, y - Knob.Height / 2);
        }

        private void UpdateAxisMotion(double dx, double dy)
        {
            if (_axisX == null && _axisY == null) return;

            double distance = Math.Sqrt(dx * dx + dy * dy);
            double fraction = distance / _radius;

            // Deadzone
            if (fraction < _deadZone)
            {
                if (!_isStopped)
                {
                    StopAxes();
                    _isStopped = true;
                }
                return;
            }

            var now = DateTimeOffset.Now;
            if (now - _lastCommandTime < _sendInterval) return;
            _lastCommandTime = now;

            _isStopped = false;
            double fractionX = dx / _radius;
            double fractionY = -dy / _radius;

            if (_invertX) fractionX = -fractionX;
            if (_invertY) fractionY = -fractionY;

            double baseMaxSpeed = 2.0;
            double speedX = fractionX * baseMaxSpeed * _speedMultiplier;
            double speedY = fractionY * baseMaxSpeed * _speedMultiplier;

            if (_axisX != null)
            {
                SendSpeedCommand(_axisX, speedX);
            }
            if (_axisY != null)
            {
                SendSpeedCommand(_axisY, speedY);
            }
        }

        private async void SendSpeedCommand(Axis axis, double speedValue)
        {
            if (Math.Abs(speedValue) < 0.01)
            {
                axis.ParentController.SendCommand("STOP", axis.AxisLetter);
                return;
            }

            double absSpeed = Math.Abs(speedValue);
            double rawSpeed = absSpeed * 1000.0;
            axis.ParentController.SendCommand($"{axis.AxisLetter}:SSPD={rawSpeed}");

            if (speedValue > 0)
            {
                axis.ParentController.SendCommand($"{axis.AxisLetter}:SCAN=1");
            }
            else
            {
                axis.ParentController.SendCommand($"{axis.AxisLetter}:SCAN=-1");
            }
        }

        private void StopAxes()
        {
            if (_axisX != null)
            {
                _axisX.ParentController.SendCommand("STOP", _axisX.AxisLetter);
            }
            if (_axisY != null)
            {
                _axisY.ParentController.SendCommand("STOP", _axisY.AxisLetter);
            }
        }
    }
}
