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
using XeryonMotionGUI.Helpers;

namespace XeryonMotionGUI.Views
{
    public sealed partial class DemoBuilderPage : Page
    {
        private StatsAggregator _statsAggregator;

        private CancellationTokenSource _executionCts;
        private bool _isRunning = false;
        private const double SnapThreshold = 30.0;

        private DraggableElement _draggedBlock;
        private Point _dragStartOffset;



        public ObservableCollection<Controller> RunningControllers => Controller.RunningControllers;
        private Dictionary<RepeatBlock, Arrow> _repeatArrows = new Dictionary<RepeatBlock, Arrow>();

        public Dictionary<string, DeviationStats> _stepDeviationDictionary = new Dictionary<string, DeviationStats>();


        private readonly List<string> BlockTypes = new()
        {
            "Step", "Wait", "Repeat", "Move", "Home", "Stop", "Scan", "Index", "Log", "Edit Parameter"
        };

        public DemoBuilderPage()
        {
            PageLocator.CurrentDemoBuilderPage = this;
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
            InitializeGreenFlag();
            InitializeBlockPalette();
            _statsAggregator = new StatsAggregator();
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
        private Dictionary<string, BlockExecutionStat> _blockStats;
        private Stopwatch _executionStopwatch;

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // UI toggles
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;

            // Reset execution state and stats before starting a new run.
            _executionCts = new CancellationTokenSource();
            _isRunning = true;
            _executionStopwatch = Stopwatch.StartNew();
            double singleLongestMs = 0.0;

            // Reinitialize our stat dictionaries to clear any previous run's data.
            _statsAggregator = new StatsAggregator();
            _blockStats = new Dictionary<string, BlockExecutionStat>(StringComparer.OrdinalIgnoreCase);
            _stepDeviationDictionary = new Dictionary<string, DeviationStats>();  // Reset step deviation stats

            // Assign the new aggregator to all blocks (top-level and children)
            AssignStatsToAllBlocks(GreenFlagBlock.Block, _statsAggregator);

            try
            {
                // Use ONE aggregator for the entire run
                _statsAggregator = new StatsAggregator();
                AssignStatsToAllBlocks(GreenFlagBlock.Block, _statsAggregator);

                var currentBlock = GreenFlagBlock.Block.NextBlock;
                while (currentBlock != null && !_executionCts.Token.IsCancellationRequested)
                {
                    // 1) Time the block, no matter what type it is
                    var sw = Stopwatch.StartNew();

                    // 2) Execute the block
                    await currentBlock.ExecuteAsync(_executionCts.Token);

                    sw.Stop();
                    double thisBlockMs = sw.Elapsed.TotalMilliseconds;

                    // 3) Record top-level time in _blockStats
                    string blockType = currentBlock.Text ?? "Unknown";
                    if (!_blockStats.ContainsKey(blockType))
                    {
                        _blockStats[blockType] = new BlockExecutionStat { BlockType = blockType };
                    }

                    var stat = _blockStats[blockType];
                    stat.Count++;
                    stat.TotalMs += thisBlockMs;
                    if (thisBlockMs < stat.MinMs) stat.MinMs = thisBlockMs;
                    if (thisBlockMs > stat.MaxMs) stat.MaxMs = thisBlockMs;

                    // 4) Move to the next block
                    currentBlock = currentBlock.NextBlock;
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
                _executionStopwatch.Stop();
                _isRunning = false;
                _executionCts.Dispose();
                _executionCts = null;

                DispatcherQueue.TryEnqueue(() =>
                {
                    StartButton.IsEnabled = true;
                    StopButton.IsEnabled = false;
                });

                // Show final summary
                ShowFinalSummary(singleLongestMs);
            }
        }


        private void AssignStatsToAllBlocks(BlockBase start, IStatsAggregator aggregator)
        {
            var visited = new HashSet<BlockBase>();
            var queue = new Queue<BlockBase>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var b = queue.Dequeue();
                if (b == null || visited.Contains(b)) continue;

                b.Stats = aggregator;
                visited.Add(b);

                // Also push b.PreviousBlock, b.NextBlock, etc.
                queue.Enqueue(b.PreviousBlock);
                queue.Enqueue(b.NextBlock);
            }
        }

        private async void ShowFinalSummary(double singleLongestMs)
        {
            // == 1) Gather aggregator stats (all steps, including repeats) ==
            var allStats = _statsAggregator.GetStats();  // from all executed blocks
            double totalProgramMs = _executionStopwatch?.Elapsed.TotalMilliseconds ?? 0.0;
            int allBlocksExecuted = allStats.Sum(kvp => kvp.Value.Count);
            double totalAllMs = allStats.Sum(kvp => kvp.Value.TotalMs);
            double overallAvgMs = (allBlocksExecuted == 0) ? 0.0 : (totalAllMs / allBlocksExecuted);

            var slowestAll = allStats.Values.OrderByDescending(s => s.AverageMs).FirstOrDefault();
            var fastestAll = allStats.Values.OrderBy(s => s.AverageMs).FirstOrDefault();

            // == 2) Gather top-level stats (primary loop blocks only) ==
            double totalTopMs = _blockStats.Sum(kvp => kvp.Value.TotalMs);
            int topLevelBlocksExecuted = _blockStats.Sum(kvp => kvp.Value.Count);
            double overallTopAvg = (topLevelBlocksExecuted == 0) ? 0.0 : (totalTopMs / topLevelBlocksExecuted);

            var slowestTop = _blockStats.Values.OrderByDescending(s => s.AverageMs).FirstOrDefault();
            var fastestTop = _blockStats.Values.OrderBy(s => s.AverageMs).FirstOrDefault();

            // == 3) Build the main report ==
            var msg = new System.Text.StringBuilder();
            msg.AppendLine("========== PROGRAM FINISHED ==========\n");
            msg.AppendLine($"• Total Program Time: {totalProgramMs:F2} ms  ({(totalProgramMs / 1000.0):F2} s)");
            msg.AppendLine($"• Single Longest Block Execution: {singleLongestMs:F2} ms");
            msg.AppendLine();

            // --- 3A) Aggregator-based (ALL) stats ---
            msg.AppendLine("=== ALL BLOCKS (including repeats) ===");
            msg.AppendLine($"  - Total Blocks Executed (allStats): {allBlocksExecuted}");
            msg.AppendLine($"  - Overall Avg Time per Block: {overallAvgMs:F2} ms\n");

            if (slowestAll != null && slowestAll.Count > 0)
            {
                msg.AppendLine($"  - Slowest (avg): {slowestAll.BlockType}");
                msg.AppendLine($"     Count: {slowestAll.Count},  Avg: {slowestAll.AverageMs:F2} ms");
                msg.AppendLine($"     Min: {slowestAll.MinMs:F2}, Max: {slowestAll.MaxMs:F2}\n");
            }
            if (fastestAll != null && fastestAll.Count > 0)
            {
                msg.AppendLine($"  - Fastest (avg): {fastestAll.BlockType}");
                msg.AppendLine($"     Count: {fastestAll.Count},  Avg: {fastestAll.AverageMs:F2} ms");
                msg.AppendLine($"     Min: {fastestAll.MinMs:F2}, Max: {fastestAll.MaxMs:F2}\n");
            }

            msg.AppendLine("--- Detailed Aggregator Breakdown ---");
            foreach (var kvp in allStats.OrderBy(k => k.Key))
            {
                var s = kvp.Value;
                double pct = (totalAllMs > 0.0) ? ((s.TotalMs / totalAllMs) * 100.0) : 0.0;
                msg.AppendLine($"[{s.BlockType}]");
                msg.AppendLine($"   Count   = {s.Count}");
                msg.AppendLine($"   TotalMs = {s.TotalMs:F2}");
                msg.AppendLine($"   AvgMs   = {s.AverageMs:F2}");
                msg.AppendLine($"   MinMs   = {s.MinMs:F2}");
                msg.AppendLine($"   MaxMs   = {s.MaxMs:F2}");
                msg.AppendLine($"   % of All= {pct:F1}%");
                msg.AppendLine();
            }

            // --- 3B) Top-level block stats ---
            msg.AppendLine("=== TOP-LEVEL BLOCKS (main loop) ===");
            msg.AppendLine($"  - Top-Level Blocks Executed: {topLevelBlocksExecuted}");
            msg.AppendLine($"  - Avg Time per Top-Level Block: {overallTopAvg:F2} ms\n");

            if (slowestTop != null && slowestTop.Count > 0)
            {
                msg.AppendLine($"  - Slowest (avg): {slowestTop.BlockType}");
                msg.AppendLine($"     Count: {slowestTop.Count},  Avg: {slowestTop.AverageMs:F2} ms");
                msg.AppendLine($"     Min: {slowestTop.MinMs:F2}, Max: {slowestTop.MaxMs:F2}\n");
            }
            if (fastestTop != null && fastestTop.Count > 0)
            {
                msg.AppendLine($"  - Fastest (avg): {fastestTop.BlockType}");
                msg.AppendLine($"     Count: {fastestTop.Count},  Avg: {fastestTop.AverageMs:F2} ms");
                msg.AppendLine($"     Min: {fastestTop.MinMs:F2}, Max: {fastestTop.MaxMs:F2}\n");
            }

            msg.AppendLine("--- Top-Level Breakdown ---");
            foreach (var kvp in _blockStats.OrderBy(k => k.Key))
            {
                var s = kvp.Value;
                double pct = (totalTopMs > 0.0) ? ((s.TotalMs / totalTopMs) * 100.0) : 0.0;
                msg.AppendLine($"[{s.BlockType}]");
                msg.AppendLine($"   Count   = {s.Count}");
                msg.AppendLine($"   TotalMs = {s.TotalMs:F2}");
                msg.AppendLine($"   AvgMs   = {s.AverageMs:F2}");
                msg.AppendLine($"   MinMs   = {s.MinMs:F2}");
                msg.AppendLine($"   MaxMs   = {s.MaxMs:F2}");
                msg.AppendLine($"   % of All= {pct:F1}%");
                msg.AppendLine();
            }

            // == 4) Append the Step Deviation Summary ==
            // Here we assume _stepDeviationDictionary is a Dictionary<string, DeviationStats>,
            // where the key is the StepBlock's text (type) so that all executions of the same step aggregate.
            if (_stepDeviationDictionary != null && _stepDeviationDictionary.Any())
            {
                msg.AppendLine("=== STEP DEVIATION SUMMARY ===");

                // Merge all deviation stats across all StepBlock types.
                double globalMinDev = double.MaxValue;
                double globalMaxDev = double.MinValue;
                double globalSumDev = 0.0;
                int globalCountDev = 0;

                foreach (var kvp in _stepDeviationDictionary)
                {
                    var devStats = kvp.Value;
                    globalCountDev += devStats.Count;
                    globalSumDev += devStats.SumDeviation;
                    if (devStats.MinDeviation < globalMinDev)
                        globalMinDev = devStats.MinDeviation;
                    if (devStats.MaxDeviation > globalMaxDev)
                        globalMaxDev = devStats.MaxDeviation;
                }

                double globalAvgDev = (globalCountDev == 0) ? 0.0 : globalSumDev / globalCountDev;

                msg.AppendLine($"Total Steps (from StepBlocks): {globalCountDev}");
                msg.AppendLine($"Average Deviation: {globalAvgDev:F2} enc");
                msg.AppendLine($"Minimum Deviation: {globalMinDev:F2} enc");
                msg.AppendLine($"Maximum Deviation: {globalMaxDev:F2} enc");
                msg.AppendLine("(Enc = raw encoder counts. Convert to mm if needed.)");
                msg.AppendLine();

                // Optionally, list each step type's deviation stats:
                msg.AppendLine("--- Detailed Step Deviation Breakdown ---");
                foreach (var kvp in _stepDeviationDictionary.OrderBy(k => k.Key))
                {
                    var dev = kvp.Value;
                    double pct = (globalSumDev > 0.0) ? ((dev.SumDeviation / globalSumDev) * 100.0) : 0.0;
                    msg.AppendLine($"Step Type '{kvp.Key}':");
                    msg.AppendLine($"   Count      = {dev.Count}");
                    msg.AppendLine($"   TotalDev   = {dev.SumDeviation:F2} enc");
                    msg.AppendLine($"   AvgDev     = {dev.AverageDeviation:F2} enc");
                    msg.AppendLine($"   MinDev     = {dev.MinDeviation:F2} enc");
                    msg.AppendLine($"   MaxDev     = {dev.MaxDeviation:F2} enc");
                    msg.AppendLine($"   % of Total = {pct:F1}%");
                    msg.AppendLine();
                }
            }

            // == 5) Show the final message in a ContentDialog ==
            var dialog = new ContentDialog
            {
                Title = "Full Program Summary",
                Content = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = msg.ToString(),
                        TextWrapping = TextWrapping.Wrap,
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 14,
                    },
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                },
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
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

    public class DeviationStats
    {
        public int Count { get; set; } = 0;
        public double SumDeviation { get; set; } = 0.0;
        public double MinDeviation { get; set; } = double.MaxValue;
        public double MaxDeviation { get; set; } = double.MinValue;

        public double AverageDeviation => Count == 0 ? 0.0 : SumDeviation / Count;
    }


    public class BlockExecutionStat
    {
        public string BlockType
        {
            get; set;
        }
        public int Count
        {
            get; set;
        }
        public double TotalMs
        {
            get; set;
        }
        public double MinMs { get; set; } = double.MaxValue;
        public double MaxMs { get; set; } = double.MinValue;

        public double AverageMs => Count == 0 ? 0.0 : TotalMs / Count;
    }

    public interface IStatsAggregator
    {
        void RecordBlockExecution(string blockType, double elapsedMs);
    }

    public class StatsAggregator : IStatsAggregator
    {
        private readonly Dictionary<string, BlockExecutionStat> _blockStats
            = new Dictionary<string, BlockExecutionStat>(StringComparer.OrdinalIgnoreCase);

        public void RecordBlockExecution(string blockType, double elapsedMs)
        {
            if (!_blockStats.ContainsKey(blockType))
            {
                _blockStats[blockType] = new BlockExecutionStat { BlockType = blockType };
            }

            var stat = _blockStats[blockType];
            stat.Count++;
            stat.TotalMs += elapsedMs;
            if (elapsedMs < stat.MinMs) stat.MinMs = elapsedMs;
            if (elapsedMs > stat.MaxMs) stat.MaxMs = elapsedMs;
        }

        public Dictionary<string, BlockExecutionStat> GetStats() => _blockStats;
    }

}