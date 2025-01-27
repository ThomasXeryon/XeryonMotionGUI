using System.Diagnostics;

namespace XeryonMotionGUI.Blocks
{
    public class HomeBlock : BlockBase
    {
        public HomeBlock()
        {
            Text = "Home";
            RequiresAxis = true; // Requires an axis to operate
            Width = 140;
            Height = 140;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            Debug.WriteLine($"[HomeBlock] Homing {SelectedAxis.FriendlyName}.");

            // Highlight the block
            if (this.UiElement != null)
            {
                this.UiElement.HighlightBlock(true);
            }

            try
            {
                // Perform the home actio
                SelectedAxis.Home();
            }
            finally
            {
                // Remove the highlight
                if (this.UiElement != null)
                {
                    this.UiElement.HighlightBlock(false);
                }
            }

            Debug.WriteLine($"[HomeBlock] Home completed.");
        }
    }
}