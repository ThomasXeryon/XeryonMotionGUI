using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using XeryonMotionGUI.Classes;
using XeryonMotionGUI.Helpers;

namespace XeryonMotionGUI.Blocks
{
    public class StepBlock : BlockBase
    {
        private bool _isPositive = true; // Default to positive direction
        private double _stepSize = 1.0;
        private Units _selectedUnit = Units.mm; // default for linear

        // Backing field for axis if we want to override base.SelectedAxis
        private Axis _mySelectedAxis;

        // This override ensures we do our specialized “unit check” 
        // while still allowing the base logic to pick the default axis.
        public override Axis SelectedAxis
        {
            get => _mySelectedAxis;
            set
            {
                if (_mySelectedAxis != value)
                {
                    _mySelectedAxis = value;
                    OnPropertyChanged(nameof(SelectedAxis));
                    OnPropertyChanged(nameof(AvailableUnits));

                    // If the user’s chosen unit is not valid for this axis, switch it
                    if (_mySelectedAxis != null && !_mySelectedAxis.AvailableUnits.Contains(_selectedUnit))
                    {
                        _selectedUnit = _mySelectedAxis.AvailableUnits.FirstOrDefault();
                        OnPropertyChanged(nameof(SelectedUnit));
                    }
                }
            }
        }

        public bool IsPositive
        {
            get => _isPositive;
            set
            {
                if (_isPositive != value)
                {
                    _isPositive = value;
                    OnPropertyChanged();
                }
            }
        }

        public double StepSize
        {
            get => _stepSize;
            set
            {
                if (_stepSize != value)
                {
                    _stepSize = value;
                    OnPropertyChanged();
                }
            }
        }

        public Units SelectedUnit
        {
            get => _selectedUnit;
            set
            {
                if (_selectedUnit != value)
                {
                    _selectedUnit = value;
                    OnPropertyChanged();
                }
            }
        }

        public IEnumerable<Units> AvailableUnits
        {
            get
            {
                // If we have an axis, only show that axis’s valid units
                if (SelectedAxis != null)
                {
                    return SelectedAxis.AvailableUnits;
                }
                // Otherwise, show all possible
                return Enum.GetValues(typeof(Units)).Cast<Units>();
            }
        }

        public StepBlock()
        {
            Text = "Step";
            Width = 140;
            Height = 340;
            RequiresAxis = true;

            // Could set defaults here
            _stepSize = 1.0;
            _selectedUnit = Units.mm;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (SelectedAxis == null)
            {
                Debug.WriteLine("[StepBlock] No axis selected. Cannot execute.");
                return;
            }

            // 1) Indicate we’re executing:
            _dispatcherQueue?.TryEnqueue(() => UiElement?.HighlightBlock(true));

            try
            {
                // 2) Instead of letting the axis read its own properties,
                //    pass the numeric step, unit, and resolution directly:
                double stepValue = StepSize;          // e.g. 2.5
                Units stepUnit = SelectedUnit;        // e.g. mm
                double stepResolution = SelectedAxis.Resolution;
                bool isPositive = IsPositive;

                // If user wants negative direction, multiply by -1:
                double finalStepVal = isPositive ? stepValue : -stepValue;

                // 3) Now call the new method on the axis:
                await SelectedAxis.TakeStep(finalStepVal, stepUnit, stepResolution);
            }
            finally
            {
                // 4) Stop highlighting
                _dispatcherQueue?.TryEnqueue(() => UiElement?.HighlightBlock(false));
            }
        }

    }
}
