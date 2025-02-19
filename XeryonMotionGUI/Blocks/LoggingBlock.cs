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
        private bool _IsStart = true;

        public bool IsStart
        {
            get => _IsStart;
            set
            {
                _IsStart = value;
                OnPropertyChanged();
            }
        }

        public void SetDispatcherQueue(DispatcherQueue queue)
        {
            _dispatcherQueue = queue;
        }

        public LoggingBlock()
        {
            _IsStart = IsStart;
            Text = "Logging";
            RequiresAxis = true;
            Width = 150;
            Height = 200;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            SelectedAxis.AutoLogging = false;
            if (SelectedAxis == null)
            {
                Debug.WriteLine("[LoggingBlock] No axis assigned.");
                return;
            }

            if (_IsStart)
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
