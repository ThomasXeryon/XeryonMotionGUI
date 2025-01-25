using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using Windows.Foundation;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using XeryonMotionGUI.Classes;
using Microsoft.UI.Xaml.Data;

namespace XeryonMotionGUI.Views
{
    public sealed partial class DemoBuilderPage : Page
    {

        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning = false;
        public ObservableCollection<Controller> RunningControllers => Controller.RunningControllers;

        private List<string> BlockTypes = new List<string>
        {
        "Step +",
        "Step -",
        "Scan Left",
        "Scan Right",
        "Move Left",
        "Move Right",
        "Wait",
        "Repeat",
        "Home",
        "Speed"
        };

        private DraggableElement _draggedBlock; // Block currently being dragged
        private Point _dragStartOffset; // Offset between cursor and block origin
        private const double SnapThreshold = 30.0; // Distance to show snap shadow

        public DemoBuilderPage()
        {
            this.InitializeComponent();
            InitializeGreenFlag();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
            InitializeBlockPalette(); // Dynamically create blocks in the palette

        }

        private void InitializeBlockPalette()
        {
            foreach (var blockType in BlockTypes)
            {
                var block = new DraggableElement
                {
                    Text = blockType,
                    Margin = new Thickness(10),
                    Width = 150 // Increase the width to 150 (or any desired value)
                };

                // Bind the RunningControllers collection to the block
                block.SetBinding(DraggableElement.RunningControllersProperty, new Binding
                {
                    Source = this,
                    Path = new PropertyPath(nameof(RunningControllers))
                });

                // Debug log for Wait blocks
                if (blockType == "Wait")
                {
                    Debug.WriteLine($"Created Wait block with WaitTime: {block.WaitTime}");
                }

                BlockPalette.Children.Add(block);

                // Attach the PointerPressed event handler
                block.PointerPressed += PaletteBlock_PointerPressed;

            }
        }

        // Initialize the top-most Start block (GreenFlagBlock)
        private void InitializeGreenFlag()
        {
            // Set the GreenFlagBlock to a fixed position
            Canvas.SetLeft(GreenFlagBlock, 50); // Adjust X position as needed
            Canvas.SetTop(GreenFlagBlock, 10); // Adjust Y position as needed
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning && _cancellationTokenSource != null)
            {
                Debug.WriteLine("Stop button clicked. Stopping execution...");
                _cancellationTokenSource.Cancel(); // Request cancellation
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Start button clicked. Executing block actions...");

            // Disable Start Button and enable Stop Button
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;

            // Initialize cancellation token
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            _isRunning = true;

            try
            {
                // Traverse the connected blocks starting from the GreenFlagBlock
                var currentBlock = GreenFlagBlock.NextBlock;
                while (currentBlock != null)
                {
                    Debug.WriteLine($"Executing block: {currentBlock.Text}");

                    if (cancellationToken.IsCancellationRequested)
                    {
                        Debug.WriteLine("Execution stopped.");
                        break; // Exit the loop if stop is requested
                    }

                    await currentBlock.ExecuteActionAsync(); // Execute the action for the current block
                    currentBlock = currentBlock.NextBlock; // Move to the next block in the chain
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during execution: {ex.Message}");
            }
            finally
            {
                // Reset buttons after execution completes
                StartButton.IsEnabled = true;
                StopButton.IsEnabled = false;
                _isRunning = false;
                _cancellationTokenSource = null;
            }
        }


        // Handle PointerPressed on the palette blocks (begin dragging from palette)
        private void PaletteBlock_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is DraggableElement paletteBlock)
            {
                // Clone the palette block for dragging
                _draggedBlock = new DraggableElement
                {
                    Text = paletteBlock.Text,
                    Width = paletteBlock.Width,
                    Height = paletteBlock.Height,
                    DataContext = paletteBlock.DataContext, // Preserve the DataContext
                    RunningControllers = paletteBlock.RunningControllers, // Preserve RunningControllers
                    SelectedController = paletteBlock.SelectedController, // Preserve SelectedController
                    SelectedAxis = paletteBlock.SelectedAxis, // Preserve SelectedAxis
                    WaitTime = paletteBlock.WaitTime // Preserve WaitTime
                };

                // Null-check _draggedBlock before subscribing to PropertyChanged
                if (_draggedBlock == null)
                {
                    Debug.WriteLine("Error: _draggedBlock is null during initialization.");
                    return; // Safeguard
                }

                // Subscribe to PropertyChanged events on the original block
                paletteBlock.PropertyChanged += (s, args) =>
                {
                    if (_draggedBlock == null)
                    {
                        Debug.WriteLine("Error: _draggedBlock is null in PropertyChanged handler.");
                        return; // Safeguard
                    }

                    if (args.PropertyName == nameof(DraggableElement.WaitTime))
                    {
                        Debug.WriteLine($"WaitTime updated in paletteBlock: {paletteBlock.WaitTime}");
                        _draggedBlock.WaitTime = paletteBlock.WaitTime;
                        Debug.WriteLine($"WaitTime updated in _draggedBlock: {_draggedBlock.WaitTime}");
                    }
                };

                // Add the clone to the workspace
                WorkspaceCanvas.Children.Add(_draggedBlock);

                // Set the initial position of the clone
                var initialPosition = e.GetCurrentPoint(WorkspaceCanvas).Position;
                Canvas.SetLeft(_draggedBlock, initialPosition.X);
                Canvas.SetTop(_draggedBlock, initialPosition.Y);

                _dragStartOffset = new Point(
                    initialPosition.X - Canvas.GetLeft(_draggedBlock),
                    initialPosition.Y - Canvas.GetTop(_draggedBlock)
                );

                // Attach drag events to the clone
                AttachDragEvents(_draggedBlock);

                // Add event handlers for PointerMoved and PointerReleased
                WorkspaceCanvas.PointerMoved += WorkspaceCanvas_PointerMoved;
                WorkspaceCanvas.PointerReleased += WorkspaceCanvas_PointerReleased;
            }
        }


        // Attach drag-and-drop events to a block
        private void AttachDragEvents(DraggableElement block)
        {
            block.PointerPressed += WorkspaceBlock_PointerPressed;
        }

        // Handle PointerPressed on workspace blocks (start dragging an existing block)
        private void WorkspaceBlock_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is DraggableElement block)
            {
                _draggedBlock = block;
                var initialPosition = e.GetCurrentPoint(WorkspaceCanvas).Position;

                _dragStartOffset = new Point(
                    initialPosition.X - Canvas.GetLeft(block),
                    initialPosition.Y - Canvas.GetTop(block)
                );

                WorkspaceCanvas.PointerMoved += WorkspaceCanvas_PointerMoved;
                WorkspaceCanvas.PointerReleased += WorkspaceCanvas_PointerReleased;
            }
        }

        // Handle PointerMoved (real-time dragging logic with shadow snapping)
        private void WorkspaceCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_draggedBlock != null)
            {
                var currentPosition = e.GetCurrentPoint(WorkspaceCanvas).Position;

                // Calculate the new position of the dragged block
                double newLeft = currentPosition.X - _dragStartOffset.X;
                double newTop = currentPosition.Y - _dragStartOffset.Y;

                // Move the dragged block
                Canvas.SetLeft(_draggedBlock, newLeft);
                Canvas.SetTop(_draggedBlock, newTop);

                // Check if the block is no longer snapped to its PreviousBlock
                if (_draggedBlock.PreviousBlock != null)
                {
                    double previousBlockLeft = Canvas.GetLeft(_draggedBlock.PreviousBlock);
                    double previousBlockTop = Canvas.GetTop(_draggedBlock.PreviousBlock);

                    // Calculate the expected position if the block were still snapped
                    double expectedLeft = previousBlockLeft;
                    double expectedTop = previousBlockTop + _draggedBlock.PreviousBlock.ActualHeight;

                    // Check if the block is no longer within the snap threshold
                    if (Math.Abs(newLeft - expectedLeft) > SnapThreshold ||
                        Math.Abs(newTop - expectedTop) > SnapThreshold)
                    {
                        // Reset connections
                        _draggedBlock.PreviousBlock.NextBlock = null;
                        _draggedBlock.PreviousBlock = null;
                    }
                }

                // Move all connected blocks below the dragged block
                MoveConnectedBlocks(_draggedBlock, newLeft, newTop);

                // Update the snap shadow
                UpdateSnapShadow(_draggedBlock);

                // Trash icon hover detection
                if (IsHoveringOverTrash(_draggedBlock))
                {
                    TrashIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                }
                else
                {
                    TrashIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray); // Default color
                }
            }
        }

        private void MoveConnectedBlocks(DraggableElement block, double parentLeft, double parentTop)
        {
            if (block.NextBlock != null)
            {
                var nextBlock = block.NextBlock;

                // Calculate the new position of the next block
                double nextLeft = parentLeft;
                double nextTop = parentTop + block.ActualHeight;

                // Move the next block
                Canvas.SetLeft(nextBlock, nextLeft);
                Canvas.SetTop(nextBlock, nextTop);

                // Recursively move blocks connected below
                MoveConnectedBlocks(nextBlock, nextLeft, nextTop);
            }
        }

        private void UpdateSnapConnections(DraggableElement block)
        {
            foreach (var child in WorkspaceCanvas.Children)
            {
                if (child is DraggableElement targetBlock && targetBlock != block && targetBlock != SnapShadow)
                {
                    double blockLeft = Canvas.GetLeft(block);
                    double blockTop = Canvas.GetTop(block);

                    double targetLeft = Canvas.GetLeft(targetBlock);
                    double targetTop = Canvas.GetTop(targetBlock);

                    // Check if the block is snapped below the target block and the target block has no NextBlock
                    if (targetBlock.NextBlock == null &&
                        Math.Abs(blockLeft - targetLeft) <= SnapThreshold &&
                        Math.Abs(blockTop - (targetTop + targetBlock.ActualHeight)) <= SnapThreshold)
                    {
                        // Update connections
                        block.PreviousBlock = targetBlock;
                        targetBlock.NextBlock = block;
                        return; // Exit after snapping to the first valid target
                    }
                }
            }

            // If no snap target is found, clear connections
            if (block.PreviousBlock != null)
            {
                block.PreviousBlock.NextBlock = null;
                block.PreviousBlock = null;
            }
        }


        // Handle PointerReleased (stop dragging and check snapping or trash)
        private void WorkspaceCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_draggedBlock != null)
            {
                // Check if dropped on the trash icon
                if (IsDroppedInTrash(_draggedBlock))
                {
                    WorkspaceCanvas.Children.Remove(_draggedBlock);
                    TrashIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
                    ResetTrashIconColorAfterDelay();
                }
                else
                {
                    // Snap the block to the shadow's position if snapping is active
                    if (SnapShadow.Visibility == Visibility.Visible)
                    {
                        // Move the dragged block to the shadow's position
                        Canvas.SetLeft(_draggedBlock, Canvas.GetLeft(SnapShadow));
                        Canvas.SetTop(_draggedBlock, Canvas.GetTop(SnapShadow));

                        // Update connections for the dragged block
                        UpdateSnapConnections(_draggedBlock);

                        // Move all connected blocks below the dragged block
                        MoveConnectedBlocks(_draggedBlock, Canvas.GetLeft(_draggedBlock), Canvas.GetTop(_draggedBlock));
                    }
                }

                // Hide the snap shadow
                SnapShadow.Visibility = Visibility.Collapsed;

                WorkspaceCanvas.PointerMoved -= WorkspaceCanvas_PointerMoved;
                WorkspaceCanvas.PointerReleased -= WorkspaceCanvas_PointerReleased;

                _draggedBlock = null;
            }
        }

        // Update the position and visibility of the snap shadow
        private void UpdateSnapShadow(DraggableElement block)
        {
            DraggableElement snapTarget = null; // Keep track of the closest block to snap to
            double minDistance = double.MaxValue;

            foreach (var child in WorkspaceCanvas.Children)
            {
                // Ensure the child is a valid snapping target (not the dragged block, the shadow itself, or already snapped)
                if (child is DraggableElement targetBlock && targetBlock != block && targetBlock != SnapShadow)
                {
                    double blockLeft = Canvas.GetLeft(block);
                    double blockTop = Canvas.GetTop(block);

                    double targetLeft = Canvas.GetLeft(targetBlock);
                    double targetTop = Canvas.GetTop(targetBlock);

                    // Adjust for margins/padding (if any)
                    double marginLeft = targetBlock.Margin.Left;
                    double marginTop = targetBlock.Margin.Top;

                    // Calculate distances
                    double deltaX = Math.Abs(blockLeft - (targetLeft + marginLeft));
                    double deltaY = Math.Abs(blockTop - (targetTop + targetBlock.ActualHeight + marginTop)); // Snap below target
                    double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY); // Total distance

                    // Check if within snapping threshold and closer than any previously found block
                    if (deltaX <= SnapThreshold && deltaY <= SnapThreshold && distance < minDistance)
                    {
                        snapTarget = targetBlock;
                        minDistance = distance;
                    }
                }
            }

            // Explicitly check the GreenFlagBlock as a snapping target
            double greenFlagLeft = Canvas.GetLeft(GreenFlagBlock);
            double greenFlagTop = Canvas.GetTop(GreenFlagBlock);
            double greenMarginLeft = GreenFlagBlock.Margin.Left;
            double greenMarginTop = GreenFlagBlock.Margin.Top;

            double greenDeltaX = Math.Abs(Canvas.GetLeft(block) - (greenFlagLeft + greenMarginLeft));
            double greenDeltaY = Math.Abs(Canvas.GetTop(block) - (greenFlagTop + GreenFlagBlock.ActualHeight + greenMarginTop));
            double greenDistance = Math.Sqrt(greenDeltaX * greenDeltaX + greenDeltaY * greenDeltaY);

            if (greenDeltaX <= SnapThreshold && greenDeltaY <= SnapThreshold && greenDistance < minDistance &&
                GreenFlagBlock.NextBlock == null) // Ensure the GreenFlagBlock is available
            {
                snapTarget = GreenFlagBlock;
                minDistance = greenDistance;
            }

            if (snapTarget != null)
            {
                // Align the shadow directly below the snapping target
                double targetLeft = Canvas.GetLeft(snapTarget) + snapTarget.Margin.Left;
                double targetTop = Canvas.GetTop(snapTarget) + snapTarget.ActualHeight + snapTarget.Margin.Top;

                // Update the shadow block
                SnapShadow.Visibility = Visibility.Visible;
                SnapShadow.Text = block.Text;
                SnapShadow.Width = block.ActualWidth;
                SnapShadow.Height = block.ActualHeight;
                SnapShadow.Background = new SolidColorBrush(Microsoft.UI.Colors.DarkGray);

                // Correct alignment
                Canvas.SetLeft(SnapShadow, targetLeft);
                Canvas.SetTop(SnapShadow, targetTop);
            }
            else
            {
                // Hide the shadow if no snap target is found
                SnapShadow.Visibility = Visibility.Collapsed;
            }
        }

        // Check if the block is hovering over the trash area
        private bool IsHoveringOverTrash(DraggableElement block)
        {
            // Get the block's position and size
            double blockLeft = Canvas.GetLeft(block);
            double blockTop = Canvas.GetTop(block);
            double blockRight = blockLeft + block.ActualWidth;
            double blockBottom = blockTop + block.ActualHeight;

            // Get the trash icon's bounds relative to the workspace
            Rect trashBounds = TrashIcon.TransformToVisual(WorkspaceCanvas)
                .TransformBounds(new Rect(0, 0, TrashIcon.ActualWidth, TrashIcon.ActualHeight));

            // Check if the block's bounds overlap with the trash bounds
            return blockRight > trashBounds.X &&
                   blockLeft < trashBounds.X + trashBounds.Width &&
                   blockBottom > trashBounds.Y &&
                   blockTop < trashBounds.Y + trashBounds.Height;
        }

        // Check if the block is dropped in the trash area
        private bool IsDroppedInTrash(DraggableElement block)
        {
            // Reuse IsHoveringOverTrash logic
            return IsHoveringOverTrash(block);
        }

        // Reset the trash icon color to gray after a short delay
        private async void ResetTrashIconColorAfterDelay()
        {
            await Task.Delay(500); // 500ms delay
            TrashIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray); // Reset to default
        }
    }
}
