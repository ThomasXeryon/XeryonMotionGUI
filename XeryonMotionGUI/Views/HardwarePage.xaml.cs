using Microsoft.UI;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using XeryonMotionGUI.Models;
using XeryonMotionGUI.ViewModels;
using Microsoft.UI.Xaml.Media;
using XeryonMotionGUI.Classes;
using System.IO.Ports;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using WinUIEx.Messaging;

namespace XeryonMotionGUI.Views;

public sealed partial class HardwarePage : Page
{
    public ObservableCollection<Controller> FoundControllers => Controller.FoundControllers;

    public HardwarePage()
    {
        InitializeComponent();
        DataContext = this;
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        _ = CheckForControllers();
    }

    void CheckForControllersButton_Click(object sender, RoutedEventArgs e)
    {
        _ = CheckForControllers();
    }

    private async Task CheckForControllers()
    {
        await Task.Delay(200);
        Debug.WriteLine("Searching for controllers");
        string[] ports = System.IO.Ports.SerialPort.GetPortNames();
        for (int i = Controller.FoundControllers.Count - 1; i >= 0; i--)
        {
            if (!Controller.FoundControllers[i].Running)
            {
                Controller.FoundControllers.RemoveAt(i);
            }
        }
        foreach (var port in ports)
        {
            var (isXeryon, response) = CheckIfXeryon(port);

            if (isXeryon)
            {
                SerialPort serialPort = new SerialPort(port);
                serialPort.BaudRate = 115200;
                serialPort.ReadTimeout = 200;
                serialPort.Open();
                var controller = new Controller();
                controller.Axes = new ObservableCollection<Axis>
                {
                    new Axis(),
                    new Axis(),
                    new Axis(),
                    new Axis()
                };
                controller.Port = serialPort;
                serialPort.Write("INFO=0");
                await Task.Delay(100);
                serialPort.DiscardInBuffer();
                var axesResponse = "";
                controller.Type = "";
                try
                {
                    serialPort.Write("AXES=?");
                    axesResponse = serialPort.ReadLine().Trim();
                }
                catch (TimeoutException)
                {
                    Debug.WriteLine(port + " IS OEM");
                    // IS OEM
                    controller.Type = "XD-OEM";
                    controller.Name = "XD-OEM Single Axis controller";
                    axesResponse = "AXES=1";
                }
                var axes = Convert.ToInt32(Regex.Match(axesResponse, @"AXES[:=](\d+)").Groups[1].Value);
                Debug.WriteLine($"Axis count: {axes}");

                if (axes == 1) // IS SINGLE AXIS XD-C
                {
                    if (controller.Type == "")
                    {
                        Debug.WriteLine(port + " IS XD-C");
                        controller.Name = "XD-C Single Axis controller";
                        controller.Type = "XD-C";
                    }
                    var ser = Regex.Match(response, @"SRNO=(\d+)");
                    var soft = Regex.Match(response, @"SOFT=(\d+)");
                    var dev = Regex.Match(response, @"SOFT=\d+\s+(.*?)\s+STAT=", RegexOptions.Singleline);
                    if (ser.Success)
                    {
                        controller.Serial = ser.Groups[1].Value;
                        controller.Status = "Connect";
                    }
                    if (soft.Success)
                    {
                        controller.Soft = soft.Groups[1].Value;
                    }
                    if (dev.Success)
                    {
                        controller.Axes[0].Resolution = Convert.ToInt32(dev.Groups[1].Value.Contains('=') ? dev.Groups[1].Value.Split('=')[1] : dev.Groups[1].Value);
                        controller.Axes[0].Type = dev.Groups[1].Value.Contains('=') ? dev.Groups[1].Value.Split('=')[0] : dev.Groups[1].Value;
                    }
                    serialPort.Write("INFO=0");
                    await Task.Delay(50);
                    serialPort.DiscardInBuffer();
                    serialPort.Write("LLIM=?");
                    var LLIM = serialPort.ReadLine().Replace("LLIM=", "").Trim();
                    Debug.WriteLine(LLIM);
                    serialPort.Write("HLIM=?");
                    var HLIM = serialPort.ReadLine().Replace("HLIM=", "").Trim();
                    Debug.WriteLine(HLIM);
                    controller.Axes[0].Range = Math.Round(((Convert.ToInt32(HLIM) + -Convert.ToDouble(LLIM)) * Convert.ToDouble(controller.Axes[0].Resolution) / 1000000), 2);
                    serialPort.Write("INFO=1");
                    serialPort.Close();

                    controller.Axes[0].AxisLetter = "None";
                    controller.Axes[0].FriendlyName = "Not set";
                    controller.Axes[0].Name = $"XLS-3/5-X-{controller.Axes[0].Resolution}";
                }
                else // NOT SINGLE AXIS
                {
                    // Handle multi-axis controllers if needed
                }
                controller.FriendlyPort = port;
                controller.Status = "Connect";
                Controller.FoundControllers.Add(controller);
            }
            else
            {
                Debug.WriteLine(port + " Response: " + response);
            }
        }
    }

    public (bool isXeryon, string response) CheckIfXeryon(string port)
    {
        SerialPort serialPort = new SerialPort(port);
        string response = string.Empty;
        try
        {
            Debug.WriteLine("Checking for: " + port);
            serialPort.BaudRate = 115200;
            serialPort.ReadTimeout = 2000;
            serialPort.Open();
            serialPort.Write("INFO=1");
            serialPort.Write("POLI=50");
            System.Threading.Thread.Sleep(100);
            response = serialPort.ReadExisting();
            response = string.Join("\n", response.Split('\n').TakeLast(6));
            Debug.WriteLine("Response from controller: " + response);
            bool isXeryon = response.Contains("SRNO");
            Debug.WriteLine(port + (isXeryon ? " Is Xeryon" : " Is NOT Xeryon"));
            return (isXeryon, response);
        }
        catch (Exception)
        {
            Debug.WriteLine(port + " Is NOT Xeryon");
            return (false, response);
        }
        finally
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }
    }

    private void ConnectCToController(Controller controller)
    {

    }

    private async Task ShowMessage(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.Content.XamlRoot // Set XamlRoot to the root of the page
        };

        await dialog.ShowAsync();
    }
}
