using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace XeryonMotionGUI.Blocks
{
    public class WaitBlock : BlockBase
    {
        private int _waitTime = 1000; // Default wait time

        public int WaitTime
        {
            get => _waitTime;
            set
            {
                _waitTime = value;
                OnPropertyChanged();
            }
        }

        public WaitBlock()
        {
            Text = "Wait";
            RequiresAxis = false; // Wait block doesn't need an axi
            Width = 150;
            Height = 120;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            Debug.WriteLine($"[WaitBlock] Waiting for {WaitTime} ms.");

            // Highlight the WaitBlock
            if (this.UiElement != null)
            {
                this.UiElement.HighlightBlock(true);
            }

            try
            {
                // Perform the wait action
                await Task.Delay(WaitTime, cancellationToken);
            }
            finally
            {
                // Remove the highlight
                if (this.UiElement != null)
                {
                    this.UiElement.HighlightBlock(false);
                }
            }

            Debug.WriteLine($"[WaitBlock] Wait completed.");
        }
    }
}