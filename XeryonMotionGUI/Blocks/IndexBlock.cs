using System.Diagnostics;

namespace XeryonMotionGUI.Blocks
{
    public class IndexBlock : BlockBase
    {
        private readonly string _direction;

        public IndexBlock(string direction = "Index")
        {
            _direction = direction;
            Text = direction; // Set the block text (e.g., "Index", "Index +", or "Index -")
            RequiresAxis = true; // Requires an axis to operat
            Width = 140;
            Height = 140;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            Debug.WriteLine($"[IndexBlock] Indexing {SelectedAxis.FriendlyName} in the {_direction} direction.");

            // Highlight the block
            if (this.UiElement != null)
            {
                this.UiElement.HighlightBlock(true);
            }

            try
            {
                // Perform the index action
                if (_direction == "Index +")
                {
                    SelectedAxis.IndexPlus();
                }
                else if (_direction == "Index -")
                {
                    SelectedAxis.IndexMinus();
                }
                else
                {
                    SelectedAxis.Index();
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

            Debug.WriteLine($"[IndexBlock] Index completed.");
        }
    }
}