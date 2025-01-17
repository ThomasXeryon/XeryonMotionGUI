﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Reflection.Metadata;
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

        private bool _loadingSettings;
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

        private string _FriendlyName;
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

        // Initialize the controller
        public async Task InitializeAsync()
        {
            if (Running)
            {
                Port.DataReceived += DataReceivedHandler;
            }
            await LoadParametersFromController();
        }

        // Data handling
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            try
            {
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
            catch (Exception)
            {
                throw;
            }
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

        public async Task SendCommand(string command)
        {
            if (Port.IsOpen)
            {
                Port.Write(command);
                Debug.WriteLine($"Sending Command: {command}");
            }
            else
            {
                Debug.WriteLine("Serial port not open. Command not sent.");
            }
        }

        public void SendSetting(string commandName, double value, int resolution)
        {
            if (Port.IsOpen)
            {
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
                        value = Math.Round(value * 1000000 / resolution, 0); // Convert and round to 3 decimal points
                        break;
                    case "CFRQ":
                        value = value * 10;
                        break;
                    default:
                        break;
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

        public async Task LoadParametersFromController()
        {
            LoadingSettings = true;
            Port.DataReceived -= DataReceivedHandler;
            Port.WriteLine("INFO=0");
            Port.DiscardInBuffer();
            await Task.Delay(100);
            Port.DiscardInBuffer();
            Port.DiscardOutBuffer();
            Port.ReadTimeout = 200;
            try
            {
                foreach (var axis in Axes)
                {
                    foreach (var parameter in axis.Parameters)
                    {
                        try
                        {
                            // Send the command with =?
                            string command = $"{parameter.Command}=?";
                            Debug.WriteLine($"Sending command: {command}");
                            Port.WriteLine(command);

                            // Wait for the response
                            string response = string.Empty;
                            try
                            {
                                response = Port.ReadLine(); // Read response
                                Debug.WriteLine($"Received response: {response}");
                            }
                            catch (TimeoutException)
                            {
                                Debug.WriteLine($"Timeout waiting for response to: {command}");
                                continue; // Skip to the next parameter
                            }

                            if (response.StartsWith(parameter.Command, StringComparison.OrdinalIgnoreCase))
                            {
                                var parts = response.Split('=');
                                if (parts.Length == 2 && double.TryParse(parts[1], out var rawValue))
                                {
                                    double convertedValue = rawValue;

                                    // Apply command-specific conversions
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
                                            convertedValue = Math.Round(rawValue / 1000000 * axis.Resolution, 3);
                                            break;
                                        case "CFRQ":
                                            convertedValue = rawValue / 10; // Convert Hz to kHz
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

                        // Small delay before the next command
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
                Port.WriteLine("INFO=7");
            }
        }
    }
}