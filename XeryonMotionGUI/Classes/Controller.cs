using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;

namespace XeryonMotionGUI.Classes
{
    public class Controller : INotifyPropertyChanged
    {
        public static ObservableCollection<Controller> FoundControllers { get; set; } = new ObservableCollection<Controller>();
        public static ObservableCollection<Controller> RunningControllers { get; set; } = new ObservableCollection<Controller>();


        public event PropertyChangedEventHandler PropertyChanged;

        private ICommand _OpenPortCommand;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _Status;
        public string Status
        {
            get => _Status;
            set
            {
                if (_Status != value)
                {
                    _Status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        private bool _Running;
        public bool Running
        {
            get => _Running;
            set
            {
                if (_Running != value)
                {
                    _Running = value;
                    OnPropertyChanged(nameof(Running));
                }
            }
        }

        private SerialPort _Port;
        public SerialPort Port
        {
            get => _Port;
            set
            {
                if (_Port != value)
                {
                    _Port = value;
                    OnPropertyChanged(nameof(Port));
                }
            }
        }

        private string _Name;
        public string Name
        {
            get => _Name;
            set
            {
                if (_Name != value)
                {
                    _Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        private string _FriendlyPort;
        public string FriendlyPort
        {
            get => _FriendlyPort;
            set
            {
                if (_FriendlyPort != value)
                {
                    _FriendlyPort = value;
                    OnPropertyChanged(nameof(FriendlyPort));
                }
            }
        }

        private int _AxisCount;
        public int AxisCount
        {
            get => _AxisCount;
            set
            {
                if (_AxisCount != value)
                {
                    _AxisCount = value;
                    OnPropertyChanged(nameof(AxisCount));
                }
            }
        }

        private ObservableCollection<Axis> _Axes;
        public ObservableCollection<Axis> Axes
        {
            get => _Axes;
            set
            {
                if (_Axes != value)
                {
                    _Axes = value;
                    OnPropertyChanged(nameof(Axes));
                }
            }
        }

        private string _Type;
        public string Type
        {
            get => _Type;
            set
            {
                if (_Type != value)
                {
                    _Type = value;
                    OnPropertyChanged(nameof(Type));
                }
            }
        }

        private string _Serial;
        public string Serial
        {
            get => _Serial;
            set
            {
                if (_Serial != value)
                {
                    _Serial = value;
                    OnPropertyChanged(nameof(Serial));
                }
            }
        }

        private string _Soft;
        public string Soft
        {
            get => _Soft;
            set
            {
                if (_Soft != value)
                {
                    _Soft = value;
                    OnPropertyChanged(nameof(Soft));
                }
            }
        }

        private string _Fgpa;
        public string Fgpa
        {
            get => _Fgpa;
            set
            {
                if (_Fgpa != value)
                {
                    _Fgpa = value;
                    OnPropertyChanged(nameof(Fgpa));
                }
            }
        }

        public string ControllerTitle => $"Controller {_FriendlyPort}";

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

            // Check if the controller has a single axis or multiple axes at the top level
            if (Axes.Count == 1)
            {
                // Single Axis Controller: Bind the EPOS and STAT directly to the first axis
                HandleSingleAxisData(dataParts);
            }
            else
            {
                // Multi-Axis Controller: Placeholder for future multi-axis implementation
                // Will implement later: Map the EPOS and STAT to specific axes
                HandleMultiAxisData(dataParts);
            }
            sp.DiscardInBuffer();
        }

        private void HandleSingleAxisData(string[] dataParts)
        {
            foreach (string part in dataParts)
            {
                if (part.StartsWith("STAT="))
                {
                    if (int.TryParse(part.Substring(5), out int statValue))
                    {
                        // Single Axis: Directly update the single axis STAT
                        Axes[0].STAT = statValue;
                    }
                }

                if (part.StartsWith("EPOS="))
                {
                    if (int.TryParse(part.Substring(5), out int eposValue))
                    {
                        // Single Axis: Directly update the single axis EPOS
                        Axes[0].EPOS = eposValue;
                        //Debug.WriteLine($"EPOS: {eposValue}");
                    }
                }
            }
        }

        private void HandleMultiAxisData(string[] dataParts)
        {
            foreach (string part in dataParts)
            {
                // For now, we don't know how to associate STAT and EPOS with specific axes in multi-axis mode
                // Placeholder logic: log the data or process later
                if (part.StartsWith("STAT="))
                {
                    Debug.WriteLine("Multi-Axis Controller: STAT parsing not yet implemented.");
                }

                if (part.StartsWith("EPOS="))
                {
                    Debug.WriteLine("Multi-Axis Controller: EPOS parsing not yet implemented.");
                }
            }
        }


        public void OpenPort()
        {
            try
            {
                if (!Running)
                {
                    Port.Open();
                    Running = true;
                    Status = "Disconnect";
                    UpdateRunningControllers();
                    Initialize();
                    Debug.WriteLine("Controller Connected");
                }
                else
                {
                    Port.Close();
                    Running = false;
                    Status = "Connect";
                    UpdateRunningControllers();
                }
            }
            catch (Exception)
            {
                ShowMessage("Error opening port", "Could not open port. Please check the port and try again.");
            }
        }

        private async Task ShowMessage(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK"
                //XamlRoot = this.Content.XamlRoot // Set XamlRoot to the root of the page
            };

            await dialog.ShowAsync();
        }

        public ICommand OpenPortCommand
        {
            get
            {
                _OpenPortCommand ??= new RelayCommand(OpenPort);
                return _OpenPortCommand;
            }
        }

        public static void UpdateRunningControllers()
        {
            var controllersToRemove = RunningControllers.Where(c => !c.Running).ToList();
            foreach (var controller in controllersToRemove)
            {
                RunningControllers.Remove(controller);
            }

            var controllersToAdd = FoundControllers.Where(c => c.Running && !RunningControllers.Contains(c)).ToList();
            foreach (var controller in controllersToAdd)
            {
                RunningControllers.Add(controller);
            }
        }

        public void SendCommand(string command)
        {
            if (Port.IsOpen)
            {

                    Debug.WriteLine($"Sending Command: {command}");
                    Port.Write(command);
            }
            else
            {
                Debug.WriteLine("Serial port not open. Command not sent.");
            }
        }
    }
}
