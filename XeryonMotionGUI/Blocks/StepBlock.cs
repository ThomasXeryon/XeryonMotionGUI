using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace XeryonMotionGUI.Blocks
{
    public class StepBlock : BlockBase
    {
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

        public StepBlock()
        {
            Text = "Step";
            Width = 140;
            Height = 200;
            RequiresAxis = true; // Requires an axis to operat
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            string direction = IsPositive ? "positive" : "negative";
            Debug.WriteLine($"[StepBlock] Stepping {SelectedAxis.FriendlyName} in the {direction} direction.");

            // Highlight the block
            if (this.UiElement != null)
            {
                this.UiElement.HighlightBlock(true);
            }

            try
            {
                // Perform the step action
                if (IsPositive)
                {
                    SelectedAxis.StepPositive();
                }
                else
                {
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

            Debug.WriteLine($"[StepBlock] Step completed.");
        }
    }
 }
