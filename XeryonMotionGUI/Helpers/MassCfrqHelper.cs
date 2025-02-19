using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeryonMotionGUI.Helpers;
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
        // Force CFRQ >= 100,000 => mass=0
        if (cfrq >= MAX_CFRQ)
            return 0;

        // Otherwise clamp to at least 3,000
        if (cfrq < MIN_CFRQ)
            cfrq = MIN_CFRQ;

        // Use inverse of the same power-law
        double mass = Math.Pow(A / cfrq, 1.0 / B);
        return (int)Math.Round(mass);
    }
}