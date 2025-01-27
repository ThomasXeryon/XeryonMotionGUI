using System.Diagnostics;

namespace XeryonMotionGUI.Blocks
{
    public class StopBlock : BlockBase
    {
        public StopBlock()
        {
            Text = "Stop";
            RequiresAxis = true; // Requires an axis to operate
            Width = 140;
            Height = 140;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            Debug.WriteLine($"[StopBlock] Stopping {SelectedAxis.FriendlyName}.");

            // Highlight the block
            if (this.UiElement != null)
            {
                this.UiElement.HighlightBlock(true);
            }

            try
            {
                // Perform the stop action
                SelectedAxis.Stop();
            }
            finally
            {
                // Remove the highlight
                if (this.UiElement != null)
                {
                    this.UiElement.HighlightBlock(false);
                }
            }

            Debug.WriteLine($"[StopBlock] Stop completed.");
        }
    }
}