using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeryonMotionGUI.Classes;
public class Axis
{
    public  string Name
    {
        get; set;
    }
    public  string Type
    {
        get; set;
    }
    public  int Resolution
    {
        get; set;
    }
    public  string FriendlyName
    {
        get; set;
    }
    public  Double Range
    {
        get; set;
    }
    public  string AxisLetter
    {
        get; set;
    }
    public double DesiredPosition
    {
        get; set;
    }
    public double ActualPosition
    {
        get; set;
    }
    public double StepSize
    {
        get; set;
    }
    public double DPOS
    {
        get; set;
    }
    public double EPOS
    {
        get; set;
    }
    public int STAT
    {
        get; set;
    }
    public int TIME
    {
        get; set;
    }

    public string AxisTitle
    {
        get
        {
            if (AxisLetter != "None")
            {
                return "Axis " + AxisLetter;
            }
            return "Axis";
        }
    }

    public bool AmplifiersEnabled
    {
        get; set;
    }
    public bool EndStop
    {
        get; set;
    }
    public bool ThermalProtection1
    {
        get; set;
    }
    public bool ThermalProtection2
    {
        get; set;
    }
    public bool ForceZero
    {
        get; set;
    }
    public bool MotorOn
    {
        get; set;
    }
    public bool ClosedLoop
    {
        get; set;
    }
    public bool EncoderAtIndex
    {
        get; set;
    }
    public bool EncoderValid
    {
        get; set;
    }
    public bool SearchingIndex
    {
        get; set;
    }
    public bool PositionReached
    {
        get; set;
    }
    public bool ErrorCompensation
    {
        get; set;
    }
    public bool EncoderError
    {
        get; set;
    }
    public bool Scanning
    {
        get; set;
    }
    public bool LeftEndStop
    {
        get; set;
    }
    public bool RightEndStop
    {
        get; set;
    }
    public bool ErrorLimit
    {
        get; set;
    }
    public bool SearchingOptimal
    {
        get; set;
    }
    public bool SafetyTimeoutTriggered
    {
        get; set;
    }
    public bool EtherCATacknowledge
    {
        get; set;
    }
    public bool EmergencyStop
    {
        get; set;
    }
    public bool PositionFail
    {
        get; set;
    }

    public void UpdateStatusBits()
    {
        AmplifiersEnabled = (STAT & (1 << 0)) != 0;
        EndStop = (STAT & (1 << 1)) != 0;
        ThermalProtection1 = (STAT & (1 << 2)) != 0;
        ThermalProtection2 = (STAT & (1 << 3)) != 0;
        ForceZero = (STAT & (1 << 4)) != 0;
        MotorOn = (STAT & (1 << 5)) != 0;
        ClosedLoop = (STAT & (1 << 6)) != 0;
        EncoderAtIndex = (STAT & (1 << 7)) != 0;
        EncoderValid = (STAT & (1 << 8)) != 0;
        SearchingIndex = (STAT & (1 << 9)) != 0;
        PositionReached = (STAT & (1 << 10)) != 0;
        ErrorCompensation = (STAT & (1 << 11)) != 0;
        EncoderError = (STAT & (1 << 12)) != 0;
        Scanning = (STAT & (1 << 13)) != 0;
        LeftEndStop = (STAT & (1 << 14)) != 0;
        RightEndStop = (STAT & (1 << 15)) != 0;
        ErrorLimit = (STAT & (1 << 16)) != 0;
        SearchingOptimal = (STAT & (1 << 17)) != 0;
        SafetyTimeoutTriggered = (STAT & (1 << 18)) != 0;
        EtherCATacknowledge = (STAT & (1 << 19)) != 0;
        EmergencyStop = (STAT & (1 << 20)) != 0;
        PositionFail = (STAT & (1 << 21)) != 0;
    }
}
