using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace XeryonMotionGUI.Blocks
{
    public class StepBlock : BlockBase
    {
        private bool _isPositive = true;
        public bool IsPositive
        {
            get => _isPositive;
            set
            {
                _isPositive = value;
                OnPropertyChanged();
            }
        }

        public void SetDispatcherQueue(DispatcherQueue queue)
        {
            _dispatcherQueue = queue;
        }

        public StepBlock()
        {
            Text = "Step";
            Width = 140;
            Height = 200;
            RequiresAxis = true;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            Debug.WriteLine($"[StepBlock] Stepping {SelectedAxis.FriendlyName} in the " +
                            $"{(IsPositive ? "positive" : "negative")} direction.");

            // 1) Highlight block on the UI thread
            //    Make sure you have a reference to `_dispatcherQueue`.
            if (this.UiElement != null && _dispatcherQueue != null)
            {
                _dispatcherQueue.TryEnqueue(() => this.UiElement.HighlightBlock(true));
            }

            try
            {
                // 2) Execute the actual stepping (hardware command)
                //    This part can run on any thread (background is fine).
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
                // 3) Remove highlight on UI thread
                if (this.UiElement != null && _dispatcherQueue != null)
                {
                    _dispatcherQueue.TryEnqueue(() => this.UiElement.HighlightBlock(false));
                }
            }

            Debug.WriteLine($"[StepBlock] Step completed.");
        }
    }
}
