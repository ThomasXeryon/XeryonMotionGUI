using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Dispatching;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Text.Json;
using Windows.Storage;
using Windows.Storage.Pickers;
using VMProgramInfo = XeryonMotionGUI.ViewModels.ProgramInfo;
using WinRT.Interop;     // for InitializeWithWindow
using Windows.Foundation;
using XeryonMotionGUI.Blocks;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading;
using System.ComponentModel;
using System.Linq;

using System.Threading.Tasks;
using XeryonMotionGUI.Helpers;
using XeryonMotionGUI.ViewModels;

// Alias for our ViewModel SavedBlockData to avoid ambiguity.
using SavedBlockDataVM = XeryonMotionGUI.ViewModels.SavedBlockData;
using XeryonMotionGUI.Classes;

namespace XeryonMotionGUI.Views
{
    using XeryonMotionGUI.ViewModels;
    using SavedBlockData = XeryonMotionGUI.ViewModels.SavedBlockData;

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
            BlockBase demoProgram = GenerateRandomDemoProgram(10, axis: null);

            // Starting vertical position—set so that generated blocks appear below the Green Flag.
            double yPos = Canvas.GetTop(GreenFlagBlock) + GreenFlagBlock.ActualHeight + 20;

            // Traverse the linked list and add each block’s UI element to the WorkspaceCanvas.
            BlockBase currentBlock = demoProgram;
            while (currentBlock != null)
            {
                if (currentBlock.UiElement is DraggableElement element)
                {
                    Canvas.SetZIndex(element, 0);
                    Canvas.SetLeft(element, 50);
                    Canvas.SetTop(element, yPos);
                    if (!WorkspaceCanvas.Children.Contains(element))
                    {
                        WorkspaceCanvas.Children.Add(element);
                    }
                    yPos += element.ActualHeight + 20;
                }
                currentBlock = currentBlock.NextBlock;
            }
            var viewModel = (DemoBuilderViewModel)DataContext;
            _ = viewModel.LoadAllProgramsAsync(); // Start loading saved programs
        }

        // Initialize the GreenFlagBlock.
        private void InitializeGreenFlag()
        {
            Canvas.SetLeft(GreenFlagBlock, 50);
            Canvas.SetTop(GreenFlagBlock, 10);
            GreenFlagBlock.Block = new StartBlock();
            GreenFlagBlock.Block.SetDispatcherQueue(DispatcherQueue.GetForCurrentThread());
            Debug.WriteLine("Assigned StartBlock to GreenFlagBlock.");
        }

        // Initialize the block palette.
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

        private void Block_PositionChanged(object sender, EventArgs e)
        {
            UpdateAllArrows();
        }

        private void PaletteBlock_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is DraggableElement paletteBlock)
            {
                var newBlockInstance = BlockFactory.CreateBlock(
                    paletteBlock.Text,
                    this.RunningControllers,
                    DispatcherQueue.GetForCurrentThread()
                );
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
                AttachDragEvents(_draggedBlock);

                // *** NEW: Only add the block if a program is selected ***
                var vm = (DemoBuilderViewModel)DataContext;
                if (vm.SelectedProgram != null)
                {
                    AddBlockToSelectedProgram(newBlockInstance);
                    SaveCurrentProgram();
                }
                else
                {
                    // Optionally: show a message indicating that no program is selected.
                    Debug.WriteLine("No program selected. Block not added to any program.");
                }
                UpdateSnapConnections(_draggedBlock);
            }
        }


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
                repeatBlock.StartBlock = FindStartBlock(repeatBlock);
                repeatBlock.EndBlock = FindEndBlock(repeatBlock);
                Debug.WriteLine($"[RepeatBlock] Updated StartBlock = {repeatBlock.StartBlock?.Text}, EndBlock = {repeatBlock.EndBlock?.Text}");
                UpdateArrowForRepeatBlock(repeatBlock);
            }
        }

        private void UpdateArrowForRepeatBlock(RepeatBlock repeatBlock)
        {
            if (repeatBlock.EndBlock != null)
            {
                if (!_repeatArrows.ContainsKey(repeatBlock))
                {
                    var arrow = new Arrow();
                    arrow.AddToCanvas(WorkspaceCanvas);
                    _repeatArrows.Add(repeatBlock, arrow);
                }
                var sourceElement = WorkspaceCanvas.Children
                    .OfType<DraggableElement>()
                    .FirstOrDefault(de => de.Block == repeatBlock);
                var endBlockElement = WorkspaceCanvas.Children
                    .OfType<DraggableElement>()
                    .FirstOrDefault(de => de.Block == repeatBlock.EndBlock);
                if (sourceElement != null && endBlockElement != null)
                {
                    var arrowToUpdate = _repeatArrows[repeatBlock];
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

        private void WorkspaceBlock_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is DraggableElement block)
            {
                _draggedBlock = block;
                var position = e.GetCurrentPoint(WorkspaceCanvas).Position;
                _dragStartOffset = new Point(position.X - Canvas.GetLeft(block), position.Y - Canvas.GetTop(block));
                WorkspaceCanvas.PointerMoved += WorkspaceCanvas_PointerMoved;
                WorkspaceCanvas.PointerReleased += WorkspaceCanvas_PointerReleased;
            }
        }

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
                if (IsHoveringOverTrash(_draggedBlock))
                {
                    if (TrashIcon.RenderTransform is ScaleTransform scale)
                    {
                        scale.ScaleX = 1.2;
                        scale.ScaleY = 1.2;
                    }
                }
                else
                {
                    if (TrashIcon.RenderTransform is ScaleTransform scale)
                    {
                        scale.ScaleX = 1.0;
                        scale.ScaleY = 1.0;
                    }
                }
            }
        }

        private void WorkspaceCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_draggedBlock != null)
            {
                Debug.WriteLine($"PointerReleased: Block '{_draggedBlock.Text}' dropped.");
                if (IsDroppedInTrash(_draggedBlock))
                {
                    Debug.WriteLine($"Block '{_draggedBlock.Text}' dropped in trash. Removing entire chain.");
                    var chainBlocks = GetAllConnectedBlocks(_draggedBlock);
                    foreach (var blk in chainBlocks)
                    {
                        RemoveArrowsForBlock(blk);
                        WorkspaceCanvas.Children.Remove(blk);
                        blk.Block.PreviousBlock = null;
                        blk.Block.NextBlock = null;
                        blk.PreviousBlock = null;
                        blk.NextBlock = null;
                    }
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

        private void SnapToNearestBlock(DraggableElement block)
        {
            if (WorkspaceCanvas == null || SnapShadow == null || SnapShadow.Visibility != Visibility.Visible)
                return;
            double snapLeft = Canvas.GetLeft(SnapShadow);
            double snapTop = Canvas.GetTop(SnapShadow);
            Canvas.SetLeft(block, snapLeft);
            Canvas.SetTop(block, snapTop);
            if (block.PreviousBlock != null)
                block.PreviousBlock.NextBlock = null;
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
                    double deltaX = System.Math.Abs(blockLeft - (targetLeft + targetBlock.Margin.Left));
                    double deltaY = System.Math.Abs(blockTop - (targetTop + targetBlock.ActualHeight + targetBlock.Margin.Top));
                    double distance = System.Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
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
            if (block.PreviousBlock != null)
                return;
            foreach (var child in WorkspaceCanvas.Children)
            {
                if (child is DraggableElement targetBlock && targetBlock != block && targetBlock != SnapShadow)
                {
                    double blockLeft = Canvas.GetLeft(block);
                    double blockTop = Canvas.GetTop(block);
                    double targetLeft = Canvas.GetLeft(targetBlock);
                    double targetTop = Canvas.GetTop(targetBlock);
                    bool xSnapped = System.Math.Abs(blockLeft - targetLeft) <= SnapThreshold;
                    bool ySnapped = System.Math.Abs(blockTop - (targetTop + targetBlock.ActualHeight)) <= SnapThreshold;
                    if (xSnapped && ySnapped && targetBlock.NextBlock == null)
                    {
                        block.PreviousBlock = targetBlock;
                        targetBlock.NextBlock = block;
                        block.Block.PreviousBlock = targetBlock.Block;
                        targetBlock.Block.NextBlock = block.Block;
                        Debug.WriteLine($"Snapped '{block.Text}' to '{targetBlock.Text}'. UI References: {block.PreviousBlock?.Text}, {targetBlock.NextBlock?.Text}; Logic References: {block.Block.PreviousBlock?.Text}, {targetBlock.Block.NextBlock?.Text}");
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

        private BlockBase FindStartBlock(RepeatBlock repeatBlock)
        {
            var startBlock = repeatBlock.PreviousBlock;
            while (startBlock?.PreviousBlock != null)
            {
                startBlock = startBlock.PreviousBlock;
            }
            return startBlock;
        }

        private BlockBase FindEndBlock(RepeatBlock repeatBlock)
        {
            var endBlock = repeatBlock.PreviousBlock;
            for (int i = 1; i < repeatBlock.BlocksToRepeat && endBlock?.PreviousBlock != null; i++)
            {
                endBlock = endBlock.PreviousBlock;
            }
            return endBlock;
        }

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

        private Dictionary<string, BlockExecutionStat> _blockStats;
        private Stopwatch _executionStopwatch;

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            _executionCts = new CancellationTokenSource();
            _isRunning = true;
            _executionStopwatch = Stopwatch.StartNew();
            double singleLongestMs = 0.0;
            _statsAggregator = new StatsAggregator();
            _blockStats = new Dictionary<string, BlockExecutionStat>(System.StringComparer.OrdinalIgnoreCase);
            _stepDeviationDictionary = new Dictionary<string, DeviationStats>();
            AssignStatsToAllBlocks(GreenFlagBlock.Block, _statsAggregator);
            try
            {
                _statsAggregator = new StatsAggregator();
                AssignStatsToAllBlocks(GreenFlagBlock.Block, _statsAggregator);
                var currentBlock = GreenFlagBlock.Block.NextBlock;
                while (currentBlock != null && !_executionCts.Token.IsCancellationRequested)
                {
                    var sw = Stopwatch.StartNew();
                    await currentBlock.ExecuteAsync(_executionCts.Token);
                    sw.Stop();
                    double thisBlockMs = sw.Elapsed.TotalMilliseconds;
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
                    currentBlock = currentBlock.NextBlock;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Execution was canceled.");
            }
            catch (System.Exception ex)
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
                queue.Enqueue(b.PreviousBlock);
                queue.Enqueue(b.NextBlock);
            }
        }

        private async void ShowFinalSummary(double singleLongestMs)
        {
            var allStats = _statsAggregator.GetStats();
            double totalProgramMs = _executionStopwatch?.Elapsed.TotalMilliseconds ?? 0.0;
            int allBlocksExecuted = allStats.Sum(kvp => kvp.Value.Count);
            double totalAllMs = allStats.Sum(kvp => kvp.Value.TotalMs);
            double overallAvgMs = (allBlocksExecuted == 0) ? 0.0 : (totalAllMs / allBlocksExecuted);
            var slowestAll = allStats.Values.OrderByDescending(s => s.AverageMs).FirstOrDefault();
            var fastestAll = allStats.Values.OrderBy(s => s.AverageMs).FirstOrDefault();
            double totalTopMs = _blockStats.Sum(kvp => kvp.Value.TotalMs);
            int topLevelBlocksExecuted = _blockStats.Sum(kvp => kvp.Value.Count);
            double overallTopAvg = (topLevelBlocksExecuted == 0) ? 0.0 : (totalTopMs / topLevelBlocksExecuted);
            var slowestTop = _blockStats.Values.OrderByDescending(s => s.AverageMs).FirstOrDefault();
            var fastestTop = _blockStats.Values.OrderBy(s => s.AverageMs).FirstOrDefault();
            var msg = new System.Text.StringBuilder();
            msg.AppendLine("========== PROGRAM FINISHED ==========\n");
            msg.AppendLine($"• Total Program Time: {totalProgramMs:F2} ms  ({(totalProgramMs / 1000.0):F2} s)");
            msg.AppendLine($"• Single Longest Block Execution: {singleLongestMs:F2} ms");
            msg.AppendLine();
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
            if (_stepDeviationDictionary != null && _stepDeviationDictionary.Any())
            {
                msg.AppendLine("=== STEP DEVIATION SUMMARY ===");
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

        private bool IsDroppedInTrash(DraggableElement block)
        {
            Debug.WriteLine($"Checking if block '{block.Text}' is dropped in trash...");
            return IsHoveringOverTrash(block);
        }

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

        private async void ResetTrashIconColorAfterDelay()
        {
            await Task.Delay(500);
            TrashIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }

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

        public BlockBase GenerateRandomDemoProgram(int numberOfBlocks, Axis axis = null)
        {
            double maxAxisTravelMm = axis != null ? (axis.PositiveRange - axis.NegativeRange) : 100.0;
            double minAxisTravelMm = 0.0;
            double maxSpeed = axis?.Parameters?.FirstOrDefault(p => p.Command == "SPEED")?.Value ?? 50.0;
            if (maxSpeed <= 0) maxSpeed = 50.0;
            double currentPositionMm = 0.0;
            var possibleBlockTypes = new List<string>
            {
                "Step", "Wait", "Move", "Home", "Stop", "Scan", "Repeat", "Edit Parameter"
            };
            Random rnd = new Random();
            BlockBase head = new StartBlock { Text = "Start" };
            head.SetDispatcherQueue(DispatcherQueue.GetForCurrentThread());
            BlockBase current = head;
            for (int i = 0; i < numberOfBlocks; i++)
            {
                string selectedType = possibleBlockTypes[rnd.Next(possibleBlockTypes.Count)];
                BlockBase newBlock = BlockFactory.CreateBlock(selectedType, this.RunningControllers, DispatcherQueue.GetForCurrentThread());
                newBlock.SetDispatcherQueue(DispatcherQueue.GetForCurrentThread());
                switch (selectedType)
                {
                    case "Wait":
                        if (newBlock is WaitBlock waitB)
                        {
                            waitB.WaitTime = rnd.Next(100, 2001);
                        }
                        break;
                    case "Step":
                        if (newBlock is StepBlock stepB)
                        {
                            bool isPos = rnd.Next(2) == 0;
                            stepB.IsPositive = isPos;
                            double maxPossibleStep = isPos ? System.Math.Max(0.0, maxAxisTravelMm - currentPositionMm)
                                                           : System.Math.Max(0.0, currentPositionMm - minAxisTravelMm);
                            double maxRequest = System.Math.Min(10.0, maxPossibleStep);
                            if (maxRequest < 1.0) maxRequest = 1.0;
                            int randomStep = rnd.Next(1, (int)System.Math.Round(maxRequest) + 1);
                            stepB.StepSize = randomStep;
                            currentPositionMm += isPos ? randomStep : -randomStep;
                            if (currentPositionMm < minAxisTravelMm) currentPositionMm = minAxisTravelMm;
                            if (currentPositionMm > maxAxisTravelMm) currentPositionMm = maxAxisTravelMm;
                        }
                        break;
                    case "Move":
                        if (newBlock is MoveBlock moveB)
                        {
                            double randomSpeed = rnd.Next(1, (int)maxSpeed + 1);
                        }
                        break;
                    case "Scan":
                        if (newBlock is ScanBlock scanB)
                        {
                            double randomSpeed = rnd.Next(1, (int)maxSpeed + 1);
                        }
                        break;
                    case "Home":
                        if (newBlock is HomeBlock)
                        {
                            currentPositionMm = 0.0;
                        }
                        break;
                    case "Stop":
                        break;
                    case "Repeat":
                        if (newBlock is RepeatBlock repeatB)
                        {
                            repeatB.RepeatCount = rnd.Next(2, 6);
                            repeatB.BlocksToRepeat = rnd.Next(1, 4);
                        }
                        break;
                    case "Edit Parameter":
                        if (newBlock is ParameterEditBlock paramB)
                        {
                            paramB.SelectedParameter = "SPEED";
                            double newSpeed = rnd.Next(5, (int)maxSpeed);
                            paramB.ParameterValue = newSpeed;
                        }
                        break;
                }
                current.NextBlock = newBlock;
                newBlock.PreviousBlock = current;
                current = newBlock;
                if (selectedType == "Move" || selectedType == "Scan")
                {
                    BlockBase waitBlock = BlockFactory.CreateBlock("Wait", this.RunningControllers, DispatcherQueue.GetForCurrentThread());
                    waitBlock.SetDispatcherQueue(DispatcherQueue.GetForCurrentThread());
                    if (waitBlock is WaitBlock wb)
                    {
                        wb.WaitTime = rnd.Next(100, 2001);
                    }
                    current.NextBlock = waitBlock;
                    waitBlock.PreviousBlock = current;
                    current = waitBlock;
                    i++;
                }
            }
            return head;
        }

        private async void SaveProgramButton_Click(object sender, RoutedEventArgs e)
        {
            // Use the alias to refer to the ViewModels.ProgramInfo type.
            VMProgramInfo selected = ((DemoBuilderViewModel)DataContext).SelectedProgram;
            if (selected == null)
                return;

            try
            {
                // Get all draggable blocks (except the SnapShadow and StartBlock)
                var children = WorkspaceCanvas.Children
                    .OfType<DraggableElement>()
                    .Where(de => de != SnapShadow && !(de.Block is StartBlock))
                    .ToList();

                // Clear existing saved blocks
                selected.Blocks.Clear();

                // Convert each block into a saved block data object and add to the selected program.
                foreach (var draggable in children)
                {
                    var savedBlockData = ConvertBlockBaseToSavedBlockData(draggable.Block);
                    selected.Blocks.Add(savedBlockData);
                }

                // Save the entire collection of programs
                await ((DemoBuilderViewModel)DataContext).SaveAllProgramsAsync();

                Debug.WriteLine($"Program '{selected.ProgramName}' saved successfully.");
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Error saving program: {ex.Message}");
            }
        }


        private async void LoadProgramButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openPicker = new FileOpenPicker();
                IntPtr hwnd = WindowNative.GetWindowHandle(App.MainWindow);
                InitializeWithWindow.Initialize(openPicker, hwnd);
                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".json");
                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file == null)
                    return;
                string jsonString = await FileIO.ReadTextAsync(file);
                var blockDataList = JsonSerializer.Deserialize<List<SavedBlockDataVM>>(jsonString);
                if (blockDataList == null || !blockDataList.Any())
                {
                    Debug.WriteLine("No blocks found in file.");
                    return;
                }
                var existingBlocks = WorkspaceCanvas.Children
                    .OfType<DraggableElement>()
                    .Where(de => de != SnapShadow && !(de.Block is StartBlock))
                    .ToList();
                foreach (var b in existingBlocks)
                {
                    WorkspaceCanvas.Children.Remove(b);
                }
                double startLeft = Canvas.GetLeft(GreenFlagBlock);
                double startTop = Canvas.GetTop(GreenFlagBlock) + GreenFlagBlock.ActualHeight + 20;
                var newDraggableList = new List<DraggableElement>();
                for (int i = 0; i < blockDataList.Count; i++)
                {
                    var data = blockDataList[i];
                    string blockType = data.BlockType switch
                    {
                        "WaitBlock" => "Wait",
                        "StepBlock" => "Step",
                        "MoveBlock" => "Move",
                        "ScanBlock" => "Scan",
                        "IndexBlock" => "Index",
                        "StopBlock" => "Stop",
                        "HomeBlock" => "Home",
                        "LoggingBlock" => "Log",
                        "ParameterEditBlock" => "Edit Parameter",
                        "RepeatBlock" => "Repeat",
                        _ => "Wait"
                    };
                    BlockBase newBlock = BlockFactory.CreateBlock(blockType, this.RunningControllers, DispatcherQueue.GetForCurrentThread());
                    newBlock.SetDispatcherQueue(DispatcherQueue.GetForCurrentThread());
                    var de = new DraggableElement
                    {
                        Block = newBlock,
                        Text = blockType,
                        WorkspaceCanvas = WorkspaceCanvas,
                        SnapShadow = SnapShadow,
                        RunningControllers = this.RunningControllers
                    };
                    Canvas.SetLeft(de, startLeft);
                    Canvas.SetTop(de, startTop);
                    WorkspaceCanvas.Children.Add(de);
                    AttachDragEvents(de);
                    switch (newBlock)
                    {
                        case WaitBlock wb:
                            if (data.WaitTime.HasValue) wb.WaitTime = data.WaitTime.Value;
                            break;
                        case StepBlock sb:
                            if (data.IsPositive.HasValue) sb.IsPositive = data.IsPositive.Value;
                            if (data.StepSize.HasValue) sb.StepSize = data.StepSize.Value;
                            break;
                        case MoveBlock mb:
                            if (data.IsPositive.HasValue) mb.IsPositive = data.IsPositive.Value;
                            break;
                        case ScanBlock scanb:
                            if (data.IsPositive.HasValue) scanb.IsPositive = data.IsPositive.Value;
                            break;
                        case ParameterEditBlock peb:
                            peb.SelectedParameter = data.SelectedParameter;
                            if (data.ParameterValue.HasValue) peb.ParameterValue = data.ParameterValue.Value;
                            break;
                        case RepeatBlock rb:
                            if (data.RepeatCount.HasValue) rb.RepeatCount = data.RepeatCount.Value;
                            if (data.BlocksToRepeat.HasValue) rb.BlocksToRepeat = data.BlocksToRepeat.Value;
                            break;
                    }
                    if (!string.IsNullOrEmpty(data.AxisSerial))
                    {
                        foreach (var ctrl in this.RunningControllers)
                        {
                            var axisMatch = ctrl.Axes.FirstOrDefault(a => a.DeviceSerial == data.AxisSerial);
                            if (axisMatch != null)
                            {
                                newBlock.SelectedController = ctrl;
                                newBlock.SelectedAxis = axisMatch;
                                break;
                            }
                        }
                    }
                    de.UpdateLayout();
                    double blockHeight = (de.ActualHeight > 0) ? de.ActualHeight : 80;
                    startTop += blockHeight + 20;
                    newDraggableList.Add(de);
                }
                for (int i = 0; i < newDraggableList.Count; i++)
                {
                    var de = newDraggableList[i];
                    var data = blockDataList[i];
                    if (data.NextBlockIndex.HasValue)
                    {
                        int nbi = data.NextBlockIndex.Value;
                        if (nbi >= 0 && nbi < newDraggableList.Count)
                        {
                            de.NextBlock = newDraggableList[nbi];
                        }
                    }
                    if (data.PreviousBlockIndex.HasValue)
                    {
                        int pbi = data.PreviousBlockIndex.Value;
                        if (pbi >= 0 && pbi < newDraggableList.Count)
                        {
                            de.PreviousBlock = newDraggableList[pbi];
                        }
                    }
                }
                foreach (var rootBlock in newDraggableList.Where(d => d.PreviousBlock == null))
                {
                    rootBlock.PreviousBlock = GreenFlagBlock;
                    GreenFlagBlock.NextBlock = rootBlock;
                    double startLeftt = Canvas.GetLeft(GreenFlagBlock);
                    double startWidth = GreenFlagBlock.ActualWidth;
                    double startCenterX = startLeftt + (startWidth / 2.0);
                    rootBlock.UpdateLayout();
                    double blockWidth = rootBlock.ActualWidth;
                    double blockHalfWidth = blockWidth / 2.0;
                    double finalLeft = startCenterX - blockHalfWidth;
                    double finalTop = Canvas.GetTop(GreenFlagBlock) + GreenFlagBlock.ActualHeight;
                    Canvas.SetLeft(rootBlock, finalLeft);
                    Canvas.SetTop(rootBlock, finalTop);
                    Debug.WriteLine($"StartBlock: Left={startLeftt}, Width={startWidth}, CenterX={startCenterX}");
                    Debug.WriteLine($"RootBlock: Width={blockWidth}, FinalLeft={finalLeft}, FinalTop={finalTop}");
                    MoveConnectedBlocks(rootBlock, finalLeft, finalTop);
                    UpdateSnapConnections(rootBlock);
                }
                foreach (var block in newDraggableList)
                {
                    UpdateSnapConnections(block);
                }
                UpdateAllArrows();
                Debug.WriteLine("Program loaded successfully. Blocks snapped under StartBlock.");
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Error loading program: {ex.Message}");
            }
        }

        private List<DraggableElement> GetAllConnectedBlocks(DraggableElement start)
        {
            var result = new List<DraggableElement>();
            var up = start;
            while (up != null && !result.Contains(up))
            {
                result.Add(up);
                up = up.PreviousBlock;
            }
            var down = start;
            while (down != null && !result.Contains(down))
            {
                result.Add(down);
                down = down.NextBlock;
            }
            return result;
        }

        public void LoadBlocksForSelectedProgram()
        {
            // Use the VM alias so that we're working with the view model's ProgramInfo type.
            VMProgramInfo selected = ((DemoBuilderViewModel)DataContext).SelectedProgram;
            if (selected == null) return;

            // 1. Clear any existing blocks (except the Start block and the SnapShadow) from the workspace.
            var existingBlocks = WorkspaceCanvas.Children
        .OfType<DraggableElement>()
        .Where(de => de != SnapShadow && !(de.Block is StartBlock))
        .ToList();
            foreach (var block in existingBlocks)
            {
                WorkspaceCanvas.Children.Remove(block);
            }
            WorkspaceCanvas.UpdateLayout();

            // 2. Create a new list to hold the draggable elements for each saved block.
            var newDraggableList = new List<DraggableElement>();

            foreach (var savedBlockData in selected.Blocks)
            {
                // Convert saved data into a BlockBase instance.
                BlockBase block = ConvertSavedBlockDataToBlockBase(savedBlockData);

                // Create the DraggableElement.
                var draggableElement = new DraggableElement
                {
                    Block = block,
                    Text = block.GetType().Name,
                    WorkspaceCanvas = WorkspaceCanvas,
                    SnapShadow = SnapShadow,
                    RunningControllers = this.RunningControllers
                };

                // Use saved coordinates, but if they are 0, set defaults.
                double left = savedBlockData.X;
                double top = savedBlockData.Y;
                if (left == 0) left = 50;
                if (top == 0) top = Canvas.GetTop(GreenFlagBlock) + GreenFlagBlock.ActualHeight + 20;
                Canvas.SetLeft(draggableElement, left);
                Canvas.SetTop(draggableElement, top);

                // Set Z-index to ensure the block is visible.
                Canvas.SetZIndex(draggableElement, 1);

                WorkspaceCanvas.Children.Add(draggableElement);
                AttachDragEvents(draggableElement);
                newDraggableList.Add(draggableElement);

                // Force layout update (optional but helpful for debugging)
                draggableElement.UpdateLayout();
            }


            // 6. Restore any link references between blocks (if your design uses them).
            for (int i = 0; i < newDraggableList.Count; i++)
            {
                var savedBlockData = selected.Blocks[i];
                var draggableElement = newDraggableList[i];

                if (savedBlockData.PreviousBlockIndex.HasValue)
                {
                    int prevIndex = savedBlockData.PreviousBlockIndex.Value;
                    if (prevIndex >= 0 && prevIndex < newDraggableList.Count)
                    {
                        draggableElement.PreviousBlock = newDraggableList[prevIndex];
                        draggableElement.PreviousBlock.NextBlock = draggableElement;
                    }
                }
                if (savedBlockData.NextBlockIndex.HasValue)
                {
                    int nextIndex = savedBlockData.NextBlockIndex.Value;
                    if (nextIndex >= 0 && nextIndex < newDraggableList.Count)
                    {
                        draggableElement.NextBlock = newDraggableList[nextIndex];
                        draggableElement.NextBlock.PreviousBlock = draggableElement;
                    }
                }
            }



            // 7. Update snap connections and arrows so that the UI correctly reflects block chaining.
            foreach (var block in newDraggableList)
            {
                UpdateSnapConnections(block);
            }
            UpdateAllArrows();
        }


        private BlockBase ConvertSavedBlockDataToBlockBase(SavedBlockDataVM savedBlockData)
        {
            string blockType = savedBlockData.BlockType switch
            {
                "WaitBlock" => "Wait",
                "StepBlock" => "Step",
                "MoveBlock" => "Move",
                "ScanBlock" => "Scan",
                "IndexBlock" => "Index",
                "StopBlock" => "Stop",
                "HomeBlock" => "Home",
                "LoggingBlock" => "Log",
                "ParameterEditBlock" => "Edit Parameter",
                "RepeatBlock" => "Repeat",
                _ => throw new System.ArgumentException($"Unknown block type: {savedBlockData.BlockType}")
            };
            BlockBase block = BlockFactory.CreateBlock(blockType, this.RunningControllers, DispatcherQueue.GetForCurrentThread());
            block.SetDispatcherQueue(DispatcherQueue.GetForCurrentThread());
            block.SelectedAxis = this.RunningControllers
                .SelectMany(c => c.Axes)
                .FirstOrDefault(a => a.DeviceSerial == savedBlockData.AxisSerial);
            block.SelectedController = this.RunningControllers
                .FirstOrDefault(c => c.FriendlyName == savedBlockData.ControllerFriendlyName);
            switch (block)
            {
                case WaitBlock wb:
                    wb.WaitTime = savedBlockData.WaitTime ?? 0;
                    break;
                case StepBlock sb:
                    sb.IsPositive = savedBlockData.IsPositive ?? false;
                    sb.StepSize = savedBlockData.StepSize ?? 0;
                    break;
                case MoveBlock mb:
                    mb.IsPositive = savedBlockData.IsPositive ?? false;
                    break;
                case ScanBlock scanb:
                    scanb.IsPositive = savedBlockData.IsPositive ?? false;
                    break;
                case ParameterEditBlock peb:
                    peb.SelectedParameter = savedBlockData.SelectedParameter;
                    peb.ParameterValue = savedBlockData.ParameterValue ?? 0;
                    break;
                case RepeatBlock rb:
                    rb.RepeatCount = savedBlockData.RepeatCount ?? 0;
                    rb.BlocksToRepeat = savedBlockData.BlocksToRepeat ?? 0;
                    break;
            }
            return block;
        }

        private void AddBlockToSelectedProgram(BlockBase block)
        {
            var vm = (DemoBuilderViewModel)DataContext;
            if (vm.SelectedProgram != null)
            {
                // Convert the block to saved data and add it to the selected program.
                var savedBlockData = ConvertBlockBaseToSavedBlockData(block);
                vm.SelectedProgram.Blocks.Add(savedBlockData);
            }
        }


        private void SaveCurrentProgram()
        {
            VMProgramInfo selected = ((DemoBuilderViewModel)DataContext).SelectedProgram;

            if (selected != null)
            {
                selected.Blocks.Clear();
                var children = WorkspaceCanvas.Children
                    .OfType<DraggableElement>()
                    .Where(de => de != SnapShadow && !(de.Block is StartBlock))
                    .ToList();
                foreach (var draggable in children)
                {
                    var savedBlockData = ConvertBlockBaseToSavedBlockData(draggable.Block);
                    selected.Blocks.Add(savedBlockData);
                }
            }
        }

        private SavedBlockDataVM ConvertBlockBaseToSavedBlockData(BlockBase block)
        {
            var savedBlockData = new SavedBlockData
            {
                BlockType = block.GetType().Name,
                X = (int)Canvas.GetLeft(block.UiElement),
                Y = (int)Canvas.GetTop(block.UiElement),
                AxisSerial = block.SelectedAxis?.DeviceSerial,
                ControllerFriendlyName = block.SelectedController?.FriendlyName
            };

            switch (block)
            {
                case WaitBlock wb:
                    savedBlockData.WaitTime = wb.WaitTime;
                    break;
                case StepBlock sb:
                    savedBlockData.IsPositive = sb.IsPositive;
                    savedBlockData.StepSize = (int)sb.StepSize;
                    break;
                case MoveBlock mb:
                    savedBlockData.IsPositive = mb.IsPositive;
                    break;
                case ScanBlock scanb:
                    savedBlockData.IsPositive = scanb.IsPositive;
                    break;
                case ParameterEditBlock peb:
                    savedBlockData.SelectedParameter = peb.SelectedParameter;
                    savedBlockData.ParameterValue = (int)peb.ParameterValue;
                    break;
                case RepeatBlock rb:
                    savedBlockData.RepeatCount = rb.RepeatCount;
                    savedBlockData.BlocksToRepeat = rb.BlocksToRepeat;
                    break;
            }
            return savedBlockData;
        }

        private void SavedProgramsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SavedProgramsListView.SelectedItem is ProgramInfo selectedProgram)
            {
                var vm = (DemoBuilderViewModel)DataContext;
                VMProgramInfo selected = ((DemoBuilderViewModel)DataContext).SelectedProgram;
                LoadBlocksForSelectedProgram();
            }
        }
    }

    // --------------------- Supporting Classes ---------------------

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
        private readonly Dictionary<string, BlockExecutionStat> _blockStats =
            new Dictionary<string, BlockExecutionStat>(System.StringComparer.OrdinalIgnoreCase);
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

    // ProgramInfo is defined as a partial class in the ViewModels namespace.
    public partial class ProgramInfo : ObservableObject
    {
        [ObservableProperty]
        private string _programName;
        public ObservableCollection<SavedBlockDataVM> Blocks
        {
            get; set;
        }
        public ProgramInfo()
        {
            Blocks = new ObservableCollection<SavedBlockDataVM>();
        }
        public ProgramInfo(string name, ObservableCollection<SavedBlockDataVM> blocks)
        {
            _programName = name;
            Blocks = blocks;
        }
    }
}
