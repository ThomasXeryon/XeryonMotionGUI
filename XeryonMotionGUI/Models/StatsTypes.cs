using System;
using System.Collections.Generic;

namespace XeryonMotionGUI.Models
{
    public interface IStatsAggregator
    {
        void RecordBlockExecution(string blockType, double elapsedMs);
    }

    public class DeviationStats
    {
        public int Count { get; set; } = 0;
        public double SumDeviation { get; set; } = 0.0;
        public double MinDeviation { get; set; } = double.MaxValue;
        public double MaxDeviation { get; set; } = double.MinValue;
        public double AverageDeviation => Count == 0 ? 0.0 : SumDeviation / Count;
    }
}