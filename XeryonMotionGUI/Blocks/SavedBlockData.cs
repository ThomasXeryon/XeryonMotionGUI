using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeryonMotionGUI.Blocks
{

    public class SavedBlockData
    {
        public string BlockType
        {
            get; set;
        }
        public double X
        {
            get; set;
        }
        public double Y
        {
            get; set;
        }
        public string AxisSerial
        {
            get; set;
        }
        public string ControllerFriendlyName
        {
            get; set;
        }
        public int? NextBlockIndex
        {
            get; set;
        }
        public int? PreviousBlockIndex
        {
            get; set;
        }
        public int? WaitTime
        {
            get; set;
        }
        public bool? IsPositive
        {
            get; set;
        }
        public int? StepSize
        {
            get; set;
        }
        public string SelectedParameter
        {
            get; set;
        }
        public int? ParameterValue
        {
            get; set;
        }
        public int? RepeatCount
        {
            get; set;
        }
        public int? BlocksToRepeat
        {
            get; set;
        }
    }

}
