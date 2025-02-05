using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace XeryonMotionGUI.Classes
{
    public class Controller : INotifyPropertyChanged
    {
        #region Static Collections
        public static ObservableCollection<Controller> FoundControllers { get; set; } = new ObservableCollection<Controller>();
        public static ObservableCollection<Controller> RunningControllers { get; set; } = new ObservableCollection<Controller>();
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
        private static readonly Stopwatch _globalStopwatch = Stopwatch.StartNew();

        #region Backing Fields
        private ICommand _OpenPortCommand;

        private string _deviceId;
        private string _deviceKey;
        private string _deviceSerial;
        private bool _isConnected;
        private bool _loadingSettings;

        private string _Status;
        private bool _running;
        private SerialPort _Port;
        private string _Name;
        private string _FriendlyName;
        private string _FriendlyPort;
        private int _AxisCount;
        private ObservableCollection<Axis> _Axes;
        private string _Type;
        private string _Serial;
        private string _Soft;
        private string _Fgpa;
        #endregion

        #region Constructor
        public Controller(string name, int axisCount = 1, string type = "Default")
        {
            Name = name;
            AxisCount = axisCount;
            Type = type;

            // Initialize Axes based on axis count
            InitializeAxes(axisCount, type);
        }
        #endregion

        #region Public Properties

        public string _Label;

        public string Label
        {
            get => _Label;
            set
            {
                if (_Label != value)
                {
                    _Label = value;
                    OnPropertyChanged(nameof(Label));
                }
            }
        }

        public string DeviceId
        {
            get => _deviceId;
            set
            {
                if (_deviceId != value)
                {
                    _deviceId = value;
                    OnPropertyChanged(nameof(DeviceId));
                }
            }
        }

        public string DeviceKey
        {
            get => _deviceKey;
            set
            {
                if (_deviceKey != value)
                {
                    _deviceKey = value;
                    OnPropertyChanged(nameof(DeviceKey));
                }
            }
        }

        public string DeviceSerial
        {
            get => _deviceSerial;
            set
            {
                if (_deviceSerial != value)
                {
                    _deviceSerial = value;
                    OnPropertyChanged(nameof(DeviceSerial));
                }
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged(nameof(IsConnected));
                }
            }
        }

        public bool LoadingSettings
        {
            get => _loadingSettings;
            set
            {
                if (_loadingSettings != value)
                {
                    _loadingSettings = value;
                    OnPropertyChanged(nameof(LoadingSettings));
                }
            }
        }

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

        public bool Running
        {
            get => _running;
            set
            {
                if (_running != value)
                {
                    _running = value;
                    OnPropertyChanged(nameof(Running));
                }
            }
        }

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

        public string FriendlyName
        {
            get => _FriendlyName;
            set
            {
                if (_FriendlyName != value)
                {
                    _FriendlyName = value;
                    OnPropertyChanged(nameof(FriendlyName));
                }
            }
        }

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
        #endregion

        #region Command Properties

        // Command for Reconnect
        public ICommand ReconnectCommand => new RelayCommand(Reconnect);

        // Command for opening/closing port
        public ICommand OpenPortCommand
        {
            get
            {
                _OpenPortCommand ??= new RelayCommand(OpenPort);
                return _OpenPortCommand;
            }
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() is DispatcherQueue dispatcherQueue)
            {
                dispatcherQueue.TryEnqueue(() =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                });
            }
            else
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region Axis Management
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
        #endregion

        #region Connection Methods
        private void Reconnect()
        {
            try
            {
                // Attempt to reconnect the controller
                Port.Open();
                IsConnected = true; // Mark as connected
                Debug.WriteLine($"Reconnected to port {Port.PortName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to reconnect: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            try
            {
                if (Port != null && Port.IsOpen)
                {
                    Port.DataReceived -= DataReceivedHandler;
                    Port.Close();
                    Debug.WriteLine($"Port {Port.PortName} closed.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error closing port {Port?.PortName}: {ex.Message}");
            }
            finally
            {
                Running = false; // Safely notify UI of the state change
                UpdateRunningControllers();
            }
        }

        public void OpenPort()
        {
            try
            {
                if (!Running)
                {
                    Port.Open();
                    Port.BaudRate = 230400;
                    Port.ReadTimeout = 200;
                    Running = true;
                    Status = "Disconnect";
                    UpdateRunningControllers();
                    InitializeAsync();
                    Debug.WriteLine("Controller Connected");
                }
                else
                {
                    Debug.WriteLine("Trying to close controller");
                    try
                    {
                        Port.DataReceived -= DataReceivedHandler; // Remove event handler
                        Port.DiscardInBuffer();
                        Port.DiscardOutBuffer();
                        Port.Close();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        Running = false;
                        Status = "Connect";
                        UpdateRunningControllers();
                        Debug.WriteLine("Controller Disconnected");
                    }
                }
            }
            catch (Exception)
            {
                ShowMessage("Error opening port", "Could not open port. Please check the port and try again.");
            }
        }
        #endregion

        #region Data Handling
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            var sp = (SerialPort)sender;
            while (sp.BytesToRead > 0)
            {
                string line;
                try
                {
                    line = sp.ReadLine();
                }
                catch (TimeoutException)
                {
                    break;
                }

                // We'll get time as a double from stopwatch
                double timeSeconds = _globalStopwatch.Elapsed.TotalSeconds;

                // parse line
                ParseLine(line, timeSeconds);
            }
        }

        private void ParseLine(string line, double timeStamp)
        {
            // Split the incoming line on spaces and tabs.
            string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                // Check if the part contains a colon.
                string axisLetter = "";
                string remainder = part;
                if (part.Contains(":"))
                {
                    // Split at the first colon.
                    var tokens = part.Split(new char[] { ':' }, 2);
                    axisLetter = tokens[0].Trim();
                    remainder = tokens[1].Trim();
                }

                // Determine the target axis:
                // If an axis letter was provided, try to find that axis;
                // otherwise, use the first axis in the collection.
                var targetAxis = !string.IsNullOrEmpty(axisLetter)
                    ? Axes.FirstOrDefault(a => a.AxisLetter.Equals(axisLetter, StringComparison.OrdinalIgnoreCase))
                    : (Axes.Count > 0 ? Axes[0] : null);

                if (targetAxis == null)
                {
                    Debug.WriteLine($"No target axis found for part: {part}");
                    continue;
                }

                // Process the command in the remainder of the part.
                if (remainder.StartsWith("STAT=", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(remainder.Substring(5), out int statValue))
                    {
                        targetAxis.STAT = statValue;
                    }
                }
                else if (remainder.StartsWith("EPOS=", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(remainder.Substring(5), out int eposValue))
                    {
                        targetAxis.OnEPOSUpdate(eposValue, timeStamp);
                        targetAxis.EPOS = eposValue;
                    }
                }
            }
        }


        #endregion

        #region Parameter / Settings Methods
        public async Task InitializeAsync()
        {
            if (Running)
            {
                Port.DataReceived += DataReceivedHandler;
            }
            await LoadParametersFromController();
        }

        public void SendSetting(string commandName, double value, int resolution, string axisLetter = "")
        {
            if (Port.IsOpen)
            {
                // Apply any command-specific conversion.
                switch (commandName)
                {
                    case "SSPD":
                    case "MSPD":
                    case "ISPD":
                        value = value * 1000;
                        break;
                    case "LLIM":
                    case "HLIM":
                    case "ZON1":
                    case "ZON2":
                        value = Math.Round(value * 1000000 / resolution, 0); // Convert and round
                        break;
                    case "CFRQ":
                        value = value * 10;
                        break;
                    default:
                        break;
                }

                // If an axis letter is provided, use it as a prefix.
                string prefix = string.IsNullOrEmpty(axisLetter) ? "" : axisLetter + ":";
                string command = $"{prefix}{commandName}={value}";
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

        public async void UploadSettings(string settings)
        {
            Debug.WriteLine("Uploading settings...");

            // Split settings text into lines, ignoring empty lines.
            var lines = settings.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                try
                {
                    // Remove comments after '%' (if any) and trim.
                    var cleanLine = line.Split('%')[0].Trim();
                    if (string.IsNullOrWhiteSpace(cleanLine))
                    {
                        continue; // Skip empty or comment-only lines
                    }

                    string axisLetter = null;
                    string command = null;
                    double rawValue = 0;

                    // Check if the line has an axis letter prefix (e.g., A:SSPD=200)
                    if (cleanLine.Contains(":"))
                    {
                        // Split into two parts at the first colon.
                        var axisParts = cleanLine.Split(new[] { ':' }, 2);
                        if (axisParts.Length == 2)
                        {
                            // The first part is the axis letter.
                            axisLetter = axisParts[0].Trim();
                            // The second part should contain the command and value.
                            var commandParts = axisParts[1].Split('=');
                            if (commandParts.Length == 2 && double.TryParse(commandParts[1], out rawValue))
                            {
                                command = commandParts[0].Trim();
                            }
                        }
                    }
                    else
                    {
                        // For single-axis controllers (e.g., SSPD=200)
                        var commandParts = cleanLine.Split('=');
                        if (commandParts.Length == 2 && double.TryParse(commandParts[1], out rawValue))
                        {
                            command = commandParts[0].Trim();
                        }
                    }

                    if (!string.IsNullOrEmpty(command))
                    {
                        // Determine the target axis.
                        Axis targetAxis = null;
                        if (!string.IsNullOrEmpty(axisLetter))
                        {
                            // Multi-axis: select the axis matching the provided letter.
                            targetAxis = Axes.FirstOrDefault(a => a.AxisLetter.Equals(axisLetter, StringComparison.OrdinalIgnoreCase));
                        }
                        else if (Axes.Count == 1)
                        {
                            // Single-axis: use the only available axis.
                            targetAxis = Axes[0];
                        }

                        if (targetAxis != null)
                        {
                            // Find the parameter (by its command name) within the target axis.
                            var parameter = targetAxis.Parameters.FirstOrDefault(p => p.Command == command);
                            if (parameter != null)
                            {
                                // Update the parameter value.
                                parameter.Value = rawValue;
                                Debug.WriteLine($"Axis {targetAxis.AxisLetter}: Updated {parameter.Name} to {rawValue}");
                            }
                            else
                            {
                                Debug.WriteLine($"Axis {targetAxis.AxisLetter}: Command {command} not found in parameters.");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"Axis {axisLetter ?? "None"}: Not found for command {command}.");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Invalid format in settings line: {line}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing line '{line}': {ex.Message}");
                }

                // Small delay before processing the next line.
                await Task.Delay(10);
            }

            Debug.WriteLine("Settings upload complete.");
        }


        public async Task LoadParametersFromController()
        {
            LoadingSettings = true;
            Port.DataReceived -= DataReceivedHandler;
            Port.WriteLine("INFO=0");
            Port.DiscardInBuffer();
            await Task.Delay(100);
            Port.DiscardInBuffer();
            Port.ReadTimeout = 200;

            try
            {
                foreach (var axis in Axes)
                {
                    foreach (var parameter in axis.Parameters)
                    {
                        try
                        {
                            // Construct the command using the axis letter prefix if available.
                            string prefix = string.IsNullOrEmpty(axis.AxisLetter) ? "" : axis.AxisLetter + ":";
                            string command = $"{prefix}{parameter.Command}=?";
                            Debug.WriteLine($"Sending command: {command}");
                            Port.WriteLine(command);

                            // Wait for the response.
                            string response = string.Empty;
                            try
                            {
                                response = Port.ReadLine();
                                Debug.WriteLine($"Received response: {response}");
                            }
                            catch (TimeoutException)
                            {
                                Debug.WriteLine($"Timeout waiting for response to: {command}");
                                continue; // Skip to the next parameter
                            }

                            // Build expected command prefix for response comparison.
                            string expectedCommand = string.IsNullOrEmpty(axis.AxisLetter)
                                ? parameter.Command
                                : $"{axis.AxisLetter}:{parameter.Command}";

                            if (response.StartsWith(expectedCommand, StringComparison.OrdinalIgnoreCase))
                            {
                                var parts = response.Split('=');
                                if (parts.Length == 2 && double.TryParse(parts[1], out var rawValue))
                                {
                                    double convertedValue = rawValue;

                                    // Apply command-specific conversions.
                                    switch (parameter.Command)
                                    {
                                        case "SSPD":
                                        case "MSPD":
                                        case "ISPD":
                                            convertedValue = rawValue / 1000;
                                            break;
                                        case "LLIM":
                                        case "HLIM":
                                        case "ZON1":
                                        case "ZON2":
                                            // Conversion: encoder units to mm.
                                            convertedValue = Math.Round(rawValue / 1000000 * axis.Resolution, 3);
                                            break;
                                        case "CFRQ":
                                            convertedValue = rawValue / 10; // Hz to kHz conversion.
                                            break;
                                        default:
                                            break;
                                    }

                                    parameter.Value = convertedValue;
                                    Debug.WriteLine($"Updated {parameter.Name} to {convertedValue} (raw: {rawValue})");
                                }
                                else
                                {
                                    Debug.WriteLine($"Invalid response format for {parameter.Name}: {response}");
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"Unexpected response for {parameter.Name}: {response}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error loading parameter {parameter.Name}: {ex.Message}");
                        }

                        // Small delay before the next command.
                        await Task.Delay(0);
                    }
                }
            }
            finally
            {
                if (Running)
                {
                    Port.DataReceived += DataReceivedHandler;
                }
                LoadingSettings = false;
                Port.WriteLine("INFO=3");
            }
        }

        #endregion

        #region Command Sending
        public async Task SendCommand(string command, string axisLetter = "")
        {
            // Determine the command prefix if an axis letter is provided.
            string prefix = string.IsNullOrEmpty(axisLetter) ? "" : axisLetter + ":";
            string commandToSend = prefix + command;

            if (Port.IsOpen)
            {
                Port.Write(commandToSend);
                Debug.WriteLine($"Sending Command: {commandToSend}");
            }
            else
            {
                Debug.WriteLine("Serial port not open. Command not sent.");
            }
        }

        #endregion

        #region Static Methods
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
        #endregion

        #region UI Helpers
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
        #endregion
    }
}
