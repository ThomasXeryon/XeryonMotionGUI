using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace XeryonMotionGUI.Blocks
{
    public class RepeatBlock : BlockBase, INotifyPropertyChanged
    {
        private int _repeatCount = 1;
        private int _blocksToRepeat = 1; // Number of blocks above to repeat
        public BlockBase StartBlock
        {
            get; set;
        } // First block in the repeat sequence
        public BlockBase EndBlock
        {
            get; set;
        }   // Last block in the repeat sequence


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
                    if (block.UiElement != null)
                    {
                        block.UiElement.HighlightBlock(true);
                    }

                    // Call the block's ExecuteAsync method
                    await block.ExecuteAsync(cancellationToken);

                    // Remove the highlight
                    if (block.UiElement != null)
                    {
                        block.UiElement.HighlightBlock(false);
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