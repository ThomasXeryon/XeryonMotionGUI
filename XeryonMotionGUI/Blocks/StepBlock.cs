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
        int StepSize = 50000;

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
                // Await the step command so that the block stays highlighted until it finishes.
                if (IsPositive)
                {
                    await SelectedAxis.TakeStep(SelectedAxis.StepSize);
                }
                else
                {
                    await SelectedAxis.TakeStep(-SelectedAxis.StepSize);
                }
            }
            finally
            {
                // Remove the highlight once the asynchronous step is complete.
                if (this.UiElement != null)
                {
                    this.UiElement.HighlightBlock(false);
                }
            }

            Debug.WriteLine($"[StepBlock] Step completed.");
        }

    }
 }
