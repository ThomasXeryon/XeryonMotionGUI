using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XeryonMotionGUI.Models; // Add this
using Microsoft.UI.Dispatching;
using XeryonMotionGUI.Classes;
using XeryonMotionGUI.Helpers;
using XeryonMotionGUI.Views;

namespace XeryonMotionGUI.Blocks
{
    public class StepBlock : BlockBase
    {


        private readonly DeviationStats _deviationStats = new DeviationStats();
        public DeviationStats DeviationStats => _deviationStats;
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

        private bool _isPositive = true; // Default to positive direction

        public bool IsPositive
        {
            get => _isPositive;
            set
            {
                if (_isPositive != value)
                {
                    _isPositive = value;
                    Debug.WriteLine($"StepBlock.IsPositive set to: {_isPositive}");
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

            // Highlight the block in the UI
            _dispatcherQueue?.TryEnqueue(() => UiElement?.HighlightBlock(true));

            try
            {
                double finalStepVal = IsPositive ? StepSize : -StepSize;
                // 1) Capture the desired endpoint BEFORE calling TakeStep,
                //    rounding the encoder values to whole numbers.
                int oldEPOS = (int)Math.Round(SelectedAxis.EPOS);
                int target = oldEPOS + (int)Math.Round(UnitConversion.ToEncoder(finalStepVal, SelectedUnit, SelectedAxis.Resolution));

                // 2) Execute the actual step
                await SelectedAxis.TakeStep(finalStepVal, SelectedUnit, SelectedAxis.Resolution);

                // 3) Once done, measure final deviation
                int finalEPOS = (int)Math.Round(SelectedAxis.EPOS);
                int diff = Math.Abs(finalEPOS - target);

                // 4) Update local deviation stats (these remain as integers)
                _deviationStats.Count++;
                _deviationStats.SumDeviation += diff;
                if (diff < _deviationStats.MinDeviation)
                    _deviationStats.MinDeviation = diff;
                if (diff > _deviationStats.MaxDeviation)
                    _deviationStats.MaxDeviation = diff;
            }
            finally
            {
                // Stop highlighting
                _dispatcherQueue?.TryEnqueue(() => UiElement?.HighlightBlock(false));
            }
        }

    }
}
