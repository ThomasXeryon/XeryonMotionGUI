using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using XeryonMotionGUI.Classes;

namespace XeryonMotionGUI
{
    public sealed partial class DraggableElement : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Debug.WriteLine($"PropertyChanged triggered for {propertyName}");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static readonly DependencyProperty WaitTimeProperty =
        DependencyProperty.Register(
            nameof(WaitTime),
            typeof(int),
            typeof(DraggableElement),
            new PropertyMetadata(0));

        private int _waitTime;
        public int WaitTime
        {
            get => _waitTime;
            set
            {
                if (_waitTime != value)
                {
                    _waitTime = value;
                    OnPropertyChanged();
                    SetValue(WaitTimeProperty, value);
                    WaitTime = (int)WaitTimeInput.Value; // Force an update in the backing field

                    Debug.WriteLine($"WaitTime updated to: {_waitTime}");
                }
            }
        }

        public static readonly DependencyProperty RepeatCountProperty =
    DependencyProperty.Register(
        nameof(RepeatCount),
        typeof(int),
        typeof(DraggableElement),
        new PropertyMetadata(1)); // Default to 1

        private int _repeatCount = 1; // Default value
        public int RepeatCount
        {
            get => _repeatCount;
            set
            {
                if (_repeatCount != value)
                {
                    _repeatCount = value;
                    OnPropertyChanged();
                    SetValue(RepeatCountProperty, value);
                    Debug.WriteLine($"RepeatCount set to: {_repeatCount}");
                }
            }
        }

        // Define the RunningControllers DependencyProperty
        public static readonly DependencyProperty RunningControllersProperty =
            DependencyProperty.Register(
                nameof(RunningControllers),
                typeof(ObservableCollection<Controller>),
                typeof(DraggableElement),
                new PropertyMetadata(null));

        public ObservableCollection<Controller> RunningControllers
        {
            get => (ObservableCollection<Controller>)GetValue(RunningControllersProperty);
            set => SetValue(RunningControllersProperty, value);
        }

        public DraggableElement NextBlock
        {
            get; set;
        } // The block snapped below
        public DraggableElement PreviousBlock
        {
            get; set;
        } // The block snapped above

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(DraggableElement),
                new PropertyMetadata(string.Empty, OnTextChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty SelectedControllerProperty =
            DependencyProperty.Register(
                nameof(SelectedController),
                typeof(Controller),
                typeof(DraggableElement),
                new PropertyMetadata(null, OnSelectedControllerChanged));

        public Controller SelectedController
        {
            get => (Controller)GetValue(SelectedControllerProperty);
            set => SetValue(SelectedControllerProperty, value);
        }

        public static readonly DependencyProperty SelectedAxisProperty =
            DependencyProperty.Register(
                nameof(SelectedAxis),
                typeof(Axis),
                typeof(DraggableElement),
                new PropertyMetadata(null));

        public Axis SelectedAxis
        {
            get => (Axis)GetValue(SelectedAxisProperty);
            set => SetValue(SelectedAxisProperty, value);
        }

        public DraggableElement()
        {
            this.InitializeComponent();
            this.DataContext = this; // Set DataContext to enable binding
            Debug.WriteLine($"DataContext set to: {this.DataContext}");
        }

        // Handle block actions based on block type
        public async Task ExecuteActionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Set the block as executing
                IsExecuting = true;

                // Skip SelectedAxis check for "Wait" blocks
                if (Text != "Wait" && SelectedAxis == null)
                {
                    Debug.WriteLine("No axis selected for the block.");
                    return; // Skip this block if no axis is selected
                }

                switch (Text)
                {
                    case "Step +":
                        SelectedAxis.StepSize = 1000; // Set step size to 1
                        SelectedAxis.StepPositive(); // Call StepPositive on the selected axis
                        break;
                    case "Step -":
                        SelectedAxis.StepSize = 1000; // Set step size to 1
                        SelectedAxis.StepNegative(); // Call StepNegative on the selected axis
                        break;
                    case "Scan Left":
                        SelectedAxis.ScanNegative(); // Call ScanNegative on the selected axis
                        break;
                    case "Scan Right":
                        SelectedAxis.ScanPositive(); // Call ScanPositive on the selected axis
                        break;
                    case "Move Left":
                        SelectedAxis.MoveNegative(); // Call MoveNegative on the selected axis
                        break;
                    case "Move Right":
                        SelectedAxis.MovePositive(); // Call MovePositive on the selected axis
                        break;
                    case "Wait":
                        if (cancellationToken.IsCancellationRequested)
                            return; // Exit immediately if cancellation is requested

                        Debug.WriteLine($"Waiting for {WaitTime} ms...");
                        await Task.Delay(WaitTime, cancellationToken); // Use cancellation token
                        break;
                    case "Repeat":
                        ExecuteRepeat(); // Implement repeat logic if needed
                        break;
                    case "Home":
                        SelectedAxis.Home();
                        break;
                    default:
                        Debug.WriteLine($"Unknown block type: {Text}");
                        break;
                }
            }
            finally
            {
                // Reset the executing state
                IsExecuting = false;
            }
        }

        // Define actions for each block type
        private void ExecuteStepPlus()
        {
            Debug.WriteLine("Step + action executed.");
            // Add logic for "Step +" here
        }

        private void ExecuteStepMinus()
        {
            Debug.WriteLine("Step - action executed.");
            // Add logic for "Step -" here
        }

        private void ExecuteTurnLeft()
        {
            Debug.WriteLine("Turn Left action executed.");
            // Add logic for "Turn Left" here
        }

        private void ExecuteTurnRight()
        {
            Debug.WriteLine("Turn Right action executed.");
            // Add logic for "Turn Right" here
        }

        private async Task ExecuteWaitAsync()
        {
            Debug.WriteLine($"Executing wait block with WaitTime: {WaitTime}");

            if (WaitTime <= 0)
            {
                Debug.WriteLine("Invalid wait time. Please set a positive wait time.");
                return;
            }

            Debug.WriteLine($"Waiting for {WaitTime} ms...");
            await Task.Delay(WaitTime); // Delay for the specified wait time
            Debug.WriteLine("Wait completed.");
        }



        private async void ExecuteRepeat()
        {
            Debug.WriteLine("Repeat action executed.");

            if (RepeatCount <= 0)
            {
                Debug.WriteLine("Invalid RepeatCount. Must be greater than 0.");
                return;
            }

            // Collect all the blocks above this block
            var blocksToRepeat = new List<DraggableElement>();
            var currentBlock = PreviousBlock;

            while (currentBlock != null)
            {
                blocksToRepeat.Insert(0, currentBlock); // Insert at the start to maintain order
                currentBlock = currentBlock.PreviousBlock;
            }

            if (blocksToRepeat.Count == 0)
            {
                Debug.WriteLine("No blocks above to repeat.");
                return;
            }

            Debug.WriteLine($"Repeating {blocksToRepeat.Count} blocks {RepeatCount} times.");

            // Execute the blocks the specified number of times
            for (int i = 0; i < RepeatCount; i++)
            {
                Debug.WriteLine($"Starting repetition {i + 1}...");

                foreach (var block in blocksToRepeat)
                {
                    Debug.WriteLine($"Executing block: {block.Text}");
                    block.IsExecuting = true; // Set executing state
                    await block.ExecuteActionAsync();
                    block.IsExecuting = false; // Reset executing state
                }

                Debug.WriteLine($"Repetition {i + 1} complete.");
            }
        }

        // Handle text changes (optional)
        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var block = d as DraggableElement;
            if (block != null)
            {
                Debug.WriteLine($"Block text changed to: {block.Text}");

                // Only set the default if WaitTime has not been initialized
                if (block.Text == "Wait" && block.WaitTime == 0)
                {
                    block.WaitTime = 1000;
                }
            }
        }
        private void WaitTimeInput_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            // Use the new value directly, ensuring it's converted to an integer
            WaitTime = (int)args.NewValue;

            // Debug to confirm the new value
            Debug.WriteLine($"WaitTimeInput ValueChanged: New WaitTime = {WaitTime}");
        }

        private void RepeatInput_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            // Use the new value directly, ensuring it's converted to an integer
            RepeatCount = (int)args.NewValue - 1;

            // Debug to confirm the new value
            Debug.WriteLine($"Repeat time ValueChanged: New RepeatTime = {RepeatCount}");
        }


        // Handle selected controller changes
        private static void OnSelectedControllerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var block = d as DraggableElement;
            if (block != null)
            {
                block.SelectedAxis = null; // Clear selected axis when controller changes
            }
        }

        public static readonly DependencyProperty IsExecutingProperty =
    DependencyProperty.Register(
        nameof(IsExecuting),
        typeof(bool),
        typeof(DraggableElement),
        new PropertyMetadata(false, OnIsExecutingChanged));

        public bool IsExecuting
        {
            get => (bool)GetValue(IsExecutingProperty);
            set => SetValue(IsExecutingProperty, value);
        }

        private static void OnIsExecutingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var block = d as DraggableElement;
            if (block != null)
            {
                block.UpdateVisualState();
            }
        }

        private void UpdateVisualState()
        {
            var border = this.FindName("BlockBorder") as Border;
            if (border != null)
            {
                if (IsExecuting)
                {
                    // Apply a green fade background when executing
                    border.Background = new SolidColorBrush(Microsoft.UI.Colors.LightGreen);
                }
                else
                {
                    // Revert to the default background when not executing
                    border.Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray);
                }
            }
        }
    }

}