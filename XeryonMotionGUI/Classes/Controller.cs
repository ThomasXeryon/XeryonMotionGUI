using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeryonMotionGUI.Classes;
public class Controller
{
    public bool Running
    {
        get; set;
    }
    public string Status
    {
        get; set;
    }
    public SerialPort Port
    {
        get; set;
    }
    public string Name
    {
        get; set;
    }
    public string FriendlyPort
    {
        get; set;
    }
    public int AxisCount
    {
        get; set;
    }
    public Axis[] Axes
    {
        get; set;
    }
    public string Type
    {
        get; set;
    }
    public string Serial
    {
        get; set;
    }
    public string Soft
    {
        get; set;
    }
    public string Fgpa
    {
        get; set;
    }
    public string ControllerTitle
    {
        get
        {
            return "Controller " + FriendlyPort;
        }
    }

    public void Initialize()
    {
        if (Running)
        {
            Port.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
        }
    }

    private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
    {
        SerialPort sp = (SerialPort)sender;
        string inData = sp.ReadExisting();
        string[] dataParts = inData.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string part in dataParts)
        {
            if (part.StartsWith("STAT="))
            {
                if (int.TryParse(part.Substring(5), out int statValue))
                {
                    foreach (var axis in Axes)
                    {
                        axis.STAT = statValue;
                    }
                }
            }

            if (part.StartsWith("EPOS="))
            {
                if (int.TryParse(part.Substring(5), out int statValue))
                {
                    foreach (var axis in Axes)
                    {
                        axis.EPOS = statValue;
                    }
                }
            }

            if (part.StartsWith("TIME="))
            {
                if (int.TryParse(part.Substring(5), out int statValue))
                {
                    foreach (var axis in Axes)
                    {
                        axis.TIME = statValue;
                    }
                }
            }
        }
    }
}
