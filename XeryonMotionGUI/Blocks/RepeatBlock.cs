using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using XeryonMotionGUI.Models; // Already added
using XeryonMotionGUI.Views;

namespace XeryonMotionGUI.Blocks
{
    public class RepeatBlock : BlockBase, INotifyPropertyChanged
    {
        private int _repeatCount = 1;
        private int _blocksToRepeat = 1; // Number of blocks above to repeat

        private BlockBase _startBlock;
        public BlockBase StartBlock
        {
            get => _startBlock;
            set
            {
                if (_startBlock != value)
                {
                    _startBlock = value;
                    OnPropertyChanged();
                }
            }
        }

        private BlockBase _endBlock;
        public BlockBase EndBlock
        {
            get => _endBlock;
            set
            {
                if (_endBlock != value)
                {
                    _endBlock = value;
                    OnPropertyChanged();
                }
            }
        }

        public void SetDispatcherQueue(DispatcherQueue queue)
        {
            _dispatcherQueue = queue;
        }

        public int RepeatCount
        {
            get => _repeatCount;
            set
            {
                if (_repeatCount != value)
                {
                    _repeatCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public int BlocksToRepeat
        {
            get => _blocksToRepeat;
            set
            {
                if (_blocksToRepeat != value)
                {
                    _blocksToRepeat = value;
                    OnPropertyChanged();
                }
            }
        }

        public RepeatBlock()
        {
            Text = "Repeat";
            RequiresAxis = false; // Repeat block doesn't need an axis
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            Debug.WriteLine($"[RepeatBlock] Repeating {RepeatCount} times for {BlocksToRepeat} blocks above.");

            if (PreviousBlock == null)
            {
                Debug.WriteLine("[RepeatBlock] Error: No blocks above to repeat.");
                return;
            }

            // 1) Collect the blocks above
            var blocksToRepeat = new List<BlockBase>();
            var cur = PreviousBlock;
            while (cur != null && blocksToRepeat.Count < BlocksToRepeat)
            {
                blocksToRepeat.Add(cur);
                cur = cur.PreviousBlock;
            }
            blocksToRepeat.Reverse();

            // 2) Repeat them
            for (int i = 0; i < RepeatCount; i++)
            {
                Debug.WriteLine($"[RepeatBlock] Iteration {i + 1} of {RepeatCount}.");

                foreach (var childBlock in blocksToRepeat)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Debug.WriteLine("[RepeatBlock] Execution canceled.");
                        return;
                    }

                    // Time the child block
                    var sw = Stopwatch.StartNew();
                    await childBlock.ExecuteAsync(cancellationToken);
                    sw.Stop();
                    double elapsedMs = sw.Elapsed.TotalMilliseconds;

                    // Record normal aggregator stats (top-level Stats aggregator)
                    Stats?.RecordBlockExecution(childBlock.Text, elapsedMs);

                    // If the child is a StepBlock, merge its local DeviationStats
                    if (childBlock is StepBlock stepB)
                    {
                        // Get a reference to your main page
                        var mainPage = XeryonMotionGUI.Helpers.PageLocator.GetDemoBuilderPage();
                        // Use the step's Text as a key (or any other unique identifier for the step type)
                        string stepKey = stepB.Text;
                        if (!mainPage._stepDeviationDictionary.ContainsKey(stepKey))
                        {
                            mainPage._stepDeviationDictionary[stepKey] = new DeviationStats();
                        }
                        var globalStats = mainPage._stepDeviationDictionary[stepKey];
                        var localStats = stepB.DeviationStats; // Now accessible via the public property

                        globalStats.Count += localStats.Count;
                        globalStats.SumDeviation += localStats.SumDeviation;
                        if (localStats.MinDeviation < globalStats.MinDeviation)
                            globalStats.MinDeviation = localStats.MinDeviation;
                        if (localStats.MaxDeviation > globalStats.MaxDeviation)
                            globalStats.MaxDeviation = localStats.MaxDeviation;

                        // Reset the block's local deviation stats after merging
                        localStats.Count = 0;
                        localStats.SumDeviation = 0.0;
                        localStats.MinDeviation = double.MaxValue;
                        localStats.MaxDeviation = double.MinValue;
                    }
                }
            }

            Debug.WriteLine($"[RepeatBlock] Repeat completed.");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}