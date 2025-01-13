using System.Collections.ObjectModel;

namespace XeryonMotionGUI.Classes
{
    public static class ParameterFactory
    {
        /// <summary>
        /// Creates parameters based on the specified controller type and axis type.
        /// </summary>
        /// <param name="controllerType">The type of controller for which to create parameters.</param>
        /// <param name="axisType">Optional axis type to customize parameters further (e.g., Linear or Rotational).</param>
        /// <returns>A collection of parameters tailored to the controller and axis type.</returns>
        public static ObservableCollection<Parameter> CreateParameters(string controllerType, string axisType)
        {
            switch (controllerType)
            {
                case "OEM":
                    return CreateOEMParameters(axisType);
                case "XD-C":
                    return CreateXDCParameters(axisType);
                case "XD-M":
                    return CreateXDMParameters(axisType);
                case "XD-19":
                    return CreateXD19Parameters(axisType);
                case "XWS":
                    return CreateXWSParameters(axisType);
                case "INTG":
                    return CreateINTGParameters(axisType);
                default:
                    return CreateDefaultParameters();
            }
        }

        private static ObservableCollection<Parameter> CreateOEMParameters(string axisType)
        {
            var parameters = new ObservableCollection<Parameter>
            {
                new Parameter(0, 1, 0.01, 0.01, "Zone 1 Size:", "ZON1"),
                new Parameter(0, 1, 0.01, 0.1, "Zone 2 Size:", "ZON2"),
                new Parameter(0, 185000, 1000, 85000, "Zone 1 Frequency:", "FREQ"),
                new Parameter(0, 185000, 1000, 83000, "Zone 2 Frequency:", "FRQ2"),
                new Parameter(0, 200, 5, 90, "Zone 1 Proportional:", "PROP"),
                new Parameter(0, 200, 5, 45, "Zone 2 Proportional:", "PRO2"),
                new Parameter(0, 200, 2, 2, "Position Tolerance:", "PTOL"),
                new Parameter(0, 200, 2, 4, "Position Tolerance 2:", "PTO2"),
                new Parameter(0, 5000, 1000, 10, "Position Timout:", "TOUT"),
                new Parameter(0, 400, 5, 200, "Speed:", "SSPD"),
                new Parameter(0, 64400, 1000, 32000, "Acceleration:", "ACCE"),
                new Parameter(0, 64400, 1000, 32000, "Deceleration:", "DECE"),
                new Parameter(0, 1500, 100, 0, "Mass:", "MASS"),
                new Parameter(0, 1, 1, 1, "Amplitude Control:", "AMPL"),
                new Parameter(-200, 0, 1, -100, "Left Soft Limit:", "LLIM"),
                new Parameter(0, 200, 1, 100, "Right Soft Limit:", "RLIM"),
                new Parameter(0, 1, 1, 1, "Phase Correction:", "PHAS"),
                new Parameter(0, 1000, 1, 50, "Error Limit:", "ELIM")
            };

            AddCommonAxisParameters(parameters, axisType);
            return parameters;
        }

        private static ObservableCollection<Parameter> CreateXDCParameters(string axisType)
        {
            var parameters = new ObservableCollection<Parameter>
            {
                new Parameter(0, 1, 0.01, 0.01, "Zone 1 Size:", "ZON1"),
                new Parameter(0, 1, 0.01, 0.1, "Zone 2 Size:", "ZON2"),
                new Parameter(0, 185000, 1000, 85000, "Zone 1 Frequency:", "FREQ"),
                new Parameter(0, 185000, 1000, 83000, "Zone 2 Frequency:", "FRQ2"),
                new Parameter(0, 200, 5, 90, "Zone 1 Proportional:", "PROP"),
                new Parameter(0, 200, 5, 45, "Zone 2 Proportional:", "PRO2"),
                new Parameter(0, 200, 2, 2, "Position Tolerance:", "PTOL"),
                new Parameter(0, 200, 2, 4, "Position Tolerance 2:", "PTO2"),
                new Parameter(0, 5000, 1000, 10, "Position Timout:", "TOUT"),
                new Parameter(0, 400, 5, 200, "Speed:", "SSPD"),
                new Parameter(0, 64400, 1000, 32000, "Acceleration:", "ACCE"),
                new Parameter(0, 64400, 1000, 32000, "Deceleration:", "DECE"),
                new Parameter(0, 1500, 100, 0, "Mass:", "MASS"),
                new Parameter(0, 1, 1, 1, "Amplitude Control:", "AMPL"),
                new Parameter(-200, 0, 1, -100, "Left Soft Limit:", "LLIM"),
                new Parameter(0, 200, 1, 100, "Right Soft Limit:", "RLIM"),
                new Parameter(0, 1, 1, 1, "Phase Correction:", "PHAS"),
                new Parameter(0, 1000, 1, 50, "Error Limit:", "ELIM")
            };
            AddCommonAxisParameters(parameters, axisType);
            return parameters;
        }

        private static ObservableCollection<Parameter> CreateXDMParameters(string axisType)
        {
            var parameters = new ObservableCollection<Parameter>
            {
                new Parameter(0, 1, 0.01, 0.01, "Zone 1 Size:", "ZON1"),
                new Parameter(0, 1, 0.01, 0.1, "Zone 2 Size:", "ZON2"),
                new Parameter(0, 185000, 1000, 85000, "Zone 1 Frequency:", "FREQ"),
                new Parameter(0, 185000, 1000, 83000, "Zone 2 Frequency:", "FRQ2"),
                new Parameter(0, 200, 5, 90, "Zone 1 Proportional:", "PROP"),
                new Parameter(0, 200, 5, 45, "Zone 2 Proportional:", "PRO2"),
                new Parameter(0, 200, 2, 2, "Position Tolerance:", "PTOL"),
                new Parameter(0, 200, 2, 4, "Position Tolerance 2:", "PTO2"),
                new Parameter(0, 5000, 1000, 10, "Position Timout:", "TOUT"),
                new Parameter(0, 400, 5, 200, "Speed:", "SSPD"),
                new Parameter(0, 64400, 1000, 32000, "Acceleration:", "ACCE"),
                new Parameter(0, 64400, 1000, 32000, "Deceleration:", "DECE"),
                new Parameter(0, 1500, 100, 0, "Mass:", "MASS"),
                new Parameter(0, 1, 1, 1, "Amplitude Control:", "AMPL"),
                new Parameter(-200, 0, 1, -100, "Left Soft Limit:", "LLIM"),
                new Parameter(0, 200, 1, 100, "Right Soft Limit:", "RLIM"),
                new Parameter(0, 1, 1, 1, "Phase Correction:", "PHAS"),
                new Parameter(0, 1000, 1, 50, "Error Limit:", "ELIM")
            }; 

            AddCommonAxisParameters(parameters, axisType);
            return parameters;
        }

        private static ObservableCollection<Parameter> CreateXD19Parameters(string axisType)
        {
            var parameters = new ObservableCollection<Parameter>
            {
                new Parameter(0, 1, 0.01, 0.01, "Zone 1 Size:", "ZON1"),
                new Parameter(0, 1, 0.01, 0.1, "Zone 2 Size:", "ZON2"),
                new Parameter(0, 185000, 1000, 85000, "Zone 1 Frequency:", "FREQ"),
                new Parameter(0, 185000, 1000, 83000, "Zone 2 Frequency:", "FRQ2"),
                new Parameter(0, 200, 5, 90, "Zone 1 Proportional:", "PROP"),
                new Parameter(0, 200, 5, 45, "Zone 2 Proportional:", "PRO2"),
                new Parameter(0, 200, 2, 2, "Position Tolerance:", "PTOL"),
                new Parameter(0, 200, 2, 4, "Position Tolerance 2:", "PTO2"),
                new Parameter(0, 5000, 1000, 10, "Position Timout:", "TOUT"),
                new Parameter(0, 400, 5, 200, "Speed:", "SSPD"),
                new Parameter(0, 64400, 1000, 32000, "Acceleration:", "ACCE"),
                new Parameter(0, 64400, 1000, 32000, "Deceleration:", "DECE"),
                new Parameter(0, 1500, 100, 0, "Mass:", "MASS"),
                new Parameter(0, 1, 1, 1, "Amplitude Control:", "AMPL"),
                new Parameter(-200, 0, 1, -100, "Left Soft Limit:", "LLIM"),
                new Parameter(0, 200, 1, 100, "Right Soft Limit:", "RLIM"),
                new Parameter(0, 1, 1, 1, "Phase Correction:", "PHAS"),
                new Parameter(0, 1000, 1, 50, "Error Limit:", "ELIM")
            };

            AddCommonAxisParameters(parameters, axisType);
            return parameters;
        }

        private static ObservableCollection<Parameter> CreateXWSParameters(string axisType)
        {
            var parameters = new ObservableCollection<Parameter>
            {
                new Parameter(0, 1, 0.01, 0.1, "Zone 2 Size:"),
                new Parameter(0, 64400, 1000, 32000, "Acceleration:")
            };

            AddCommonAxisParameters(parameters, axisType);
            return parameters;
        }

        private static ObservableCollection<Parameter> CreateINTGParameters(string axisType)
        {
            var parameters = new ObservableCollection<Parameter>
            {
                new Parameter(0, 1, 1, 1, "Phase Correction:"),
                new Parameter(-200, 0, 1, -100, "Left Soft Limit:")
            };

            AddCommonAxisParameters(parameters, axisType);
            return parameters;
        }

        private static ObservableCollection<Parameter> CreateDefaultParameters()
        {
            return new ObservableCollection<Parameter>
            {
                new Parameter(0, 1, 0.01, 0.01, "Zone 1 Size:"),
                new Parameter(0, 1, 0.01, 0.1, "Zone 2 Size:"),
                new Parameter(0, 200, 5, 90, "Zone 1 Proportional:"),
                new Parameter(0, 400, 5, 200, "Speed:"),
                new Parameter(0, 64400, 1000, 32000, "Acceleration:")
            };
        }

        private static void AddCommonAxisParameters(ObservableCollection<Parameter> parameters, string axisType)
        {
            if (axisType == "Rotational")
            {
                //parameters.Add(new Parameter(0, 360, 1, 180, "Rotation Angle:"));
            }
            else if (axisType == "Linear")
            {
                //parameters.Add(new Parameter(0, 100, 0.5, 50, "Travel Distance:"));
            }
        }
    }
}
