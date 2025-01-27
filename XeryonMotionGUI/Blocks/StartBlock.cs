using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace XeryonMotionGUI.Blocks
{
    public class StartBlock : BlockBase
    {
        public StartBlock()
        {
            Text = "Start";
            RequiresAxis = false; // Start block doesn't need an axi
            Width = 120;  // Match GreenFlagBlock's width
            Height = 50;  // Match GreenFlagBlock's height
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            Debug.WriteLine("StartBlock executed. No action performed.");
        }
    }
}
