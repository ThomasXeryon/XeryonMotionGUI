using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using XeryonMotionGUI.Blocks;
using XeryonMotionGUI.Classes;
using XeryonMotionGUI.Helpers;
using XeryonMotionGUI.ViewModels;
using XeryonMotionGUI.Views;

namespace XeryonMotionGUI
{
    public sealed partial class DraggableElement : UserControl, INotifyPropertyChanged
    {
        private Point _dragStartOffset;
        private bool _isDragging = false;
        private bool _isUpdatingPosition = false;
        private const double SnapThreshold = 60.0;

        public static readonly DependencyProperty IsPaletteBlockProperty =
        DependencyProperty.Register(nameof(IsPaletteBlock), typeof(bool), typeof(DraggableElement), new PropertyMetadata(false));

        public bool IsPaletteBlock
        {
            get => (bool)GetValue(IsPaletteBlockProperty);
            set => SetValue(IsPaletteBlockProperty, value);
        }

        public DraggableElement()
        {
            this.InitializeComponent();
            this.PointerPressed += OnPointerPressed;
            this.SizeChanged += DraggableElement_SizeChanged;
            this.MinHeight = 40; // Ensure minimum height is set in code-behind as well
            this.DataContextChanged += DraggableElement_DataContextChanged;
            this.Loaded += (s, e) =>
            {
                var directionToggle = (DefaultBlockLayout as Grid)?.Children
                    .OfType<ToggleSwitch>()
                    .FirstOrDefault(ts => ts.Header?.ToString() == "Direction");
                if (directionToggle != null)
                {
                    directionToggle.Toggled += (s, args) =>
                    {
                        if (_block != null && _block.GetType().GetProperty("IsPositive") != null)
                        {
                            _block.GetType().GetProperty("IsPositive")?.SetValue(_block, directionToggle.IsOn);
                            SaveProgramState();
                        }
                    };
                }

                var loggingToggle = (DefaultBlockLayout as Grid)?.Children
                    .OfType<ToggleSwitch>()
                    .FirstOrDefault(ts => ts.Header?.ToString() == "Logging");
                if (loggingToggle != null)
                {
                    loggingToggle.Toggled += (s, args) =>
                    {
                        if (_block is LoggingBlock lb)
                        {
                            lb.IsStart = loggingToggle.IsOn;
                            SaveProgramState();
                        }
                    };
                }
            };
        }

        private void DraggableElement_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Block != null)
            {
                Block.X = Canvas.GetLeft(this);
                Block.Y = Canvas.GetTop(this);
                // Respect MinHeight and adjust based on content
                double contentHeight = e.NewSize.Height;
                this.Height = Math.Max(this.MinHeight, contentHeight); // Ensure minimum height
                NotifyPositionChanged();
                MoveConnectedBlocks(this, Block.X, Block.Y);
                Debug.WriteLine($"Block '{Text}' size changed: Width={ActualWidth}, Height={Height}");
            }
        }

        private void DraggableElement_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            UpdateBindings();
        }

        private void UpdateBindings()
        {
            var bindingExpressions = GetBindingExpressions();
            foreach (var expr in bindingExpressions)
            {
                expr?.UpdateSource();
            }
        }

        private IEnumerable<BindingExpression> GetBindingExpressions()
        {
            var grid = DefaultBlockLayout as Grid;
            if (grid == null) return Array.Empty<BindingExpression>();

            return new[]
            {
                this.GetBindingExpression(TextProperty),
                grid.FindName("WaitTimeInput") is NumberBox waitBox ? waitBox.GetBindingExpression(NumberBox.ValueProperty) : null,
                (RepeatBlockLayout.FindName("RepeatCountInput") as NumberBox)?.GetBindingExpression(NumberBox.ValueProperty),
                (RepeatBlockLayout.FindName("BlocksToRepeatInput") as NumberBox)?.GetBindingExpression(NumberBox.ValueProperty),
                grid.Children.OfType<ToggleSwitch>().FirstOrDefault(ts => ts.Header?.ToString() == "Direction")?.GetBindingExpression(ToggleSwitch.IsOnProperty)
            }.Where(b => b != null);
        }

        private void SaveProgramState()
        {
            var page = FindParent<DemoBuilderPage>(this);
            if (page != null && page.DataContext is DemoBuilderViewModel vm)
            {
                vm.SaveCurrentProgramState(page);
                Debug.WriteLine($"Save triggered: Block '{Text}', IsPositive={(_block.GetType().GetProperty("IsPositive")?.GetValue(_block) as bool? ?? false)}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event EventHandler PositionChanged;
        private void NotifyPositionChanged()
        {
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        public static readonly DependencyProperty RunningControllersProperty =
            DependencyProperty.Register(nameof(RunningControllers), typeof(ObservableCollection<Controller>), typeof(DraggableElement), new PropertyMetadata(null));

        public ObservableCollection<Controller> RunningControllers
        {
            get => (ObservableCollection<Controller>)GetValue(RunningControllersProperty);
            set
            {
                SetValue(RunningControllersProperty, value);
                Debug.WriteLine($"RunningControllers set: Count = {value?.Count}");
            }
        }

        private ObservableCollection<Parameter> _parameters;
        public ObservableCollection<Parameter> Parameters
        {
            get => _parameters;
            set
            {
                _parameters = value;
                OnPropertyChanged(nameof(Parameters));
            }
        }

        private BlockBase _block;
        public BlockBase Block
        {
            get => _block;
            set
            {
                if (_block != value)
                {
                    if (_block is INotifyPropertyChanged oldBlock)
                    {
                        oldBlock.PropertyChanged -= Block_PropertyChanged;
                    }
                    _block = value;
                    OnPropertyChanged(nameof(Block));
                    if (_block != null)
                    {
                        this.Text = _block.Text;
                        _block.UiElement = this;
                        _block.InitializeControllerAndAxis(this.RunningControllers);
                        if (_block is INotifyPropertyChanged newBlock)
                        {
                            newBlock.PropertyChanged += Block_PropertyChanged;
                        }
                        UpdateBindings();
                        Debug.WriteLine($"Block set: Text = {this.Text}, IsPositive = {(_block.GetType().GetProperty("IsPositive")?.GetValue(_block) as bool? ?? false)}");
                    }
                }
            }
        }

        private void Block_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsPositive" && _block != null)
            {
                var isPositive = _block.GetType().GetProperty("IsPositive")?.GetValue(_block) as bool?;
                Debug.WriteLine($"Block_PropertyChanged: {Text}.IsPositive changed to {isPositive}");
            }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(DraggableElement), new PropertyMetadata(string.Empty, OnTextChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DraggableElement draggableElement && draggableElement._block != null)
            {
                draggableElement._block.Text = e.NewValue as string;
            }
        }

        private DraggableElement _previousBlock;
        public DraggableElement PreviousBlock
        {
            get => _previousBlock;
            set
            {
                if (_previousBlock != value)
                {
                    _previousBlock = value;
                    if (_block != null)
                    {
                        _block.PreviousBlock = value?._block;
                    }
                }
            }
        }

        private DraggableElement _nextBlock;
        public DraggableElement NextBlock
        {
            get => _nextBlock;
            set
            {
                if (_nextBlock != value)
                {
                    _nextBlock = value;
                    if (_block != null)
                    {
                        _block.NextBlock = value?._block;
                    }
                }
            }
        }

        public DraggableElement SnapShadow
        {
            get; set;
        }

        private Canvas _workspaceCanvas;
        public Canvas WorkspaceCanvas
        {
            get => _workspaceCanvas;
            set
            {
                _workspaceCanvas = value;
                Debug.WriteLine($"WorkspaceCanvas set in DraggableElement: {_workspaceCanvas != null}");
            }
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (WorkspaceCanvas == null)
            {
                Debug.WriteLine("WorkspaceCanvas is not set!");
                return;
            }

            _isDragging = true;
            var position = e.GetCurrentPoint(WorkspaceCanvas).Position;
            _dragStartOffset = new Point(position.X - Canvas.GetLeft(this), position.Y - Canvas.GetTop(this));
            WorkspaceCanvas.PointerMoved += OnPointerMoved;
            WorkspaceCanvas.PointerReleased += OnPointerReleased;
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging || _isUpdatingPosition) return;

            _isUpdatingPosition = true;
            try
            {
                var currentPosition = e.GetCurrentPoint(WorkspaceCanvas).Position;
                double newLeft = currentPosition.X - _dragStartOffset.X;
                double newTop = currentPosition.Y - _dragStartOffset.Y;
                Canvas.SetLeft(this, newLeft);
                Canvas.SetTop(this, newTop);
                NotifyPositionChanged();
                MoveConnectedBlocks(this, newLeft, newTop);
            }
            finally
            {
                _isUpdatingPosition = false;
            }
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging) return;

            _isDragging = false;

            UpdateBindings();
            SnapToNearestBlock();

            WorkspaceCanvas.PointerMoved -= OnPointerMoved;
            WorkspaceCanvas.PointerReleased -= OnPointerReleased;

            var page = FindParent<DemoBuilderPage>(this);
            if (page != null && page.DataContext is DemoBuilderViewModel vm)
            {
                vm.SaveCurrentProgramState(page);
            }
            Debug.WriteLine($"PointerReleased: Block '{Text}' dropped at ({Canvas.GetLeft(this)}, {Canvas.GetTop(this)})");
        }

        private void SnapToNearestBlock()
        {
            if (WorkspaceCanvas == null || SnapShadow == null || SnapShadow.Visibility != Visibility.Visible)
            {
                Debug.WriteLine("WorkspaceCanvas, SnapShadow, or visibility issue in SnapToNearestBlock.");
                return;
            }

            double snapLeft = Canvas.GetLeft(SnapShadow);
            double snapTop = Canvas.GetTop(SnapShadow);
            Canvas.SetLeft(this, snapLeft);
            Canvas.SetTop(this, snapTop);

            if (this.PreviousBlock != null)
            {
                this.PreviousBlock.NextBlock = null;
            }

            foreach (var child in WorkspaceCanvas.Children)
            {
                if (child is DraggableElement targetBlock && targetBlock != this && targetBlock != SnapShadow)
                {
                    double targetLeft = Canvas.GetLeft(targetBlock);
                    double targetTop = Canvas.GetTop(targetBlock);
                    if (Math.Abs(snapLeft - targetLeft) < SnapThreshold &&
                        Math.Abs(snapTop - (targetTop + targetBlock.ActualHeight)) < SnapThreshold)
                    {
                        this.PreviousBlock = targetBlock;
                        targetBlock.NextBlock = this;
                        Debug.WriteLine($"Block snapped to: {targetBlock.Text}");
                        break;
                    }
                }
            }

            MoveConnectedBlocks(this, snapLeft, snapTop);
        }

        private void MoveConnectedBlocks(DraggableElement block, double parentLeft, double parentTop)
        {
            if (block.NextBlock != null)
            {
                var nextBlock = block.NextBlock;
                double nextLeft = parentLeft;
                double nextTop = parentTop + block.ActualHeight;
                Canvas.SetLeft(nextBlock, nextLeft);
                Canvas.SetTop(nextBlock, nextTop);
                nextBlock.NotifyPositionChanged();
                MoveConnectedBlocks(nextBlock, nextLeft, nextTop);
            }
        }

        public void HighlightBlock(bool isExecuting)
        {
            this.Background = isExecuting ? new SolidColorBrush(Colors.LightGreen) : new SolidColorBrush(Colors.Transparent);
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }
    }
}