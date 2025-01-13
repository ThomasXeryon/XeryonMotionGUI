using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
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

        // Commands
        private ICommand _OpenPortCommand;

        // Constructor to initialize controller
        public Controller(string name, int axisCount = 1, string type = "Default")
        {
            Name = name;
            AxisCount = axisCount;
            Type = type;

            // Initialize Axes based on axis count
            InitializeAxes(axisCount, type);
        }

        // Initialize Axes dynamically
        private void InitializeAxes(int axisCount, string type)
        {
            Axes = new ObservableCollection<Axis>();
            for (int i = 0; i < axisCount; i++)
            {
                var axisName = $"Axis-{i + 1}";
                Axes.Add(new Axis(this, axisName, type));
            }
            OnPropertyChanged(nameof(Axes));
        }

        // Axis management methods
        public void AddAxis(Axis axis)
        {
            Axes.Add(axis);
            AxisCount = Axes.Count;
            OnPropertyChanged(nameof(Axes));
        }

        public void RemoveAxis(Axis axis)
        {
            Axes.Remove(axis);
            AxisCount = Axes.Count;
            OnPropertyChanged(nameof(Axes));
        }

        public Axis GetAxis(string name)
        {
            return Axes.FirstOrDefault(a => a.Name == name);
        }

        public void UpdateAxis(string name, Action<Axis> updateAction)
        {
            var axis = GetAxis(name);
            if (axis != null)
            {
                updateAction(axis);
                OnPropertyChanged(nameof(Axes));
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Properties
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


        // Command for opening/closing port
        public ICommand OpenPortCommand
        {
            get
            {
                _OpenPortCommand ??= new RelayCommand(OpenPort);
                return _OpenPortCommand;
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

        // Initialize the controller
        public void Initialize()
        {
            if (Running)
            {
                Port.DataReceived += DataReceivedHandler;
            }
        }

        // Data handling
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string inData = sp.ReadExisting();
            string[] dataParts = inData.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (Axes.Count == 1)
            {
                HandleSingleAxisData(dataParts);
            }
            else
            {
                HandleMultiAxisData(dataParts);
            }
            sp.DiscardInBuffer();
        }

        private void HandleSingleAxisData(string[] dataParts)
        {
            foreach (var part in dataParts)
            {
                if (part.StartsWith("STAT="))
                {
                    if (int.TryParse(part.Substring(5), out int statValue))
                    {
                        Axes[0].STAT = statValue;
                    }
                }

                if (part.StartsWith("EPOS="))
                {
                    if (int.TryParse(part.Substring(5), out int eposValue))
                    {
                        Axes[0].EPOS = eposValue;
                    }
                }
            }
        }

        private void HandleMultiAxisData(string[] dataParts)
        {
            foreach (var part in dataParts)
            {
                Debug.WriteLine("Multi-Axis Controller data parsing not yet implemented.");
            }
        }

        // Static method to manage running controllers
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

        // Show error messages
        private async Task ShowMessage(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK"
            };
            await dialog.ShowAsync();
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

        public void SendSetting(string commandName, double value)
        {
            if (Port.IsOpen)
            {
                if (commandName == "SSPD" || commandName == "MSPD" || commandName == "ISPD")
                {
                    value = value * 1000;
                }
                string command = $"{commandName}={value}";
                Debug.WriteLine($"Sending Command: {command}");
                Port.Write(command);
            }
            else
            {
                Debug.WriteLine("Serial port not open. Command not sent.");
            }
        }

        public void SaveSettings()
        {
            if (AxisCount == 1)
            {
                SendCommand("SAVE");
            }
            else
            {
                foreach (var axis in Axes)
                {
                    SendCommand($"{axis.AxisLetter}:SAVE");
                }
            }
        }

    }
}
