using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace XeryonMotionGUI.Blocks
{
    public class StepBlock : BlockBase
    {
        public StepBlock(string text)
        {
            Text = text;
            Width = 140;
            Height = 140;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (SelectedController == null || SelectedAxis == null)
                throw new InvalidOperationException("Controller and Axis must be selected.");

            // Highlight the StepBlock
            if (this.UiElement != null)
            {
                this.UiElement.HighlightBlock(true);
            }

            try
            {
                // Perform the step action
                if (Text == "Step +")
                {
                    Debug.WriteLine($"[StepBlock] Stepping {SelectedAxis.FriendlyName} positively.");
                    SelectedAxis.StepPositive();
                }
                else if (Text == "Step -")
                {
                    Debug.WriteLine($"[StepBlock] Stepping {SelectedAxis.FriendlyName} negatively.");
                    SelectedAxis.StepNegative();
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
        }
    }
}