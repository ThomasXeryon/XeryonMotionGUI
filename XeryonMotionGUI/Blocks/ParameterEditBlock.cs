using System.Diagnostics;
using XeryonMotionGUI.Classes;

public class ParameterEditBlock : BlockBase
{
    private Axis _selectedAxis;
    private string _selectedParameter;
    private double _parameterValue;

    public Axis SelectedAxis
    {
        get => _selectedAxis;
        set
        {
            if (_selectedAxis != value)
            {
                _selectedAxis = value;
                OnPropertyChanged();

                // Reset SelectedParameter when SelectedAxis changes
                SelectedParameter = null;
            }
        }
    }

    public string SelectedParameter
    {
        get => _selectedParameter;
        set
        {
            if (_selectedParameter != value)
            {
                _selectedParameter = value;
                OnPropertyChanged();

                // Update ParameterValue when SelectedParameter changes
                UpdateParameterValue();
            }
        }
    }

    public double ParameterValue
    {
        get => _parameterValue;
        set
        {
            if (_parameterValue != value)
            {
                _parameterValue = value;
                OnPropertyChanged();
            }
        }
    }

    public ParameterEditBlock()
    {
        Text = "Edit Parameter";
        RequiresAxis = true; // Requires an axis to operate
        Width = 140;
        Height = 240;
    }

    private void UpdateParameterValue()
    {
        if (SelectedAxis != null && !string.IsNullOrEmpty(SelectedParameter))
        {
            // Find the selected parameter
            var parameter = SelectedAxis.Parameters.FirstOrDefault(p => p.Command == SelectedParameter);
            if (parameter != null)
            {
                // Set the initial value of ParameterValue
                ParameterValue = parameter.Value;
            }
        }
    }

    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedAxis == null || string.IsNullOrEmpty(SelectedParameter))
        {
            Debug.WriteLine("[ParameterEditBlock] No axis or parameter selected.");
            return;
        }

        Debug.WriteLine($"[ParameterEditBlock] Setting {SelectedParameter} to {ParameterValue} on {SelectedAxis.FriendlyName}.");

        // Highlight the block
        if (this.UiElement != null)
        {
            this.UiElement.HighlightBlock(true);
        }

        try
        {
            // Find the parameter on the selected axis
            var parameter = SelectedAxis.Parameters.FirstOrDefault(p => p.Command == SelectedParameter);
            if (parameter != null)
            {
                // Update the parameter value
                parameter.Value = ParameterValue;
            }
            else
            {
                Debug.WriteLine($"[ParameterEditBlock] Parameter {SelectedParameter} not found on axis {SelectedAxis.FriendlyName}.");
            }
        }
        finally
        {
            // Remove the highlight
            if (this.UiElement != null)
            {
                this.UiElement.HighlightBlock(false);
            }
        }

        Debug.WriteLine($"[ParameterEditBlock] Parameter update completed.");
    }
}