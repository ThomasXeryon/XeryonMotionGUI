using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using XeryonMotionGUI.Classes;

namespace XeryonMotionGUI.Blocks
{
    /// <summary>
    /// A block that, when executed, starts or stops manual logging on the associated axis.
    /// </summary>
    public class LoggingBlock : BlockBase
    {
        private bool _isStart = false;

        public bool IsStart
        {
            get => _isStart;
            set
            {
                if (_isStart != value)
                {
                    _isStart = value;
                    Debug.WriteLine($"LoggingBlock.IsStart set to: {_isStart}");
                    OnPropertyChanged();
                }
            }
        }

        public void SetDispatcherQueue(DispatcherQueue queue)
        {
            _dispatcherQueue = queue;
        }

        public LoggingBlock()
        {
            _isStart = IsStart;
            Text = "Logging";
            RequiresAxis = true;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            SelectedAxis.AutoLogging = false;
            if (SelectedAxis == null)
            {
                Debug.WriteLine("[LoggingBlock] No axis assigned.");
                return;
            }

            if (_isStart)
            {
                SelectedAxis.StartManualLogging();
                Debug.WriteLine("Logger started");

            }
            else
            {
                SelectedAxis.StopManualLogging();
                Debug.WriteLine("Logger stopped");

            }

        }
    }
}
