using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Windows.Foundation;
using XeryonMotionGUI.Classes;
using XeryonMotionGUI.Blocks;
using System.Threading;

namespace XeryonMotionGUI.Views
{
    public sealed partial class DemoBuilderPage : Page
    {
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning = false;
        private const double SnapThreshold = 30.0;
        private bool _isUpdatingPosition = false;

        private DraggableElement _draggedBlock;
        private Point _dragStartOffset;

        public ObservableCollection<Controller> RunningControllers => Controller.RunningControllers;

        private readonly List<string> BlockTypes = new()
        {
            "Step +", "Step -", "Wait", "Repeat"
        };

        public DemoBuilderPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
            InitializeGreenFlag();
            InitializeBlockPalette();
        }

        // Position the GreenFlagBlock
        private void InitializeGreenFlag()
        {
            Canvas.SetLeft(GreenFlagBlock, 50);
            Canvas.SetTop(GreenFlagBlock, 10);

            // Assign a StartBlock to the GreenFlagBlock
            GreenFlagBlock.Block = new StartBlock();
            Debug.WriteLine("Assigned StartBlock to GreenFlagBlock.");
        }

        // Create palette blocks
        private void InitializeBlockPalette()
        {
            foreach (var blockType in BlockTypes)
            {
                // Create a DraggableElement for the palette
                var block = new DraggableElement
                {
                    Block = BlockFactory.CreateBlock(blockType), // Use BlockFactory to create the block
                    Text = blockType,
                    Margin = new Thickness(10),
                    Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray)
                };

                // Attach pointer pressed for palette logic
                block.PointerPressed += PaletteBlock_PointerPressed;
                BlockPalette.Children.Add(block);

                Debug.WriteLine($"Palette block '{blockType}' added.");
            }
        }

        // When user clicks a palette block to drag it out
        private void PaletteBlock_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is DraggableElement paletteBlock)
            {
                // 1. Create a brand-new block instance
                var newBlockInstance = BlockFactory.CreateBlock(paletteBlock.Text);

                // 2. Clone it into a new DraggableElement
                _draggedBlock = new DraggableElement
                {
                    Block = newBlockInstance, // Assign the new block
                    Text = paletteBlock.Text,
                    WorkspaceCanvas = WorkspaceCanvas,
                    SnapShadow = SnapShadow,
                    RunningControllers = this.RunningControllers
                };

                // 3. Add the clone to the workspace (if it doesn't already exist)
                if (!WorkspaceCanvas.Children.Contains(_draggedBlock))
                {
                    WorkspaceCanvas.Children.Add(_draggedBlock);
                }

                // 4. Set the initial position
                var initialPosition = e.GetCurrentPoint(WorkspaceCanvas).Position;
                Canvas.SetLeft(_draggedBlock, initialPosition.X);
                Canvas.SetTop(_draggedBlock, initialPosition.Y);

                _dragStartOffset = new Point(
                    initialPosition.X - Canvas.GetLeft(_draggedBlock),
                    initialPosition.Y - Canvas.GetTop(_draggedBlock)
                );

                // 5. Attach drag events to the clone
                AttachDragEvents(_draggedBlock);

                // 6. Register PointerMoved and PointerReleased for canvas-level dragging
                WorkspaceCanvas.PointerMoved += WorkspaceCanvas_PointerMoved;
                WorkspaceCanvas.PointerReleased += WorkspaceCanvas_PointerReleased;

                Debug.WriteLine($"Cloned block '{_draggedBlock.Text}' at {initialPosition}.");
            }
        }

        private void AttachDragEvents(DraggableElement block)
        {
            // So we can drag existing blocks
            block.PointerPressed += WorkspaceBlock_PointerPressed;
        }

        // Start dragging an existing block
        private void WorkspaceBlock_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is DraggableElement block)
            {
                _draggedBlock = block;
                var position = e.GetCurrentPoint(WorkspaceCanvas).Position;

                _dragStartOffset = new Point(
                    position.X - Canvas.GetLeft(block),
                    position.Y - Canvas.GetTop(block)
                );

                WorkspaceCanvas.PointerMoved += WorkspaceCanvas_PointerMoved;
                WorkspaceCanvas.PointerReleased += WorkspaceCanvas_PointerReleased;
            }
        }

        // Drag real-time
        private void WorkspaceCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_draggedBlock != null)
            {
                var currentPosition = e.GetCurrentPoint(WorkspaceCanvas).Position;
                double newLeft = currentPosition.X - _dragStartOffset.X;
                double newTop = currentPosition.Y - _dragStartOffset.Y;

                // Move the dragged block
                Canvas.SetLeft(_draggedBlock, newLeft);
                Canvas.SetTop(_draggedBlock, newTop);

                // Check if block is no longer within snap threshold of its previous block
                if (_draggedBlock.PreviousBlock != null)
                {
                    double previousBlockLeft = Canvas.GetLeft(_draggedBlock.PreviousBlock);
                    double previousBlockTop = Canvas.GetTop(_draggedBlock.PreviousBlock);

                    // If we've moved far enough away, break the chain
                    double expectedLeft = previousBlockLeft;
                    double expectedTop = previousBlockTop + _draggedBlock.PreviousBlock.ActualHeight;

                    if (Math.Abs(newLeft - expectedLeft) > SnapThreshold ||
                        Math.Abs(newTop - expectedTop) > SnapThreshold)
                    {
                        // UI-level: remove the "next" pointer from the previous block
                        _draggedBlock.PreviousBlock.NextBlock = null;

                        // Block-level: remove the "next" pointer from the previous block's underlying block
                        _draggedBlock.PreviousBlock.Block.NextBlock = null;

                        // Block-level: remove the "previous" pointer on *this* block's underlying block
                        _draggedBlock.Block.PreviousBlock = null;

                        // UI-level: remove the "previous" pointer on the DraggableElement
                        _draggedBlock.PreviousBlock = null;
                    }
                }

                // Move all connected blocks below the dragged block
                MoveConnectedBlocks(_draggedBlock, newLeft, newTop);

                // Update SnapShadow
                UpdateSnapShadow(_draggedBlock);
            }
        }

        // Handle PointerReleased to delete the block if hovering over trash
        private void WorkspaceCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_draggedBlock != null)
            {
                Debug.WriteLine($"PointerReleased: Block '{_draggedBlock.Text}' dropped.");

                if (IsDroppedInTrash(_draggedBlock))
                {
                    Debug.WriteLine($"Block '{_draggedBlock.Text}' dropped in trash.");
                    // Remove from UI
                    WorkspaceCanvas.Children.Remove(_draggedBlock);

                    // (Optionally) also clear block-logic references here
                    _draggedBlock.Block.PreviousBlock = null;
                    _draggedBlock.Block.NextBlock = null;
                }
                else
                {
                    Debug.WriteLine($"Block '{_draggedBlock.Text}' snapped to position.");
                    // 1) Snap the block to the nearest position
                    SnapToNearestBlock(_draggedBlock);

                    // 2) Update connections
                    UpdateSnapConnections(_draggedBlock);

                    // 3) Move any connected blocks below it
                    MoveConnectedBlocks(_draggedBlock, Canvas.GetLeft(_draggedBlock), Canvas.GetTop(_draggedBlock));
                }

                //DebugBlockConnections();

                // Hide snap shadow
                SnapShadow.Visibility = Visibility.Collapsed;

                WorkspaceCanvas.PointerMoved -= WorkspaceCanvas_PointerMoved;
                WorkspaceCanvas.PointerReleased -= WorkspaceCanvas_PointerReleased;

                _draggedBlock = null;
            }

            RemoveDuplicateBlocks();
        }

        private void SnapToNearestBlock(DraggableElement block)
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
            Canvas.SetLeft(block, snapLeft);
            Canvas.SetTop(block, snapTop);

            // Update connections
            if (block.PreviousBlock != null)
            {
                block.PreviousBlock.NextBlock = null; // Detach from previous block
            }

            // Find the block that corresponds to the shadow's position
            foreach (var child in WorkspaceCanvas.Children)
            {
                if (child is DraggableElement targetBlock &&
                    targetBlock != block &&
                    targetBlock != SnapShadow)
                {
                    double targetLeft = Canvas.GetLeft(targetBlock);
                    double targetTop = Canvas.GetTop(targetBlock);

                    if (Math.Abs(snapLeft - targetLeft) < SnapThreshold &&
                        Math.Abs(snapTop - (targetTop + targetBlock.ActualHeight)) < SnapThreshold)
                    {
                        block.PreviousBlock = targetBlock;
                        targetBlock.NextBlock = block;
                        Debug.WriteLine($"Block snapped to: {targetBlock.Text}");
                        break;
                    }
                }
            }

            // Move all connected blocks below this one
            MoveConnectedBlocks(block, snapLeft, snapTop);
        }

        private void RemoveDuplicateBlocks()
        {
            var uniqueBlocks = new HashSet<DraggableElement>();

            for (int i = WorkspaceCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (WorkspaceCanvas.Children[i] is DraggableElement block)
                {
                    if (uniqueBlocks.Contains(block))
                    {
                        WorkspaceCanvas.Children.RemoveAt(i);
                        Debug.WriteLine($"Removed duplicate block: '{block.Text}'");
                    }
                    else
                    {
                        uniqueBlocks.Add(block);
                    }
                }
            }
        }

        // Update the snap shadow position
        private void UpdateSnapShadow(DraggableElement block)
        {
            DraggableElement snapTarget = null; // Keep track of the closest block to snap to
            double minDistance = double.MaxValue;

            foreach (var child in WorkspaceCanvas.Children)
            {
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

        // Ensure only one block can snap below a target
        private void UpdateSnapConnections(DraggableElement block)
        {
            if (block.Block is RepeatBlock repeatBlock)
            {
                // Find the start block (topmost block in the sequence)
                var startBlock = block.PreviousBlock?.Block;
                while (startBlock?.PreviousBlock != null)
                {
                    startBlock = startBlock.PreviousBlock;
                }

                // Find the end block (block closest to the RepeatBlock)
                var endBlock = block.PreviousBlock?.Block;

                // Set the start and end blocks
                repeatBlock.StartBlock = startBlock;
                repeatBlock.EndBlock = endBlock;

                Debug.WriteLine($"[RepeatBlock] StartBlock = {startBlock?.Text}, EndBlock = {endBlock?.Text}");
            }
            // If it's already snapped, skip
            if (block.PreviousBlock != null) return;

            foreach (var child in WorkspaceCanvas.Children)
            {
                if (child is DraggableElement targetBlock && targetBlock != block && targetBlock != SnapShadow)
                {
                    double blockLeft = Canvas.GetLeft(block);
                    double blockTop = Canvas.GetTop(block);
                    double targetLeft = Canvas.GetLeft(targetBlock);
                    double targetTop = Canvas.GetTop(targetBlock);

                    bool xSnapped = Math.Abs(blockLeft - targetLeft) <= SnapThreshold;
                    bool ySnapped = Math.Abs(blockTop - (targetTop + targetBlock.ActualHeight)) <= SnapThreshold;

                    if (xSnapped && ySnapped && targetBlock.NextBlock == null)
                    {
                        // 1) UI-level references
                        block.PreviousBlock = targetBlock;
                        targetBlock.NextBlock = block;

                        // 2) Logic-level references
                        block.Block.PreviousBlock = targetBlock.Block;
                        targetBlock.Block.NextBlock = block.Block;

                        Debug.WriteLine(
                            $"Snapped '{block.Text}' to '{targetBlock.Text}'. " +
                            $"UI References: {block.PreviousBlock?.Text}, {targetBlock.NextBlock?.Text}; " +
                            $"Logic References: {block.Block.PreviousBlock?.Text}, {targetBlock.Block.NextBlock?.Text}");

                        return;
                    }
                }
            }

            // If no snap found, ensure we clear references
            if (block.PreviousBlock != null)
            {
                // 1) UI-level
                block.PreviousBlock.NextBlock = null;
                block.PreviousBlock = null;

                // 2) Logic-level
                block.Block.PreviousBlock = null;
                // Assuming targetBlock.Block.NextBlock was already set to null above
            }
        }

        private void MoveConnectedBlocks(DraggableElement block, double parentLeft, double parentTop)
        {
            if (block.NextBlock != null)
            {
                var nextBlock = block.NextBlock;

                Debug.WriteLine($"Moving connected block: {nextBlock.Text}");
                Debug.WriteLine($"Block '{nextBlock.Text}': PreviousBlock = {nextBlock.PreviousBlock?.Text}, NextBlock = {nextBlock.NextBlock?.Text}");

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

        // Execution logic
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
                // Execute the StartBlock first
                if (GreenFlagBlock.Block != null)
                {
                    Debug.WriteLine("Executing StartBlock...");
                    await GreenFlagBlock.Block.ExecuteAsync(cancellationToken);
                }

                // Traverse from the GreenFlag's NextBlock (logic chain)
                var currentBlock = GreenFlagBlock.Block.NextBlock; // Use BlockBase references
                while (currentBlock != null && !cancellationToken.IsCancellationRequested)
                {
                    Debug.WriteLine($"Executing block: {currentBlock.Text}");
                    Debug.WriteLine($"Block '{currentBlock.Text}': PreviousBlock = {currentBlock.PreviousBlock?.Text ?? "null"}, NextBlock = {currentBlock.NextBlock?.Text ?? "null"}");

                    // Highlight the block
                    if (currentBlock.UiElement != null)
                    {
                        currentBlock.UiElement.HighlightBlock(true);
                    }

                    // Call the block's ExecuteAsync method
                    await currentBlock.ExecuteAsync(cancellationToken);

                    // Remove the highlight
                    if (currentBlock.UiElement != null)
                    {
                        currentBlock.UiElement.HighlightBlock(false);
                    }

                    // Move to the next block in the chain
                    currentBlock = currentBlock.NextBlock;
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

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning && _cancellationTokenSource != null)
            {
                Debug.WriteLine("Stop button clicked. Stopping execution...");
                _cancellationTokenSource.Cancel(); // Request cancellation
            }
        }

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

        private bool IsDroppedInTrash(DraggableElement block)
        {
            Debug.WriteLine($"Checking if block '{block.Text}' is dropped in trash...");
            return IsHoveringOverTrash(block);
        }

        private async void ResetTrashIconColorAfterDelay()
        {
            await Task.Delay(500); // 500ms delay
            TrashIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray); // Reset to default
        }

        private void DebugBlockConnections()
        {
            Debug.WriteLine("Debugging block connections in the workspace:");

            foreach (var child in WorkspaceCanvas.Children)
            {
                if (child is DraggableElement block && block != SnapShadow) // Exclude SnapShadow
                {
                    string uiPrev = block.PreviousBlock?.Text ?? "null";
                    string uiNext = block.NextBlock?.Text ?? "null";
                    string logicPrev = block.Block.PreviousBlock?.Text ?? "null";
                    string logicNext = block.Block.NextBlock?.Text ?? "null";

                    Debug.WriteLine($"Block '{block.Text}': UI Previous = {uiPrev}, UI Next = {uiNext}; Logic Previous = {logicPrev}, Logic Next = {logicNext}");
                }
            }

            Debug.WriteLine("End of block connections debug.");
        }
    }
}