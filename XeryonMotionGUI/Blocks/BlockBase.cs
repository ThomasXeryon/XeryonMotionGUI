using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Dispatching;
using XeryonMotionGUI;
using XeryonMotionGUI.Classes;

public abstract class BlockBase : INotifyPropertyChanged
{
    public DraggableElement UiElement
    {
        get; set;
    } // Reference to the UI element

    protected DispatcherQueue _dispatcherQueue;

    // Provide a public method or property to set it
    public void SetDispatcherQueue(DispatcherQueue queue)
    {
        _dispatcherQueue = queue;
    }


    private Controller _selectedController;
    public Controller SelectedController
    {
        get => _selectedController;
        set
        {
            if (_selectedController != value)
            {
                _selectedController = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Axes)); // Notify that Axes has changed
            }
        }
    }

    public ObservableCollection<Axis> Axes => SelectedController?.Axes;

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Common properties
    private string _text;
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                OnPropertyChanged();
            }
        }
    }

    public void InitializeControllerAndAxis(ObservableCollection<Controller> runningControllers)
    {
        if (runningControllers != null && runningControllers.Count > 0)
        {
            // Set the first available controller
            SelectedController = runningControllers[0];

            if (SelectedController.Axes != null && SelectedController.Axes.Count > 0)
            {
                // Set the first available axi
                SelectedAxis = SelectedController.Axes[0];
            }
        }
    }

    public bool RequiresAxis { get; protected set; } = true; // Default to true, override in child classes if not needed

    public virtual Axis SelectedAxis
    {
        get; set;
    }

    // Helper property for validation
    public bool HasValidAxisSelection => !RequiresAxis || (SelectedController != null && SelectedAxis != null);

    // Properties for chaining blocks
    private BlockBase _nextBlock;
    public BlockBase NextBlock
    {
        get => _nextBlock;
        set
        {
            if (_nextBlock != value)
            {
                _nextBlock = value;
                OnPropertyChanged();
            }
        }
    }

    private BlockBase _previousBlock;
    public BlockBase PreviousBlock
    {
        get => _previousBlock;
        set
        {
            if (_previousBlock != value)
            {
                _previousBlock = value;
                OnPropertyChanged();
            }
        }
    }

    // Block execution logic
    public virtual async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (RequiresAxis && !HasValidAxisSelection)
            throw new InvalidOperationException("Controller and Axis must be selected.");
    }

    public double Width { get; set; } = 150; // Default width
    public double Height { get; set; } = 50; // Default height
}
