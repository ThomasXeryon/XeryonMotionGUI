using System;

namespace XeryonMotionGUI.Blocks;

public static class BlockFactory
{
    public static BlockBase CreateBlock(string blockType)
    {
        BlockBase block = blockType switch
        {
            "Wait" => new WaitBlock(),
            "Repeat" => new RepeatBlock(),
            "Step +" => new StepBlock("Step +"),
            "Step -" => new StepBlock("Step -"),
            _ => throw new ArgumentException($"Unknown block type: {blockType}")
        };

        return block;
    }
}