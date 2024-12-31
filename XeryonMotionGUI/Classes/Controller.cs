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

namespace XeryonMotionGUI.Classes
{
    public class Controller : INotifyPropertyChanged
    {
        public static ObservableCollection<Controller> FoundControllers { get; set; } = new ObservableCollection<Controller>();


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

        private Axis[] _Axes;
        public Axis[] Axes
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

        public void OpenPort()
        {
            if (!Running)
            {
                Port.Open();
                Initialize();
                Running = true;
                Status = "Disconnect";
            }
            else
            {
                Port.Close();
                Running = false;
                Status = "Connect";
            }
        }
        public ICommand OpenPortCommand
        {
            get
            {
                _OpenPortCommand ??= new RelayCommand(OpenPort);
                return _OpenPortCommand;
            }
        }
    }
}
