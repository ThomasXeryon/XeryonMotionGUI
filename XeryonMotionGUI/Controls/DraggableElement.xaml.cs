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

            // Set minimum size for the block
            this.MinWidth = 150;
            this.MinHeight = 50;
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

        // Dependency properties
        public static readonly DependencyProperty RunningControllersProperty =
                DependencyProperty.Register(
                    nameof(RunningControllers),
                    typeof(ObservableCollection<Controller>),
                    typeof(DraggableElement),
                    new PropertyMetadata(null));

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

        public static readonly DependencyProperty BlockProperty =
            DependencyProperty.Register(
                nameof(Block),
                typeof(BlockBase),
                typeof(DraggableElement),
                new PropertyMetadata(null, OnBlockChanged));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(DraggableElement),
                new PropertyMetadata(string.Empty, OnTextChanged));

        public static readonly DependencyProperty BackgroundProperty =
    DependencyProperty.Register(
        nameof(Background),
        typeof(Brush),
        typeof(DraggableElement),
        new PropertyMetadata(new SolidColorBrush(Colors.Transparent)));

        public Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
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
                    // Sync with BlockBase level
                    if (Block != null)
                    {
                        Block.PreviousBlock = value?.Block;
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
                    // Sync with BlockBase level
                    if (Block != null)
                    {
                        Block.NextBlock = value?.Block;
                    }
                }
            }
        }    // The block below this one
        public DraggableElement SnapShadow
        {
            get; set;
        }    // Reference to the snap shadow

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // No need to call UpdateHeight anymore
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

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DraggableElement draggableElement && draggableElement.Block != null)
            {
                draggableElement.Block.Text = e.NewValue as string; // Update the Block.Text property
            }
        }

        public BlockBase Block
        {
            get => (BlockBase)GetValue(BlockProperty);
            set
            {
                SetValue(BlockProperty, value);
                OnPropertyChanged(nameof(Block));
            }
        }

        private static void OnBlockChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DraggableElement draggableElement)
            {
                var newBlock = e.NewValue as BlockBase;
                if (newBlock != null)
                {
                    // Synchronize Block.Text with DraggableElement.Text
                    draggableElement.Text = newBlock.Text;

                    // Set the UiElement property of the block to this DraggableElement
                    newBlock.UiElement = draggableElement;

                    // Initialize the controller and axis
                    newBlock.InitializeControllerAndAxis(draggableElement.RunningControllers);

                    Debug.WriteLine($"Block set: Text = {draggableElement.Text}, SelectedController = {newBlock.SelectedController?.FriendlyName}, SelectedAxis = {newBlock.SelectedAxis?.FriendlyName}");
                }
            }
        }

        // Handles dragging behavior
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

                // Update the position of the dragged block
                Canvas.SetLeft(this, newLeft);
                Canvas.SetTop(this, newTop);

                this.NotifyPositionChanged();


                // Move all connected blocks below this one
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

        // Snap to the nearest block below
        private void SnapToNearestBlock()
        {
            if (WorkspaceCanvas == null || SnapShadow == null)
            {
                Debug.WriteLine("WorkspaceCanvas or SnapShadow is null in SnapToNearestBlock.");
                return;
            }

            // Only snap if the shadow is visible
            if (SnapShadow.Visibility != Visibility.Visible)
            {
                Debug.WriteLine("No shadow visible. Block will not snap.");
                return;
            }

            // Calculate the target position for snapping (based on the shadow's position)
            double snapLeft = Canvas.GetLeft(SnapShadow);
            double snapTop = Canvas.GetTop(SnapShadow);

            // Move the block to the shadow's position
            Canvas.SetLeft(this, snapLeft);
            Canvas.SetTop(this, snapTop);

            // Update connections
            if (this.PreviousBlock != null)
            {
                this.PreviousBlock.NextBlock = null; // Detach from previous block
            }

            // Find the block that corresponds to the shadow's position
            foreach (var child in WorkspaceCanvas.Children)
            {
                if (child is DraggableElement targetBlock &&
                    targetBlock != this &&
                    targetBlock != SnapShadow)
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

            // Move all connected blocks below this one
            MoveConnectedBlocks(this, snapLeft, snapTop);
        }

        // Move all connected blocks below this one
        private void MoveConnectedBlocks(DraggableElement block, double parentLeft, double parentTop)
        {
            if (block.NextBlock != null)
            {
                var nextBlock = block.NextBlock;
                double nextLeft = parentLeft;
                double nextTop = parentTop + block.ActualHeight;

                // Update the position of the next block
                Canvas.SetLeft(nextBlock, nextLeft);
                Canvas.SetTop(nextBlock, nextTop);
                nextBlock.NotifyPositionChanged();


                // Recursively move blocks connected below
                MoveConnectedBlocks(nextBlock, nextLeft, nextTop);
            }
        }

        // Handle ComboBox size changes
        private void ComboBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                Debug.WriteLine($"ComboBox size changed: Width = {e.NewSize.Width}, Height = {e.NewSize.Height}");
            }
        }

        public void HighlightBlock(bool isExecuting)
        {
            if (isExecuting)
            {
                // Apply a green fade effect
                this.Background = new SolidColorBrush(Colors.LightGreen);
            }
            else
            {
                // Reset to the default background color
                this.Background = new SolidColorBrush(Colors.Transparent);
            }
        }
    }
}