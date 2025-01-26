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
            Height = 200; // Default height
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            Debug.WriteLine($"[RepeatBlock] Repeating {RepeatCount} times.");

            // Ensure the start and end blocks are set
            if (StartBlock == null || EndBlock == null)
            {
                Debug.WriteLine("[RepeatBlock] Error: StartBlock or EndBlock is not set.");
                return;
            }

            for (int i = 0; i < RepeatCount; i++)
            {
                Debug.WriteLine($"[RepeatBlock] Iteration {i + 1} of {RepeatCount}.");

                // Start from the first block in the repeat sequence
                var currentBlock = StartBlock;

                while (currentBlock != null && !cancellationToken.IsCancellationRequested)
                {
                    Debug.WriteLine($"[RepeatBlock] Executing block: {currentBlock.Text}");

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

                    // Stop if we've reached the end block
                    if (currentBlock == EndBlock)
                    {
                        break;
                    }

                    // Move to the next block in the chain
                    currentBlock = currentBlock.NextBlock;
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