using System.Diagnostics;
using Microsoft.UI.Dispatching;

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

        public void SetDispatcherQueue(DispatcherQueue queue)
        {
            _dispatcherQueue = queue;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            Debug.WriteLine($"[StopBlock] Stopping {SelectedAxis.FriendlyName}.");

            // Highlight the block
            if (this.UiElement != null && _dispatcherQueue != null)
            {
                _dispatcherQueue.TryEnqueue(() => this.UiElement.HighlightBlock(true));
            }

            try
            {
                // Perform the stop actio
                SelectedAxis.Stop();
            }
            finally
            {
                // Remove the highlight
                if (this.UiElement != null && _dispatcherQueue != null)
                {
                    _dispatcherQueue.TryEnqueue(() => this.UiElement.HighlightBlock(false));
                }
            }

            Debug.WriteLine($"[StopBlock] Stop completed.");
        }
    }
}