using System.Diagnostics;

namespace XeryonMotionGUI.Blocks
{
    public class ScanBlock : BlockBase
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
        public ScanBlock()
        {
            Text = "Scan";
            RequiresAxis = true; // Requires an axis to operate
            Width = 140;
            Height = 200;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            string direction = IsPositive ? "positive" : "negative";
            Debug.WriteLine($"[ScanBlock] Scanning {SelectedAxis.FriendlyName} in the {direction} direction.");

            // Highlight the block
            if (this.UiElement != null)
            {
                this.UiElement.HighlightBlock(true);
            }

            try
            {
                // Perform the scan actio
                if (IsPositive)
                {
                    SelectedAxis.ScanPositive();
                }
                else
                {
                    SelectedAxis.ScanNegative();
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

            Debug.WriteLine($"[ScanBlock] Scan completed.");
        }
    }
}