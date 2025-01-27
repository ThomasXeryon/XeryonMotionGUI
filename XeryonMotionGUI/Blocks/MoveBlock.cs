using System.Diagnostics;

namespace XeryonMotionGUI.Blocks
{
    public class MoveBlock : BlockBase
    {
        private readonly string _direction;

        private bool _isPositive = true; // Default direction (positive)

        public bool IsPositive
        {
            get => _isPositive;
            set
            {
                _isPositive = value;
                OnPropertyChanged();
            }
        }
        public MoveBlock()
        {
            Text = "Move";
            RequiresAxis = true; // Requires an axis to operat
            Width = 140;
            Height = 200;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            string direction = IsPositive ? "positive" : "negative";
            Debug.WriteLine($"[MoveBlock] Moving {SelectedAxis.FriendlyName} in the {direction} direction.");

            // Highlight the block
            if (this.UiElement != null)
            {
                this.UiElement.HighlightBlock(true);
            }

            try
            {
                // Perform the move action
                if (IsPositive)
                {
                    SelectedAxis.MovePositive();
                }
                else
                {
                    SelectedAxis.MoveNegative();
                }
            }
            finally
            {
                // Remove the highlight
                if (this.UiElement != null)
                {
                    this.UiElement.HighlightBlock(false);
                }
            }

            Debug.WriteLine($"[MoveBlock] Move completed.");
        }
    }
}