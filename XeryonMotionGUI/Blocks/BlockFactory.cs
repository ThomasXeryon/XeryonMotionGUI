using System;

namespace XeryonMotionGUI.Blocks
{
    public static class BlockFactory
    {
        public static BlockBase CreateBlock(string blockType)
        {
            return blockType switch
            {
                "Wait" => new WaitBlock(),
                "Repeat" => new RepeatBlock(),
                "Step" => new StepBlock(),
                "Move" => new MoveBlock(),
                "Scan" => new ScanBlock(),
                "Home" => new HomeBlock(),
                "Stop" => new StopBlock(),
                "Index" => new IndexBlock(),
                "Index +" => new IndexBlock("Index +"),
                "Index -" => new IndexBlock("Index -"),
                "Edit Parameter" => new ParameterEditBlock(), // Add the new block
                _ => throw new ArgumentException($"Unknown block type: {blockType}")
            };
        }
    }
}