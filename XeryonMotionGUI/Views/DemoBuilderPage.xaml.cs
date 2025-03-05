using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Text.Json;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Windows.Foundation;
using XeryonMotionGUI.Blocks;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using XeryonMotionGUI.Helpers;
using XeryonMotionGUI.ViewModels;
using XeryonMotionGUI.Classes;
using XeryonMotionGUI.Models;
using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;

namespace XeryonMotionGUI.Views
{
    public sealed partial class DemoBuilderPage : Page
    {
        private double _canvasWidth = 2000; // Initial width
        private double _canvasHeight = 1500; // Initial height
        private ScaleTransform _canvasScaleTransform = new ScaleTransform();
        private Point _lastPoint;
        private bool _isPanning = false;
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
            Debug.WriteLine("DemoBuilderPage constructor started.");
            PageLocator.CurrentDemoBuilderPage = this;
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
            InitializeGreenFlag();
            // Defer InitializeBlockPalette to Loaded event to avoid UI thread blocking
            _statsAggregator = new StatsAggregator();
            SetupGestureRecognition();
            Loaded += DemoBuilderPage_Loaded;
            WorkspaceCanvas.RenderTransform = _canvasScaleTransform;
            Debug.WriteLine("DemoBuilderPage constructor completed.");
        }

        private async void DemoBuilderPage_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("DemoBuilderPage_Loaded started.");
            await EnsureDefaultProgramAsync();
            InitializeBlockPalette(); // Moved here to run after page is loaded
            Debug.WriteLine("DemoBuilderPage_Loaded completed.");
        }

        private async Task EnsureDefaultProgramAsync()
        {
            var vm = (DemoBuilderViewModel)DataContext;
            Debug.WriteLine("Ensuring default program...");
            if (vm.AllSavedPrograms.Count == 0)
            {
                await vm.AddNewProgramAsync();
                Debug.WriteLine("Created default program asynchronously.");
            }
            else
            {
                Debug.WriteLine($"Existing programs found: {vm.AllSavedPrograms.Count}");
            }
        }

        private void InitializeGreenFlag()
        {
            Debug.WriteLine("Initializing GreenFlagBlock...");
            Canvas.SetLeft(GreenFlagBlock, 50);
            Canvas.SetTop(GreenFlagBlock, 10);
            GreenFlagBlock.Block = new StartBlock();
            GreenFlagBlock.Block.SetDispatcherQueue(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            GreenFlagBlock.WorkspaceCanvas = WorkspaceCanvas;
            GreenFlagBlock.RunningControllers = RunningControllers;
            Debug.WriteLine("GreenFlagBlock initialized.");
        }

        private void InitializeBlockPalette()
        {
            Debug.WriteLine("Initializing block palette...");
            // Sort BlockTypes alphabetically
            var sortedBlockTypes = BlockTypes.OrderBy(b => b).ToList();
            foreach (var blockType in sortedBlockTypes)
            {
                var block = new DraggableElement
                {
                    Block = BlockFactory.CreateBlock(blockType, RunningControllers, Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()),
                    Text = blockType,
                    Margin = new Thickness(10),
                    WorkspaceCanvas = WorkspaceCanvas,
                    RunningControllers = RunningControllers
                };

                // Apply background color directly based on block type
                if (blockType == "Move" || blockType == "Step" || blockType == "Scan" || blockType == "Home")
                {
                    block.Background = new SolidColorBrush(Microsoft.UI.Colors.LightBlue); // Slight blue
                    Debug.WriteLine($"Applying LightBlue background to {blockType}");
                }
                else if (blockType == "Edit Parameter" || blockType == "Index" || blockType == "Stop")
                {
                    block.Background = new SolidColorBrush(Microsoft.UI.Colors.LightGreen); // Slight green
                    Debug.WriteLine($"Applying LightGreen background to {blockType}");
                }
                else if (blockType == "Wait" || blockType == "Repeat" || blockType == "Log")
                {
                    block.Background = new SolidColorBrush(Microsoft.UI.Colors.LightYellow); // Slight dark yellow
                    Debug.WriteLine($"Applying LightYellow background to {blockType}");
                }

                block.PointerPressed += PaletteBlock_PointerPressed;
                block.PositionChanged += Block_PositionChanged;
                BlockPalette.Children.Add(block);
                Debug.WriteLine($"Palette block '{blockType}' added with background.");
            }
            Debug.WriteLine("Block palette initialized.");
        }

        public void ClearWorkspace()
        {
            var blocksToRemove = WorkspaceCanvas.Children
                .OfType<DraggableElement>()
                .Where(de => de != SnapShadow && de != GreenFlagBlock)
                .ToList();
            foreach (var block in blocksToRemove)
            {
                RemoveArrowsForBlock(block);
                WorkspaceCanvas.Children.Remove(block);
                block.Block.PreviousBlock = null;
                block.Block.NextBlock = null;
                block.PreviousBlock = null;
                block.NextBlock = null;
            }
            GreenFlagBlock.NextBlock = null;
            _repeatArrows.Clear();
            Debug.WriteLine($"Workspace cleared. Remaining children: {WorkspaceCanvas.Children.Count} (Expected: 2 - GreenFlagBlock, SnapShadow)");
        }

        public List<DraggableElement> GetWorkspaceBlocks()
        {
            var blocks = WorkspaceCanvas.Children
                .OfType<DraggableElement>()
                .Where(de => de != SnapShadow && !(de.Block is Blocks.StartBlock))
                .Distinct() // Prevent duplicates
                .ToList();
            Debug.WriteLine($"GetWorkspaceBlocks returned {blocks.Count} blocks: {string.Join(", ", blocks.Select(b => b.Text))}");
            return blocks;
        }

        private void Block_PositionChanged(object sender, EventArgs e)
        {
            UpdateAllArrows();
        }

        private void PaletteBlock_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine("PaletteBlock_PointerPressed triggered.");
            if (sender is DraggableElement paletteBlock)
            {
                var vm = (DemoBuilderViewModel)DataContext;
                if (vm.SelectedProgram == null)
                {
                    Debug.WriteLine("No program selected; creating a new one asynchronously.");
                    vm.AddNewProgramAsync().GetAwaiter().GetResult();
                }

                var newBlockInstance = BlockFactory.CreateBlock(
                    paletteBlock.Text,
                    RunningControllers,
                    Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()
                );
                _draggedBlock = new DraggableElement
                {
                    Block = newBlockInstance,
                    Text = paletteBlock.Text,
                    WorkspaceCanvas = WorkspaceCanvas,
                    SnapShadow = SnapShadow,
                    RunningControllers = RunningControllers
                };
                WorkspaceCanvas.Children.Add(_draggedBlock);
                var initialPosition = e.GetCurrentPoint(WorkspaceCanvas).Position;
                _dragStartOffset = new Point(_draggedBlock.ActualWidth / 2, _draggedBlock.ActualHeight / 2); // Center on cursor
                Canvas.SetLeft(_draggedBlock, initialPosition.X - _dragStartOffset.X);
                Canvas.SetTop(_draggedBlock, initialPosition.Y - _dragStartOffset.Y);
                AttachDragEvents(_draggedBlock);
                WorkspaceCanvas.PointerMoved += WorkspaceCanvas_PointerMoved;
                WorkspaceCanvas.PointerReleased += WorkspaceCanvas_PointerReleased;
                AddBlockToSelectedProgram(_draggedBlock);
                Debug.WriteLine($"Dragged block '{_draggedBlock.Text}' added to canvas at ({Canvas.GetLeft(_draggedBlock)}, {Canvas.GetTop(_draggedBlock)}). Program: '{vm.SelectedProgram?.ProgramName}', Blocks: {vm.SelectedProgram?.Blocks.Count}");
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
                Debug.WriteLine($"Started dragging block '{block.Text}' from ({Canvas.GetLeft(block)}, {Canvas.GetTop(block)}).");
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
                        Debug.WriteLine($"Disconnected '{_draggedBlock.Text}' from previous block due to distance.");
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
                Debug.WriteLine($"PointerReleased: Block '{_draggedBlock.Text}' dropped at ({Canvas.GetLeft(_draggedBlock)}, {Canvas.GetTop(_draggedBlock)}).");
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
                    var vm = (DemoBuilderViewModel)DataContext;
                    vm.SaveCurrentProgramState(this);
                }
                else
                {
                    SnapToNearestBlock(_draggedBlock);
                    UpdateSnapConnections(_draggedBlock);
                    MoveConnectedBlocks(_draggedBlock, Canvas.GetLeft(_draggedBlock), Canvas.GetTop(_draggedBlock));
                    var vm = (DemoBuilderViewModel)DataContext;
                    vm.SaveCurrentProgramState(this);
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
            {
                Debug.WriteLine("WorkspaceCanvas or SnapShadow is null or not visible in SnapToNearestBlock.");
                return;
            }
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
                        Debug.WriteLine($"Block '{block.Text}' snapped to: '{targetBlock.Text}' at ({snapLeft}, {snapTop}).");
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
                SnapShadow.Width = block.ActualWidth; // Use actual width
                SnapShadow.Height = block.ActualHeight; // Use actual height
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
            if (block == null) return;

            if (block.Block is RepeatBlock repeatBlock)
            {
                repeatBlock.StartBlock = FindStartBlock(repeatBlock);
                repeatBlock.EndBlock = FindEndBlock(repeatBlock);
                Debug.WriteLine($"[RepeatBlock] '{repeatBlock.Text}' StartBlock = {repeatBlock.StartBlock?.Text}, EndBlock = {repeatBlock.EndBlock?.Text}");
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
                    bool xSnapped = Math.Abs(blockLeft - targetLeft) <= SnapThreshold;
                    bool ySnapped = Math.Abs(blockTop - (targetTop + targetBlock.ActualHeight)) <= SnapThreshold;
                    if (xSnapped && ySnapped && targetBlock.NextBlock == null)
                    {
                        block.PreviousBlock = targetBlock;
                        targetBlock.NextBlock = block;
                        block.Block.PreviousBlock = targetBlock.Block;
                        targetBlock.Block.NextBlock = block.Block;
                        Debug.WriteLine($"Snapped '{block.Text}' to '{targetBlock.Text}'. UI Refs: Prev={block.PreviousBlock?.Text}, Next={targetBlock.NextBlock?.Text}; Logic Refs: Prev={block.Block.PreviousBlock?.Text}, Next={targetBlock.Block.NextBlock?.Text}");
                        return;
                    }
                }
            }
            if (block.PreviousBlock != null)
            {
                block.PreviousBlock.NextBlock = null;
                block.PreviousBlock = null;
                block.Block.PreviousBlock = null;
                Debug.WriteLine($"Disconnected '{block.Text}' from previous block.");
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
                double nextLeft = parentLeft; // Align horizontally (adjust if needed)
                double nextTop = parentTop + block.ActualHeight; // Use actual height
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
            _blockStats = new Dictionary<string, BlockExecutionStat>(StringComparer.OrdinalIgnoreCase);
            _stepDeviationDictionary = new Dictionary<string, DeviationStats>();
            AssignStatsToAllBlocks(GreenFlagBlock.Block, _statsAggregator);
            try
            {
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
                Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
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
            double overallAvgMs = allBlocksExecuted == 0 ? 0.0 : totalAllMs / allBlocksExecuted;
            var slowestAll = allStats.Values.OrderByDescending(s => s.AverageMs).FirstOrDefault();
            var fastestAll = allStats.Values.OrderBy(s => s.AverageMs).FirstOrDefault();
            double totalTopMs = _blockStats.Sum(kvp => kvp.Value.TotalMs);
            int topLevelBlocksExecuted = _blockStats.Sum(kvp => kvp.Value.Count);
            double overallTopAvg = topLevelBlocksExecuted == 0 ? 0.0 : totalTopMs / topLevelBlocksExecuted;
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
                double pct = totalAllMs > 0.0 ? (s.TotalMs / totalAllMs) * 100.0 : 0.0;
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
                double pct = totalTopMs > 0.0 ? (s.TotalMs / totalTopMs) * 100.0 : 0.0;
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
                double globalAvgDev = globalCountDev == 0 ? 0.0 : globalSumDev / globalCountDev;
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
                    double pct = globalSumDev > 0.0 ? (dev.SumDeviation / globalSumDev) * 100.0 : 0.0;
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
            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
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
                    Debug.WriteLine($"Block '{block.Text}': UI Prev={uiPrev}, UI Next={uiNext}; Logic Prev={logicPrev}, Logic Next={logicNext}");
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
            head.SetDispatcherQueue(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            BlockBase current = head;
            for (int i = 0; i < numberOfBlocks; i++)
            {
                string selectedType = possibleBlockTypes[rnd.Next(possibleBlockTypes.Count)];
                BlockBase newBlock = BlockFactory.CreateBlock(selectedType, RunningControllers, Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                newBlock.SetDispatcherQueue(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
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
                            double maxPossibleStep = isPos ? Math.Max(0.0, maxAxisTravelMm - currentPositionMm)
                                                           : Math.Max(0.0, currentPositionMm - minAxisTravelMm);
                            double maxRequest = Math.Min(10.0, maxPossibleStep);
                            if (maxRequest < 1.0) maxRequest = 1.0;
                            int randomStep = rnd.Next(1, (int)Math.Round(maxRequest) + 1);
                            stepB.StepSize = randomStep;
                            currentPositionMm += isPos ? randomStep : -randomStep;
                            if (currentPositionMm < minAxisTravelMm) currentPositionMm = minAxisTravelMm;
                            if (currentPositionMm > maxAxisTravelMm) currentPositionMm = maxAxisTravelMm;
                        }
                        break;
                    case "Move":
                        if (newBlock is MoveBlock moveB)
                        {
                            moveB.IsPositive = rnd.Next(2) == 0;
                        }
                        break;
                    case "Scan":
                        if (newBlock is ScanBlock scanB)
                        {
                            scanB.IsPositive = rnd.Next(2) == 0;
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
                    BlockBase waitBlock = BlockFactory.CreateBlock("Wait", RunningControllers, Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                    waitBlock.SetDispatcherQueue(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
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
            var vm = (DemoBuilderViewModel)DataContext;
            if (vm.SelectedProgram == null)
            {
                Debug.WriteLine("No program selected to save.");
                return;
            }

            var savePicker = new FileSavePicker();
            InitializeWithWindow.Initialize(savePicker, WindowNative.GetWindowHandle(App.MainWindow));
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("JSON File", new List<string> { ".json" });
            savePicker.SuggestedFileName = $"{vm.SelectedProgram.ProgramName}.json";

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                vm.SaveCurrentProgramState(this);
                var blocksToSave = vm.SelectedProgram.Blocks.ToList();
                string json = JsonSerializer.Serialize(blocksToSave, new JsonSerializerOptions { WriteIndented = true });
                await FileIO.WriteTextAsync(file, json);
                Debug.WriteLine($"Program '{vm.SelectedProgram.ProgramName}' exported to {file.Path} with {blocksToSave.Count} blocks.");
            }
        }

        private async void LoadProgramButton_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker();
            InitializeWithWindow.Initialize(openPicker, WindowNative.GetWindowHandle(App.MainWindow));
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".json");

            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file == null) return;

            try
            {
                string jsonString = await FileIO.ReadTextAsync(file);
                var blockDataList = JsonSerializer.Deserialize<List<XeryonMotionGUI.ViewModels.SavedBlockData>>(jsonString);
                if (blockDataList == null || !blockDataList.Any())
                {
                    Debug.WriteLine("Invalid or empty program file.");
                    return;
                }

                var vm = (DemoBuilderViewModel)DataContext;
                vm.SaveCurrentProgramState(this);
                ClearWorkspace();

                var newProgram = new ProgramInfo(file.DisplayName.Replace(".json", ""), new ObservableCollection<XeryonMotionGUI.ViewModels.SavedBlockData>(blockDataList));
                vm.AllSavedPrograms.Add(newProgram);
                vm.SelectedProgram = newProgram;
                LoadBlocksForSelectedProgram();
                await vm.SaveAllProgramsAsync();
                Debug.WriteLine($"Loaded program '{newProgram.ProgramName}' with {newProgram.Blocks.Count} blocks from file.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading program file: {ex.Message}");
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
            Debug.WriteLine($"GetAllConnectedBlocks for '{start.Text}' returned {result.Count} blocks.");
            return result;
        }

        public void LoadBlocksForSelectedProgram()
        {
            Debug.WriteLine("LoadBlocksForSelectedProgram started.");
            var vm = (DemoBuilderViewModel)DataContext;
            if (vm.SelectedProgram == null)
            {
                Debug.WriteLine("No program selected to load.");
                return;
            }

            ClearWorkspace();
            Debug.WriteLine($"Loading program '{vm.SelectedProgram.ProgramName}' with {vm.SelectedProgram.Blocks.Count} blocks.");
            var newDraggableList = new List<DraggableElement>();
            foreach (var savedBlockData in vm.SelectedProgram.Blocks)
            {
                BlockBase block = ConvertSavedBlockDataToBlockBase(savedBlockData);
                var de = new DraggableElement
                {
                    Block = block,
                    Text = block.Text,
                    WorkspaceCanvas = WorkspaceCanvas,
                    SnapShadow = SnapShadow,
                    RunningControllers = RunningControllers
                };
                Canvas.SetLeft(de, savedBlockData.X);
                Canvas.SetTop(de, savedBlockData.Y);
                Canvas.SetZIndex(de, 1);
                WorkspaceCanvas.Children.Add(de);
                AttachDragEvents(de);
                newDraggableList.Add(de);

                // Apply background color based on block type when loading
                if (de.Text == "Move" || de.Text == "Step" || de.Text == "Scan" || de.Text == "Home")
                {
                    de.Background = new SolidColorBrush(Microsoft.UI.Colors.LightBlue);
                }
                else if (de.Text == "Edit Parameter" || de.Text == "Index" || de.Text == "Stop")
                {
                    de.Background = new SolidColorBrush(Microsoft.UI.Colors.LightGreen);
                }
                else if (de.Text == "Wait" || de.Text == "Repeat" || de.Text == "Log")
                {
                    de.Background = new SolidColorBrush(Microsoft.UI.Colors.LightYellow);
                }

                Debug.WriteLine($"Loaded block '{de.Text}' at exact position ({savedBlockData.X}, {savedBlockData.Y}) into canvas with background.");
            }

            for (int i = 0; i < newDraggableList.Count; i++)
            {
                var savedBlockData = vm.SelectedProgram.Blocks[i];
                var de = newDraggableList[i];
                if (savedBlockData.PreviousBlockIndex.HasValue && savedBlockData.PreviousBlockIndex.Value >= 0 && savedBlockData.PreviousBlockIndex.Value < newDraggableList.Count)
                {
                    de.PreviousBlock = newDraggableList[savedBlockData.PreviousBlockIndex.Value];
                    de.Block.PreviousBlock = de.PreviousBlock.Block;
                    Debug.WriteLine($"Connected '{de.Text}' to previous block '{de.PreviousBlock.Text}' without snapping.");
                }
                if (savedBlockData.NextBlockIndex.HasValue && savedBlockData.NextBlockIndex.Value >= 0 && savedBlockData.NextBlockIndex.Value < newDraggableList.Count)
                {
                    de.NextBlock = newDraggableList[savedBlockData.NextBlockIndex.Value];
                    de.Block.NextBlock = de.NextBlock.Block;
                    Debug.WriteLine($"Connected '{de.Text}' to next block '{de.NextBlock.Text}' without snapping.");
                }
            }

            var rootBlock = newDraggableList.FirstOrDefault(d => d.PreviousBlock == null);
            if (rootBlock != null)
            {
                rootBlock.PreviousBlock = GreenFlagBlock;
                GreenFlagBlock.NextBlock = rootBlock;
                Debug.WriteLine($"Root block '{rootBlock.Text}' connected to GreenFlagBlock, kept at ({Canvas.GetLeft(rootBlock)}, {Canvas.GetTop(rootBlock)}).");
            }

            UpdateAllArrows();
            Debug.WriteLine($"Finished loading program '{vm.SelectedProgram.ProgramName}'. Canvas has {WorkspaceCanvas.Children.Count} children.");
        }

        private BlockBase ConvertSavedBlockDataToBlockBase(XeryonMotionGUI.ViewModels.SavedBlockData savedBlockData)
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
                "LoggingBlock" => "Log", // Ensure this matches
                "ParameterEditBlock" => "Edit Parameter",
                "RepeatBlock" => "Repeat",
                _ => throw new ArgumentException($"Unknown block type: {savedBlockData.BlockType}")
            };
            BlockBase block = BlockFactory.CreateBlock(blockType, RunningControllers, Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            block.SetDispatcherQueue(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            block.SelectedAxis = RunningControllers
                .SelectMany(c => c.Axes)
                .FirstOrDefault(a => a.DeviceSerial == savedBlockData.AxisSerial);
            block.SelectedController = RunningControllers
                .FirstOrDefault(c => c.FriendlyName == savedBlockData.ControllerFriendlyName);
            switch (block)
            {
                case WaitBlock wb:
                    wb.WaitTime = savedBlockData.WaitTime ?? 0;
                    break;
                case StepBlock sb:
                    sb.IsPositive = savedBlockData.IsPositive ?? true;
                    sb.StepSize = savedBlockData.StepSize ?? 0;
                    Debug.WriteLine($"Loading StepBlock: IsPositive set to {sb.IsPositive}");
                    break;
                case MoveBlock mb:
                    mb.IsPositive = savedBlockData.IsPositive ?? true;
                    Debug.WriteLine($"Loading MoveBlock: IsPositive set to {mb.IsPositive}");
                    break;
                case ScanBlock scanb:
                    scanb.IsPositive = savedBlockData.IsPositive ?? true;
                    Debug.WriteLine($"Loading ScanBlock: IsPositive set to {scanb.IsPositive}");
                    break;
                case LoggingBlock lb:
                    lb.IsStart = savedBlockData.IsStart ?? false; // Load IsStart
                    Debug.WriteLine($"Loading LoggingBlock: IsStart set to {lb.IsStart}");
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
            Debug.WriteLine($"Converted SavedBlockData to '{block.Text}' with Type={blockType}, X={savedBlockData.X}, Y={savedBlockData.Y}, IsStart={savedBlockData.IsStart}");
            return block;
        }

        private void AddBlockToSelectedProgram(DraggableElement draggable)
        {
            var vm = (DemoBuilderViewModel)DataContext;
            if (vm.SelectedProgram == null)
            {
                Debug.WriteLine("SelectedProgram is null; creating a new program.");
                vm.AddNewProgramAsync().GetAwaiter().GetResult();
            }
            var savedBlockData = ConvertBlockBaseToSavedBlockData(draggable.Block);
            savedBlockData.X = Canvas.GetLeft(draggable);
            savedBlockData.Y = Canvas.GetTop(draggable);
            savedBlockData.PreviousBlockIndex = -1; // Default until snapped
            savedBlockData.NextBlockIndex = -1;
            vm.SelectedProgram.Blocks.Add(savedBlockData);
            vm.SaveCurrentProgramState(this); // Ensure immediate save
            Debug.WriteLine($"Added block '{draggable.Text}' to '{vm.SelectedProgram.ProgramName}' at ({savedBlockData.X}, {savedBlockData.Y}). Total blocks: {vm.SelectedProgram.Blocks.Count}");
        }

        public XeryonMotionGUI.ViewModels.SavedBlockData ConvertBlockBaseToSavedBlockData(BlockBase block)
        {
            var savedBlockData = new XeryonMotionGUI.ViewModels.SavedBlockData
            {
                BlockType = block.GetType().Name,
                X = Canvas.GetLeft(block.UiElement ?? (block.UiElement = block.UiElement ?? new DraggableElement())),
                Y = Canvas.GetTop(block.UiElement ?? (block.UiElement = block.UiElement ?? new DraggableElement())),
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
                case LoggingBlock lb:
                    savedBlockData.IsStart = lb.IsStart; // Save IsStart
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
            Debug.WriteLine($"Converted block '{block.Text}' to SavedBlockData: Type={savedBlockData.BlockType}, X={savedBlockData.X}, Y={savedBlockData.Y}, IsStart={savedBlockData.IsStart}");
            return savedBlockData;
        }

        private void SavedProgramsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var vm = (DemoBuilderViewModel)DataContext;
            if (vm.SelectedProgram != null)
            {
                Debug.WriteLine($"Switching to program '{vm.SelectedProgram.ProgramName}' with {vm.SelectedProgram.Blocks.Count} blocks.");
                ClearWorkspace();
                LoadBlocksForSelectedProgram();
            }
            else
            {
                Debug.WriteLine("No program selected; clearing workspace.");
                ClearWorkspace();
            }
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

        public class StatsAggregator : IStatsAggregator
        {
            private readonly Dictionary<string, BlockExecutionStat> _blockStats =
                new Dictionary<string, BlockExecutionStat>(StringComparer.OrdinalIgnoreCase);
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

        private void SetupGestureRecognition()
        {
            WorkspaceScrollViewer.PointerPressed += WorkspaceScrollViewer_PointerPressed;
            WorkspaceScrollViewer.PointerMoved += WorkspaceScrollViewer_PointerMoved;
            WorkspaceScrollViewer.PointerReleased += WorkspaceScrollViewer_PointerReleased;
            WorkspaceScrollViewer.PointerWheelChanged += WorkspaceScrollViewer_PointerWheelChanged;
        }

        private void WorkspaceScrollViewer_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            {
                var properties = e.GetCurrentPoint(WorkspaceScrollViewer).Properties;
                if (properties.IsMiddleButtonPressed)
                {
                    _isPanning = true;
                    _lastPoint = e.GetCurrentPoint(WorkspaceCanvas).Position;
                    WorkspaceCanvas.CapturePointer(e.Pointer);
                }
            }
        }

        private void WorkspaceScrollViewer_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isPanning)
            {
                var currentPoint = e.GetCurrentPoint(WorkspaceCanvas).Position;
                double deltaX = currentPoint.X - _lastPoint.X;
                double deltaY = currentPoint.Y - _lastPoint.Y;
                WorkspaceScrollViewer.ChangeView(WorkspaceScrollViewer.HorizontalOffset - deltaX, WorkspaceScrollViewer.VerticalOffset - deltaY, null);
                _lastPoint = currentPoint;
            }
        }

        private void WorkspaceScrollViewer_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                WorkspaceCanvas.ReleasePointerCapture(e.Pointer);
            }
        }

        private void WorkspaceScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(WorkspaceCanvas);
            var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) == CoreVirtualKeyStates.Down;
            if (ctrlPressed)
            {
                double delta = point.Properties.MouseWheelDelta > 0 ? 0.1 : -0.1;
                double newZoom = Math.Clamp(_canvasScaleTransform.ScaleX + delta, 0.1f, 10.0f);
                _canvasScaleTransform.ScaleX = newZoom;
                _canvasScaleTransform.ScaleY = newZoom;
                WorkspaceScrollViewer.ChangeView(null, null, (float)newZoom, true);
                if (TrashIcon.RenderTransform is ScaleTransform trashScale)
                {
                    trashScale.ScaleX = 1.0f / newZoom;
                    trashScale.ScaleY = 1.0f / newZoom;
                }
                e.Handled = true;
            }
        }

        private void WorkspaceCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Dynamically adjust Canvas size based on content
            double maxX = 0;
            double maxY = 0;

            foreach (var child in WorkspaceCanvas.Children)
            {
                if (child is DraggableElement block)
                {
                    double x = Canvas.GetLeft(block) + block.ActualWidth;
                    double y = Canvas.GetTop(block) + block.ActualHeight;
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }

            // Add buffer for infinite scrolling
            _canvasWidth = Math.Max(_canvasWidth, maxX + 200);
            _canvasHeight = Math.Max(_canvasHeight, maxY + 200);
            WorkspaceCanvas.Width = _canvasWidth;
            WorkspaceCanvas.Height = _canvasHeight;
            Debug.WriteLine($"WorkspaceCanvas resized to {_canvasWidth}x{_canvasHeight}");
        }
    }
}