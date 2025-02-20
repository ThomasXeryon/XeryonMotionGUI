using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Dispatching;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Windows.Foundation;
using XeryonMotionGUI.Classes;
using XeryonMotionGUI.Blocks;
using System.Threading;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace XeryonMotionGUI.Views
{
    public sealed partial class DemoBuilderPage : Page
    {
        private CancellationTokenSource _executionCts;
        private bool _isRunning = false;
        private const double SnapThreshold = 30.0;

        private DraggableElement _draggedBlock;
        private Point _dragStartOffset;

        public ObservableCollection<Controller> RunningControllers => Controller.RunningControllers;
        private Dictionary<RepeatBlock, Arrow> _repeatArrows = new Dictionary<RepeatBlock, Arrow>();

        private readonly List<string> BlockTypes = new()
        {
            "Step", "Wait", "Repeat", "Move", "Home", "Stop", "Scan", "Index", "Log", "Edit Parameter"
        };

        public DemoBuilderPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
            InitializeGreenFlag();
            InitializeBlockPalette();
        }

        // Initialize the GreenFlagBlock
        private void InitializeGreenFlag()
        {
            Canvas.SetLeft(GreenFlagBlock, 50);
            Canvas.SetTop(GreenFlagBlock, 10);

            GreenFlagBlock.Block = new StartBlock();
            GreenFlagBlock.Block.SetDispatcherQueue(DispatcherQueue.GetForCurrentThread());

            Debug.WriteLine("Assigned StartBlock to GreenFlagBlock.");
        }

        // Initialize the block palette
        private void InitializeBlockPalette()
        {
            foreach (var blockType in BlockTypes)
            {
                var block = new DraggableElement
                {
                    Block = BlockFactory.CreateBlock(blockType, this.RunningControllers, DispatcherQueue.GetForCurrentThread()),
                    Text = blockType,
                    Margin = new Thickness(10),
                    Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
                };

                block.PointerPressed += PaletteBlock_PointerPressed;
                block.PositionChanged += Block_PositionChanged;
                BlockPalette.Children.Add(block);

                Debug.WriteLine($"Palette block '{blockType}' added.");
            }
        }

        // Handle position changes to update arrows
        private void Block_PositionChanged(object sender, EventArgs e)
        {
            UpdateAllArrows();
        }

        // Handle palette block drag
        private void PaletteBlock_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is DraggableElement paletteBlock)
            {
                var newBlockInstance = BlockFactory.CreateBlock(paletteBlock.Text, this.RunningControllers, DispatcherQueue.GetForCurrentThread());
                newBlockInstance.SetDispatcherQueue(DispatcherQueue.GetForCurrentThread());

                _draggedBlock = new DraggableElement
                {
                    Block = newBlockInstance,
                    Text = paletteBlock.Text,
                    WorkspaceCanvas = WorkspaceCanvas,
                    SnapShadow = SnapShadow,
                    RunningControllers = this.RunningControllers
                };

                if (!WorkspaceCanvas.Children.Contains(_draggedBlock))
                {
                    WorkspaceCanvas.Children.Add(_draggedBlock);
                }

                var initialPosition = e.GetCurrentPoint(WorkspaceCanvas).Position;
                Canvas.SetLeft(_draggedBlock, initialPosition.X);
                Canvas.SetTop(_draggedBlock, initialPosition.Y);

                _dragStartOffset = new Point(
                    initialPosition.X - Canvas.GetLeft(_draggedBlock),
                    initialPosition.Y - Canvas.GetTop(_draggedBlock)
                );

                AttachDragEvents(_draggedBlock);
                WorkspaceCanvas.PointerMoved += WorkspaceCanvas_PointerMoved;
                WorkspaceCanvas.PointerReleased += WorkspaceCanvas_PointerReleased;

                Debug.WriteLine($"Cloned block '{_draggedBlock.Text}' at {initialPosition}.");
            }
        }

        // Attach drag events to a block
        private void AttachDragEvents(DraggableElement block)
        {
            block.PointerPressed += WorkspaceBlock_PointerPressed;
            if (block.Block is RepeatBlock repeatBlock)
            {
                repeatBlock.PropertyChanged += RepeatBlock_PropertyChanged;
            }
        }

        private void RepeatBlock_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is RepeatBlock repeatBlock && e.PropertyName == nameof(RepeatBlock.BlocksToRepeat))
            {
                Debug.WriteLine($"BlocksToRepeat changed for RepeatBlock: {repeatBlock.Text}");

                // Recalculate StartBlock and EndBlock
                repeatBlock.StartBlock = FindStartBlock(repeatBlock);
                repeatBlock.EndBlock = FindEndBlock(repeatBlock);

                Debug.WriteLine($"[RepeatBlock] Updated StartBlock = {repeatBlock.StartBlock?.Text}, EndBlock = {repeatBlock.EndBlock?.Text}");

                // Update the arrow for the RepeatBlock
                UpdateArrowForRepeatBlock(repeatBlock);
            }
        }

        private void UpdateArrowForRepeatBlock(RepeatBlock repeatBlock)
        {
            if (repeatBlock.EndBlock != null)
            {
                // Create or update the arrow for the RepeatBlock
                if (!_repeatArrows.ContainsKey(repeatBlock))
                {
                    var arrow = new Arrow();
                    arrow.AddToCanvas(WorkspaceCanvas);
                    _repeatArrows.Add(repeatBlock, arrow);
                }

                // Get the source and target elements for the arrow
                var sourceElement = WorkspaceCanvas.Children
                    .OfType<DraggableElement>()
                    .FirstOrDefault(de => de.Block == repeatBlock);

                var endBlockElement = WorkspaceCanvas.Children
                    .OfType<DraggableElement>()
                    .FirstOrDefault(de => de.Block == repeatBlock.EndBlock);

                if (sourceElement != null && endBlockElement != null)
                {
                    // Update the arrow's position
                    var arrowToUpdate = _repeatArrows[repeatBlock];
                    arrowToUpdate.UpdatePosition(sourceElement, endBlockElement);
                }
            }
            else
            {
                // Remove the arrow if the EndBlock is no longer valid
                if (_repeatArrows.ContainsKey(repeatBlock))
                {
                    _repeatArrows[repeatBlock].RemoveFromCanvas(WorkspaceCanvas);
                    _repeatArrows.Remove(repeatBlock);
                }
            }
        }

        // Handle workspace block drag
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

        // Handle real-time dragging
        private void WorkspaceCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_draggedBlock != null)
            {
                var currentPosition = e.GetCurrentPoint(WorkspaceCanvas).Position;
                double newLeft = currentPosition.X - _dragStartOffset.X;
                double newTop = currentPosition.Y - _dragStartOffset.Y;

                Canvas.SetLeft(_draggedBlock, newLeft);
                Canvas.SetTop(_draggedBlock, newTop);

                if (_draggedBlock.PreviousBlock != null)
                {
                    double previousBlockLeft = Canvas.GetLeft(_draggedBlock.PreviousBlock);
                    double previousBlockTop = Canvas.GetTop(_draggedBlock.PreviousBlock);

                    double expectedLeft = previousBlockLeft;
                    double expectedTop = previousBlockTop + _draggedBlock.PreviousBlock.ActualHeight;

                    if (Math.Abs(newLeft - expectedLeft) > SnapThreshold ||
                        Math.Abs(newTop - expectedTop) > SnapThreshold)
                    {
                        _draggedBlock.PreviousBlock.NextBlock = null;
                        _draggedBlock.PreviousBlock.Block.NextBlock = null;
                        _draggedBlock.Block.PreviousBlock = null;
                        _draggedBlock.PreviousBlock = null;
                    }
                }

                MoveConnectedBlocks(_draggedBlock, newLeft, newTop);
                UpdateAllArrows();
                UpdateSnapShadow(_draggedBlock);
            }
        }

        // Handle block release
        private void WorkspaceCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_draggedBlock != null)
            {
                Debug.WriteLine($"PointerReleased: Block '{_draggedBlock.Text}' dropped.");

                if (IsDroppedInTrash(_draggedBlock))
                {
                    Debug.WriteLine($"Block '{_draggedBlock.Text}' dropped in trash.");
                    RemoveArrowsForBlock(_draggedBlock);
                    WorkspaceCanvas.Children.Remove(_draggedBlock);
                    _draggedBlock.Block.PreviousBlock = null;
                    _draggedBlock.Block.NextBlock = null;
                }
                else
                {
                    SnapToNearestBlock(_draggedBlock);
                    UpdateSnapConnections(_draggedBlock);
                    MoveConnectedBlocks(_draggedBlock, Canvas.GetLeft(_draggedBlock), Canvas.GetTop(_draggedBlock));
                }

                SnapShadow.Visibility = Visibility.Collapsed;
                WorkspaceCanvas.PointerMoved -= WorkspaceCanvas_PointerMoved;
                WorkspaceCanvas.PointerReleased -= WorkspaceCanvas_PointerReleased;
                _draggedBlock = null;
            }

            RemoveDuplicateBlocks();
        }

        // Remove arrows for a block
        private void RemoveArrowsForBlock(DraggableElement block)
        {
            var arrowsToRemove = _repeatArrows
                .Where(kvp => kvp.Key.EndBlock?.UiElement == block || kvp.Key.StartBlock?.UiElement == block)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var repeatBlock in arrowsToRemove)
            {
                _repeatArrows[repeatBlock].RemoveFromCanvas(WorkspaceCanvas);
                _repeatArrows.Remove(repeatBlock);
                repeatBlock.PropertyChanged -= RepeatBlock_PropertyChanged;
            }
        }

        // Update all arrows
        private void UpdateAllArrows()
        {
            foreach (var repeatBlock in _repeatArrows.Keys.ToList())
            {
                var arrowToUpdate = _repeatArrows[repeatBlock];
                var sourceElement = WorkspaceCanvas.Children
                    .OfType<DraggableElement>()
                    .FirstOrDefault(de => de.Block == repeatBlock);

                var endBlockElement = WorkspaceCanvas.Children
                    .OfType<DraggableElement>()
                    .FirstOrDefault(de => de.Block == repeatBlock.EndBlock);

                if (sourceElement != null && endBlockElement != null)
                {
                    arrowToUpdate.UpdatePosition(sourceElement, endBlockElement);
                }
            }
        }

        // Snap a block to the nearest position
        private void SnapToNearestBlock(DraggableElement block)
        {
            if (WorkspaceCanvas == null || SnapShadow == null || SnapShadow.Visibility != Visibility.Visible)
            {
                return;
            }

            double snapLeft = Canvas.GetLeft(SnapShadow);
            double snapTop = Canvas.GetTop(SnapShadow);

            Canvas.SetLeft(block, snapLeft);
            Canvas.SetTop(block, snapTop);

            if (block.PreviousBlock != null)
            {
                block.PreviousBlock.NextBlock = null;
            }

            foreach (var child in WorkspaceCanvas.Children)
            {
                if (child is DraggableElement targetBlock && targetBlock != block && targetBlock != SnapShadow)
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

            MoveConnectedBlocks(block, snapLeft, snapTop);
        }

        // Remove duplicate blocks
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
            DraggableElement snapTarget = null;
            double minDistance = double.MaxValue;

            foreach (var child in WorkspaceCanvas.Children)
            {
                if (child is DraggableElement targetBlock && targetBlock != block && targetBlock != SnapShadow)
                {
                    double blockLeft = Canvas.GetLeft(block);
                    double blockTop = Canvas.GetTop(block);
                    double targetLeft = Canvas.GetLeft(targetBlock);
                    double targetTop = Canvas.GetTop(targetBlock);

                    double deltaX = Math.Abs(blockLeft - (targetLeft + targetBlock.Margin.Left));
                    double deltaY = Math.Abs(blockTop - (targetTop + targetBlock.ActualHeight + targetBlock.Margin.Top));
                    double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

                    if (deltaX <= SnapThreshold && deltaY <= SnapThreshold && distance < minDistance)
                    {
                        snapTarget = targetBlock;
                        minDistance = distance;
                    }
                }
            }

            if (snapTarget != null)
            {
                double targetLeft = Canvas.GetLeft(snapTarget) + snapTarget.Margin.Left;
                double targetTop = Canvas.GetTop(snapTarget) + snapTarget.ActualHeight + snapTarget.Margin.Top;

                SnapShadow.Visibility = Visibility.Visible;
                SnapShadow.Text = block.Text;
                SnapShadow.Width = block.ActualWidth;
                SnapShadow.Height = block.ActualHeight;
                SnapShadow.Background = new SolidColorBrush(Microsoft.UI.Colors.DarkGray);

                Canvas.SetLeft(SnapShadow, targetLeft);
                Canvas.SetTop(SnapShadow, targetTop);
            }
            else
            {
                SnapShadow.Visibility = Visibility.Collapsed;
            }
        }

        // Update snap connections
        private void UpdateSnapConnections(DraggableElement block)
        {
            if (block.Block is RepeatBlock repeatBlock)
            {
                repeatBlock.StartBlock = FindStartBlock(repeatBlock);
                repeatBlock.EndBlock = FindEndBlock(repeatBlock);

                Debug.WriteLine($"[RepeatBlock] StartBlock = {repeatBlock.StartBlock?.Text}, EndBlock = {repeatBlock.EndBlock?.Text}");

                if (!_repeatArrows.ContainsKey(repeatBlock))
                {
                    repeatBlock.PropertyChanged += RepeatBlock_PropertyChanged;
                }

                if (repeatBlock.EndBlock != null)
                {
                    if (!_repeatArrows.ContainsKey(repeatBlock))
                    {
                        var arrow = new Arrow();
                        arrow.AddToCanvas(WorkspaceCanvas);
                        _repeatArrows.Add(repeatBlock, arrow);
                    }

                    var arrowToUpdate = _repeatArrows[repeatBlock];
                    var sourceElement = WorkspaceCanvas.Children
                        .OfType<DraggableElement>()
                        .FirstOrDefault(de => de.Block == repeatBlock);

                    var endBlockElement = WorkspaceCanvas.Children
                        .OfType<DraggableElement>()
                        .FirstOrDefault(de => de.Block == repeatBlock.EndBlock);

                    if (sourceElement != null && endBlockElement != null)
                    {
                        arrowToUpdate.UpdatePosition(sourceElement, endBlockElement);
                    }
                }
                else
                {
                    if (_repeatArrows.ContainsKey(repeatBlock))
                    {
                        _repeatArrows[repeatBlock].RemoveFromCanvas(WorkspaceCanvas);
                        _repeatArrows.Remove(repeatBlock);
                    }
                }
            }

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
                        block.PreviousBlock = targetBlock;
                        targetBlock.NextBlock = block;
                        block.Block.PreviousBlock = targetBlock.Block;
                        targetBlock.Block.NextBlock = block.Block;

                        Debug.WriteLine(
                            $"Snapped '{block.Text}' to '{targetBlock.Text}'. " +
                            $"UI References: {block.PreviousBlock?.Text}, {targetBlock.NextBlock?.Text}; " +
                            $"Logic References: {block.Block.PreviousBlock?.Text}, {targetBlock.Block.NextBlock?.Text}");

                        if (block.Block is RepeatBlock repeatBlockk)
                        {
                            repeatBlockk.StartBlock = FindStartBlock(repeatBlockk);
                            repeatBlockk.EndBlock = FindEndBlock(repeatBlockk);

                            Debug.WriteLine($"[RepeatBlock] StartBlock = {repeatBlockk.StartBlock?.Text}, EndBlock = {repeatBlockk.EndBlock?.Text}");

                            if (repeatBlockk.EndBlock != null)
                            {
                                if (!_repeatArrows.ContainsKey(repeatBlockk))
                                {
                                    var arrow = new Arrow();
                                    arrow.AddToCanvas(WorkspaceCanvas);
                                    _repeatArrows.Add(repeatBlockk, arrow);
                                }

                                var arrowToUpdate = _repeatArrows[repeatBlockk];
                                var sourceElement = WorkspaceCanvas.Children
                                    .OfType<DraggableElement>()
                                    .FirstOrDefault(de => de.Block == repeatBlockk);

                                var endBlockElement = WorkspaceCanvas.Children
                                    .OfType<DraggableElement>()
                                    .FirstOrDefault(de => de.Block == repeatBlockk.EndBlock);

                                if (sourceElement != null && endBlockElement != null)
                                {
                                    arrowToUpdate.UpdatePosition(sourceElement, endBlockElement);
                                }
                            }
                        }
                        return;
                    }
                }
            }

            if (block.PreviousBlock != null)
            {
                block.PreviousBlock.NextBlock = null;
                block.PreviousBlock = null;
                block.Block.PreviousBlock = null;
            }
        }

        // Find the start block of a repeat block
        private BlockBase FindStartBlock(RepeatBlock repeatBlock)
        {
            var startBlock = repeatBlock.PreviousBlock;
            while (startBlock?.PreviousBlock != null)
            {
                startBlock = startBlock.PreviousBlock;
            }
            return startBlock;
        }

        // Find the end block of a repeat block
        private BlockBase FindEndBlock(RepeatBlock repeatBlock)
        {
            var endBlock = repeatBlock.PreviousBlock;
            for (int i = 1; i < repeatBlock.BlocksToRepeat && endBlock?.PreviousBlock != null; i++)
            {
                endBlock = endBlock.PreviousBlock;
            }
            return endBlock;
        }

        // Move connected blocks
        private void MoveConnectedBlocks(DraggableElement block, double parentLeft, double parentTop)
        {
            if (block.NextBlock != null)
            {
                var nextBlock = block.NextBlock;

                Debug.WriteLine($"Moving connected block: {nextBlock.Text}");
                Debug.WriteLine($"Block '{nextBlock.Text}': PreviousBlock = {nextBlock.PreviousBlock?.Text}, NextBlock = {nextBlock.NextBlock?.Text}");

                double nextLeft = parentLeft;
                double nextTop = parentTop + block.ActualHeight;

                Canvas.SetLeft(nextBlock, nextLeft);
                Canvas.SetTop(nextBlock, nextTop);

                MoveConnectedBlocks(nextBlock, nextLeft, nextTop);
            }
        }

        // Start execution
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Start button clicked. Executing block actions...");

            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;

            _executionCts = new CancellationTokenSource();
            _isRunning = true;

            try
            {
                var currentBlock = GreenFlagBlock.Block.NextBlock;

                while (currentBlock != null && !_executionCts.Token.IsCancellationRequested)
                {
                    var blockToProcess = currentBlock;
                    Debug.WriteLine($"Executing block: {blockToProcess.Text}");

                    await blockToProcess.ExecuteAsync(_executionCts.Token);

                    currentBlock = blockToProcess.NextBlock;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Execution was canceled.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during execution: {ex.Message}");
            }
            finally
            {
                _isRunning = false;
                _executionCts.Dispose();
                _executionCts = null;

                DispatcherQueue.TryEnqueue(() =>
                {
                    StartButton.IsEnabled = true;
                    StopButton.IsEnabled = false;
                });
            }
        }

        // Stop execution
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Stop button clicked. Stopping execution...");
            _executionCts?.Cancel();

            DispatcherQueue.TryEnqueue(() =>
            {
                StartButton.IsEnabled = true;
                StopButton.IsEnabled = false;
            });
        }

        // Check if a block is dropped in the trash
        private bool IsDroppedInTrash(DraggableElement block)
        {
            Debug.WriteLine($"Checking if block '{block.Text}' is dropped in trash...");
            return IsHoveringOverTrash(block);
        }

        // Check if a block is hovering over the trash
        private bool IsHoveringOverTrash(DraggableElement block)
        {
            double blockLeft = Canvas.GetLeft(block);
            double blockTop = Canvas.GetTop(block);
            double blockRight = blockLeft + block.ActualWidth;
            double blockBottom = blockTop + block.ActualHeight;

            Rect trashBounds = TrashIcon.TransformToVisual(WorkspaceCanvas)
                .TransformBounds(new Rect(0, 0, TrashIcon.ActualWidth, TrashIcon.ActualHeight));

            return blockRight > trashBounds.X &&
                   blockLeft < trashBounds.X + trashBounds.Width &&
                   blockBottom > trashBounds.Y &&
                   blockTop < trashBounds.Y + trashBounds.Height;
        }

        // Reset trash icon color after delay
        private async void ResetTrashIconColorAfterDelay()
        {
            await Task.Delay(500);
            TrashIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }

        // Debug block connections
        private void DebugBlockConnections()
        {
            Debug.WriteLine("Debugging block connections in the workspace:");

            foreach (var child in WorkspaceCanvas.Children)
            {
                if (child is DraggableElement block && block != SnapShadow)
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