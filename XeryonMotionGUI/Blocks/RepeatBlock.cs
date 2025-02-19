using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

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
            Width = 150; // Custom width
            Height = 200;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            Debug.WriteLine($"[RepeatBlock] Repeating {RepeatCount} times for {BlocksToRepeat} blocks above.");

            // Ensure the block above is set
            if (PreviousBlock == null)
            {
                Debug.WriteLine("[RepeatBlock] Error: No blocks above to repeat.");
                return;
            }

            // Collect the blocks to repeat
            var blocksToRepeat = new List<BlockBase>();
            var currentBlock = PreviousBlock;

            while (currentBlock != null && blocksToRepeat.Count < BlocksToRepeat)
            {
                blocksToRepeat.Add(currentBlock);
                currentBlock = currentBlock.PreviousBlock;
            }

            // Reverse the list to execute from top to bottom
            blocksToRepeat.Reverse();

            Debug.WriteLine($"[RepeatBlock] Found {blocksToRepeat.Count} blocks to repeat: {string.Join(", ", blocksToRepeat.Select(b => b.Text))}");

            // Repeat the blocks
            for (int i = 0; i < RepeatCount; i++)
            {
                Debug.WriteLine($"[RepeatBlock] Iteration {i + 1} of {RepeatCount}.");

                foreach (var block in blocksToRepeat)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Debug.WriteLine("[RepeatBlock] Execution cancelled.");
                        return;
                    }

                    Debug.WriteLine($"[RepeatBlock] Executing block: {block.Text}");

                    // Highlight the block
                    if (this.UiElement != null && _dispatcherQueue != null)
                    {
                        _dispatcherQueue.TryEnqueue(() => this.UiElement.HighlightBlock(true));
                    }

                    // Call the block's ExecuteAsync method
                    await block.ExecuteAsync(cancellationToken);

                    // Remove the highlight
                    if (this.UiElement != null && _dispatcherQueue != null)
                    {
                        _dispatcherQueue.TryEnqueue(() => this.UiElement.HighlightBlock(false));
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
