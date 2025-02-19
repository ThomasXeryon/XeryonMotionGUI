using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;  // <-- Add this for DispatcherQueue
using XeryonMotionGUI.Classes;

namespace XeryonMotionGUI.Blocks
{
    public static class BlockFactory
    {
        /// <summary>
        /// Creates a new block of the specified type and initializes it with the provided controllers,
        /// optionally assigning a DispatcherQueue for UI updates.
        /// </summary>
        /// <param name="blockType">The string identifier for the block type, e.g. "Step", "Move", etc.</param>
        /// <param name="runningControllers">A collection of controllers the block can reference.</param>
        /// <param name="dispatcherQueue">
        ///     An optional DispatcherQueue from the UI thread. 
        ///     Passing this allows the new block to safely update UI elements without COM exceptions.
        /// </param>
        /// <returns>A fully initialized BlockBase instance.</returns>
        public static BlockBase CreateBlock(
            string blockType,
            ObservableCollection<Controller> runningControllers,
            DispatcherQueue dispatcherQueue = null)
        {
            BlockBase block = blockType switch
            {
                "Wait" => new WaitBlock(),
                "Repeat" => new RepeatBlock(),
                "Step" => new StepBlock(),
                "Move" => new MoveBlock(),
                "Scan" => new ScanBlock(),
                "Home" => new HomeBlock(),
                "Stop" => new StopBlock(),
                "Index" => new IndexBlock(),
                "Log" => new LoggingBlock(),
                "Edit Parameter" => new ParameterEditBlock(),
                _ => throw new ArgumentException($"Unknown block type: {blockType}")
            };

            // 1. Initialize the controller and axis
            block.InitializeControllerAndAxis(runningControllers);

            // 2. Assign the DispatcherQueue if provided
            if (dispatcherQueue != null)
            {
                block.SetDispatcherQueue(dispatcherQueue);
            }

            return block;
        }
    }
}
