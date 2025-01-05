using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeryonMotionGUI.Helpers
{
    public class RangeHelper
    {
        public static (double PositiveHalf, double NegativeHalf) GetRangeHalves(double axisRange)
        {
            double halfRange = axisRange / 2;
            return (halfRange, -halfRange);
        }
    }
}
