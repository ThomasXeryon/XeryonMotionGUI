﻿using System;
using System.Collections.ObjectModel;
using XeryonMotionGUI.Classes;

namespace XeryonMotionGUI.Blocks
{
    public static class BlockFactory
    {
        public static BlockBase CreateBlock(string blockType, ObservableCollection<Controller> runningControllers, Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue)
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

            // Initialize the controller and axi
            block.InitializeControllerAndAxis(runningControllers);

            return block;
        }
    }
}