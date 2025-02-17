using System;

namespace XeryonMotionGUI.Helpers
{
    public enum Units
    {
        mm,
        Encoder, 
        mu,
        nm,
        inch,
        minch,
        rad,
        mrad,
        deg
    }

    public static class UnitConversion
    {
        public static double ToEncoder(double value, Units source, double encoderResolution)
        {
            switch (source)
            {
                case Units.mm: return value * 1e6 / encoderResolution;
                case Units.mu: return value * 1e3 / encoderResolution;
                case Units.nm: return value / encoderResolution;
                case Units.inch: return value * 25.4 * 1e6 / encoderResolution;
                case Units.minch: return value * 25.4 * 1e3 / encoderResolution;
                case Units.Encoder: return value;
                case Units.mrad: return value * 1e3 / encoderResolution;
                case Units.rad: return value * 1e6 / encoderResolution;
                case Units.deg: return value * (2.0 * Math.PI) / 360.0 * 1e6 / encoderResolution;
                default: throw new ArgumentException("Unexpected unit", nameof(source));
            }
        }

        public static double FromEncoder(double encoderValue, Units target, double encoderResolution)
        {
            switch (target)
            {
                case Units.mm: return encoderValue / (1e6 / encoderResolution);
                case Units.mu: return encoderValue / (1e3 / encoderResolution);
                case Units.nm: return encoderValue / (1.0 / encoderResolution);
                case Units.inch: return encoderValue / (25.4 * 1e6 / encoderResolution);
                case Units.minch: return encoderValue / (25.4 * 1e3 / encoderResolution);
                case Units.Encoder: return encoderValue;
                case Units.mrad: return encoderValue / (1e3 / encoderResolution);
                case Units.rad: return encoderValue / (1e6 / encoderResolution);
                case Units.deg: return encoderValue / ((2.0 * Math.PI) / 360.0 * 1e6 / encoderResolution);
                default: throw new ArgumentException("Unexpected unit", nameof(target));
            }
        }
    }
}
