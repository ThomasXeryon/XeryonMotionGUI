using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Foundation;
using XeryonMotionGUI.Blocks;
using XeryonMotionGUI.Classes;

namespace XeryonMotionGUI
{
    public sealed partial class DraggableElement : UserControl, INotifyPropertyChanged
    {
        private Point _dragStartOffset;
        private bool _isDragging = false;
        private bool _isUpdatingPosition = false;
        private const double SnapThreshold = 60.0; // Adjust as needed

        public DraggableElement()
        {
            this.InitializeComponent();
            this.PointerPressed += OnPointerPressed;
            this.SizeChanged += OnSizeChanged;

            // Minimum size for the block
            this.MinWidth = 150;
            this.MinHeight = 50;
        }

        // ---------------------- INotifyPropertyChanged ----------------------
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName)
            );
        }

        // ---------------------- PositionChanged Event ----------------------
        public event EventHandler PositionChanged;
        private void NotifyPositionChanged()
        {
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        // -------------------- RunningControllers DP (if needed) --------------------
        public static readonly DependencyProperty RunningControllersProperty =
                DependencyProperty.Register(
                    nameof(RunningControllers),
                    typeof(ObservableCollection<Controller>),
                    typeof(DraggableElement),
                    new PropertyMetadata(null)
                );

        public ObservableCollection<Controller> RunningControllers
        {
            get => (ObservableCollection<Controller>)GetValue(RunningControllersProperty);
            set
            {
                SetValue(RunningControllersProperty, value);
                Debug.WriteLine($"RunningControllers set: Count = {value?.Count}");
            }
        }

        // ---------------------- (Optional) Parameters property ----------------------
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

        // ---------------------- Was a DependencyProperty, now a plain property ----------------------
        // Instead of a DP, we keep a simple C# property to avoid COMExceptions if read off the UI thread.
        private BlockBase _block;
        public BlockBase Block
        {
            get => _block;
            set
            {
                if (_block != value)
                {
                    _block = value;
                    OnPropertyChanged(nameof(Block));

                    if (_block != null)
                    {
                        // 1) Sync DraggableElement.Text with Block.Text
                        this.Text = _block.Text;

                        // 2) Let the block know its UiElement is this DraggableElement
                        _block.UiElement = this;

                        // 3) Initialize the block's controller and axis
                        _block.InitializeControllerAndAxis(this.RunningControllers);

                        Debug.WriteLine(
                            $"Block set: Text = {this.Text}, " +
                            $"SelectedController = {_block.SelectedController?.FriendlyName}, " +
                            $"SelectedAxis = {_block.SelectedAxis?.FriendlyName}"
                        );
                    }
                }
            }
        }

        // ---------------------- Text DP ----------------------
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(DraggableElement),
                new PropertyMetadata(string.Empty, OnTextChanged)
            );

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        // If DraggableElement.Text changes and there's a Block, update the Block's text as well.
        // (This is optional, depending on your scenario.)
        private static void OnTextChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            if (d is DraggableElement draggableElement && draggableElement._block != null)
            {
                draggableElement._block.Text = e.NewValue as string;
            }
        }

        // ---------------------- Next/Previous Blocks (UI references) ----------------------
        private DraggableElement _previousBlock;
        public DraggableElement PreviousBlock
        {
            get => _previousBlock;
            set
            {
                if (_previousBlock != value)
                {
                    _previousBlock = value;
                    // Sync with the underlying Block if set
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
                    // Sync with the underlying Block
                    if (_block != null)
                    {
                        _block.NextBlock = value?._block;
                    }
                }
            }
        }

        // The SnapShadow reference
        public DraggableElement SnapShadow
        {
            get; set;
        }

        // ---------------------- WorkspaceCanvas (to find pointer position) ----------------------
        private Canvas _workspaceCanvas;
        public Canvas WorkspaceCanvas
        {
            get => _workspaceCanvas;
            set
            {
                _workspaceCanvas = value;
                Debug.WriteLine(
                    $"WorkspaceCanvas set in DraggableElement: {_workspaceCanvas != null}"
                );
            }
        }

        // ---------------------- OnSizeChanged / OnPointer(Drag) events ----------------------
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // We’re ignoring changes in size
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
            _dragStartOffset = new Point(
                position.X - Canvas.GetLeft(this),
                position.Y - Canvas.GetTop(this)
            );

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

                // Move the DraggableElement
                Canvas.SetLeft(this, newLeft);
                Canvas.SetTop(this, newTop);

                // Notify
                NotifyPositionChanged();

                // Move connected blocks below
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

            // Attempt to snap to the nearest block
            SnapToNearestBlock();

            // Clean up event handlers
            WorkspaceCanvas.PointerMoved -= OnPointerMoved;
            WorkspaceCanvas.PointerReleased -= OnPointerReleased;
        }

        // This method attempts to snap the current block to the SnapShadow’s position
        private void SnapToNearestBlock()
        {
            if (WorkspaceCanvas == null || SnapShadow == null)
            {
                Debug.WriteLine("WorkspaceCanvas or SnapShadow is null in SnapToNearestBlock.");
                return;
            }

            // If shadow is not visible, no snap
            if (SnapShadow.Visibility != Visibility.Visible)
            {
                Debug.WriteLine("No shadow visible. Block will not snap.");
                return;
            }

            double snapLeft = Canvas.GetLeft(SnapShadow);
            double snapTop = Canvas.GetTop(SnapShadow);

            // Move self to shadow’s position
            Canvas.SetLeft(this, snapLeft);
            Canvas.SetTop(this, snapTop);

            // If we had a previous block, break that chain
            if (this.PreviousBlock != null)
            {
                this.PreviousBlock.NextBlock = null;
            }

            // Look for a block to snap under
            foreach (var child in WorkspaceCanvas.Children)
            {
                if (child is DraggableElement targetBlock &&
                    targetBlock != this &&
                    targetBlock != SnapShadow)
                {
                    double targetLeft = Canvas.GetLeft(targetBlock);
                    double targetTop = Canvas.GetTop(targetBlock);

                    // If we’re within SnapThreshold of the target
                    if (Math.Abs(snapLeft - targetLeft) < SnapThreshold &&
                        Math.Abs(snapTop - (targetTop + targetBlock.ActualHeight)) < SnapThreshold)
                    {
                        // Snap to that block
                        this.PreviousBlock = targetBlock;
                        targetBlock.NextBlock = this;
                        Debug.WriteLine($"Block snapped to: {targetBlock.Text}");
                        break;
                    }
                }
            }

            // Move connected blocks below
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

                // Recurse
                MoveConnectedBlocks(nextBlock, nextLeft, nextTop);
            }
        }

        // Example highlight method
        public void HighlightBlock(bool isExecuting)
        {
            if (isExecuting)
            {
                // Apply a green fade effect
                this.Background = new SolidColorBrush(Colors.LightGreen);
            }
            else
            {
                // Reset to transparent
                this.Background = new SolidColorBrush(Colors.Transparent);
            }
        }

        // Example: Handling combo box resizing in the block
        private void ComboBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                Debug.WriteLine(
                    $"ComboBox size changed: Width = {e.NewSize.Width}, Height = {e.NewSize.Height}"
                );
            }
        }
    }
}
