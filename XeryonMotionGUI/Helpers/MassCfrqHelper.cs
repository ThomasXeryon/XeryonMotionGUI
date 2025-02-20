using System;

namespace XeryonMotionGUI.Helpers
{
    public static class MassCfrqHelper
    {
        private const double A = 6_660_000.0;
        private const double B = 1.02;
        private const double MIN_CFRQ = 2700;
        private const double MAX_CFRQ = 100000.0;

        public static int MassToCfrqSmooth(double mass)
        {
            // Force 0 or negative mass => 100,000
            if (mass <= 0)
                return (int)MAX_CFRQ;

            double cfrq = A / Math.Pow(mass, B);

            if (cfrq > MAX_CFRQ) cfrq = MAX_CFRQ;
            if (cfrq < MIN_CFRQ) cfrq = MIN_CFRQ;

            return (int)Math.Round(cfrq);
        }

        public static int CfrqToMassSmooth(double cfrq)
        {
            // 1) Do the original calculation first
            double clampedCfrq = cfrq;

            // Force CFRQ >= MAX_CFRQ => mass=0 (same as before)
            if (clampedCfrq >= MAX_CFRQ)
                return 0;

            // Otherwise clamp to at least MIN_CFRQ
            if (clampedCfrq < MIN_CFRQ)
                clampedCfrq = MIN_CFRQ;

            double mass = Math.Pow(A / clampedCfrq, 1.0 / B);
            int computedMass = (int)Math.Round(mass);

            // 2) Now apply the five exact-value overrides
            if (cfrq == 100000.0) return 0;
            else if (cfrq == 60000.0) return 100;
            else if (cfrq == 30000.0) return 250;
            else if (cfrq == 10000.0) return 500;
            else if (cfrq == 5000.0) return 1000;

            // 3) Otherwise return the normal computed mass
            return computedMass;
        }
    }
}
