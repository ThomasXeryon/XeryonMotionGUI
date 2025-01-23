using System.Collections.Generic;
using System.Threading.Tasks;
using XeryonMotionGUI.Classes;

namespace XeryonMotionGUI.Classes
{
    public class Block
    {
        public string Type
        {
            get; set;
        } // E.g., "MoveLeft", "Delay", "Loop"
        public Dictionary<string, object> Parameters
        {
            get; set;
        } // E.g., Steps, Duration, Iterations

        public Block(string type, Dictionary<string, object> parameters = null)
        {
            Type = type;
            Parameters = parameters ?? new Dictionary<string, object>();
        }

        public async Task ExecuteAsync(Controller controller)
        {
            switch (Type)
            {
                case "MoveLeft":
                    int stepsLeft = Parameters.ContainsKey("Steps") ? (int)Parameters["Steps"] : 100; // Default steps
                    await controller.SendCommand($"STEP=-{stepsLeft}");
                    break;

                case "MoveRight":
                    int stepsRight = Parameters.ContainsKey("Steps") ? (int)Parameters["Steps"] : 100; // Default steps
                    await controller.SendCommand($"STEP={stepsRight}");
                    break;

                case "Delay":
                    int duration = Parameters.ContainsKey("Duration") ? (int)Parameters["Duration"] : 1000; // Default duration in ms
                    await Task.Delay(duration);
                    break;

                case "Loop":
                    int iterations = Parameters.ContainsKey("Iterations") ? (int)Parameters["Iterations"] : 1; // Default iterations
                    var loopBlocks = Parameters.ContainsKey("Blocks") ? (List<Block>)Parameters["Blocks"] : new List<Block>();
                    for (int i = 0; i < iterations; i++)
                    {
                        foreach (var block in loopBlocks)
                        {
                            await block.ExecuteAsync(controller);
                        }
                    }
                    break;

                default:
                    throw new NotSupportedException($"Block type '{Type}' is not supported.");
            }
        }
    }
}