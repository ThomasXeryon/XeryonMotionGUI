using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace XeryonMotionGUI.Blocks
{
    public class StartBlock : BlockBase
    {
        public StartBlock()
        {
            Text = "Start";
            RequiresAxis = false; // Start block doesn't need an axi
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            Debug.WriteLine("StartBlock executed. No action performed.");
        }

        public void SetDispatcherQueue(DispatcherQueue queue)
        {
            _dispatcherQueue = queue;
        }
    }
}
