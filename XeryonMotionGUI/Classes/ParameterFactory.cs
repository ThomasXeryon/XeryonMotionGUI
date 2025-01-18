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
                new Parameter(0, 1, 0.001, 0, "Zone 1 Size:", "ZON1"),
                new Parameter(0, 1, 0.001, 0, "Zone 2 Size:", "ZON2"),
                new Parameter(0, 190000, 500, 0, "Zone 1 Frequency:", "FREQ"),
                new Parameter(0, 190000, 500, 0, "Zone 2 Frequency:", "FRQ2"),
                //new Parameter(0, 190000, 500, 0, "Upper Frequency Limit:", "HFRQ"),
                //new Parameter(0, 190000, 500, 0, "Lower Frequency Limit:", "LFRQ"),
                new Parameter(0, 200, 5, 0, "Zone 1 Proportional:", "PROP"),
                new Parameter(0, 200, 5, 0, "Zone 2 Proportional:", "PRO2"),
                new Parameter(0, 200, 2, 0, "Position Tolerance:", "PTOL"),
                new Parameter(0, 200, 2, 0, "Position Tolerance 2:", "PTO2"),
                new Parameter(0, 5000, 1000, 0, "Position Timout:", "TOUT"), //ms
                new Parameter(0, 5000, 1, 0, "Position Timout 2:", "TOU2"), //s
                new Parameter(0, 5000, 1000, 0, "Position Timout 3:", "TOU3"), //ms
                new Parameter(0, 5000, 1000, 0, "Contrl Loop Dead Time:", "DTIM"),
                new Parameter(0, 1000, 5, 0, "Speed:", "SSPD"),
                new Parameter(0, 1000, 5, 0, "Indexing Speed:", "ISPD"),
                new Parameter(0, 65500, 1000, 0, "Acceleration:", "ACCE"),
                new Parameter(0, 65500, 1000, 0, "Deceleration:", "DECE"),
                new Parameter(0, 2000, 100, 0, "Mass:", "CFRQ"),
                new Parameter(0, 1, 1, 0, "Amplitude Control:", "AMPL"),
                new Parameter(-200, 0, 1, 0, "Left Soft Limit:", "LLIM"),
                new Parameter(0, 200, 1, 0, "Right Soft Limit:", "HLIM"),
                new Parameter(0, 1, 1, 0, "Phase Correction:", "PHAS"),
                new Parameter(0, 1000, 1, 0, "Error Limit:", "ELIM"),
                new Parameter(1, 1000, 5, 25, "Polling Interval:", "POLI"),
                new Parameter(1, 1000, 5, 25, "Position Delay:", "DLAY"),
                new Parameter(0, 76800, 2400, 0, "UART:", "UART"),
                new Parameter(0, 12, 1, 0, "GPIO Mode:", "GPIO"),
                new Parameter(0, 1000000, 1, 0, "Encoder Offset:", "ENCO"),
                new Parameter(0, 1, 1, 0, "Encoder Direction:", "ENCD"),
                new Parameter(0, 1, 1, 0, "Actuator Direction:", "ACTD"),
                new Parameter(0, 65500,5 , 0, "Open Loop Amplitude:", "AMPL"),
                new Parameter(0, 65500,5 , 0, "Max Open Loop Amplitude:", "MAMP"),
                new Parameter(0, 65500,5 , 0, "Min Open Loop Amplitude:", "MIMP"),
                new Parameter(0, 200, 5, 0, "Integrational Favtor:", "INTF"),
                new Parameter(0, 1000000, 1, 0, "Index Error Limit:", "ILIM"),
                new Parameter(0, 1000000, 1, 0, "Error Saturation Limit:", "SLIM"),
                //new Parameter(0, 65500, 1, 0, "Duty Cycle Factor:", "DUTY"),
                new Parameter(0, 1, 1, 0, "Duty Control:", "DUCO"),
                //new Parameter(0, 1, 1, 0, "Phase Correction:", "PHAC"),
                /*new Parameter(0, 1000000, 1, 0, "Trigger Pitch:", "TRGP"),
                new Parameter(0, 1000000, 1, 0, "Trigger Width:", "TRGW"),
                new Parameter(0, 1000000, 1, 0, "Trigger Start:", "TRGS"),
                new Parameter(0, 1000000, 1, 0, "Trigger Count:", "TRGN"),*/
                new Parameter(0, 1000000, 1, 0, "Step Size:", "STPS"),
                //new Parameter(0, 1, 1, 1, "Enable Encoder:", "ENON"),
                new Parameter(0, 1, 1, 1, "Enable After Reboot:", "ENBR"),
                new Parameter(0, 1, 1, 1, "Enable:", "ENBL"),
                //new Parameter(0, 1, 1, 0, "Disable Piezo Signals:", "ZERO"),
                new Parameter(0, 1, 1, 0, "Test Leds:", "TEST"),
                new Parameter(0, 1, 1, 1, "Stop For Error:", "BLCK"),
            };

            AddCommonAxisParameters(parameters, axisType);
            return parameters;
        }

        private static ObservableCollection<Parameter> CreateXDCParameters(string axisType)
        {
            var parameters = new ObservableCollection<Parameter>
            {
                new Parameter(0, 1, 0.001, 0, "Zone 1 Size:", "ZON1"),
                new Parameter(0, 1, 0.001, 0, "Zone 2 Size:", "ZON2"),
                new Parameter(0, 190000, 500, 0, "Zone 1 Frequency:", "FREQ"),
                new Parameter(0, 190000, 500, 0, "Zone 2 Frequency:", "FRQ2"),
                new Parameter(0, 190000, 500, 0, "Upper Frequency Limit:", "HFRQ"),
                new Parameter(0, 190000, 500, 0, "Lower Frequency Limit:", "LFRQ"),
                new Parameter(0, 200, 5, 0, "Zone 1 Proportional:", "PROP"),
                new Parameter(0, 200, 5, 0, "Zone 2 Proportional:", "PRO2"),
                new Parameter(0, 200, 2, 0, "Position Tolerance:", "PTOL"),
                new Parameter(0, 200, 2, 0, "Position Tolerance 2:", "PTO2"),
                new Parameter(0, 5000, 1000, 0, "Position Timout:", "TOUT"),
                new Parameter(0, 5000, 1000, 0, "Position Timout 2:", "TOU2"),
                new Parameter(0, 5000, 1000, 0, "Position Timout 3:", "TOU3"),
                new Parameter(0, 5000, 1000, 0, "Contrl Loop Dead Time:", "DTIM"),
                new Parameter(0, 1000, 5, 0, "Speed:", "SSPD"),
                new Parameter(0, 1000, 5, 0, "Indexing Speed:", "ISPD"),
                new Parameter(0, 65500, 1000, 0, "Acceleration:", "ACCE"),
                new Parameter(0, 255, 50, 0, "Deceleration:", "DECE"),
                new Parameter(0, 2000, 100, 0, "Mass:", "CFRQ"),
                new Parameter(0, 1, 1, 0, "Amplitude Control:", "AMPL"),
                new Parameter(-200, 0, 1, 0, "Left Soft Limit:", "LLIM"),
                new Parameter(0, 200, 1, 0, "Right Soft Limit:", "HLIM"),
                new Parameter(0, 1, 1, 0, "Phase Correction:", "PHAS"),
                new Parameter(0, 1000, 1, 0, "Error Limit:", "ELIM"),
                new Parameter(1, 1000, 5, 25, "Polling Interval:", "POLI"),
                new Parameter(1, 1000, 5, 25, "Position Delay:", "DLAY"),
                new Parameter(0, 76800, 2400, 0, "UART:", "UART"),
                new Parameter(0, 12, 1, 0, "GPIO Mode:", "GPIO"),
                new Parameter(0, 1000000, 1, 0, "Encoder Offset:", "ENCO"),
                new Parameter(0, 1, 1, 0, "Encoder Direction:", "ENCD"),
                new Parameter(0, 1, 1, 0, "Actuator Direction:", "ACTD"),
                new Parameter(0, 65500,5 , 0, "Open Loop Amplitude:", "AMPL"),
                new Parameter(0, 65500,5 , 0, "Max Open Loop Amplitude:", "MAMP"),
                new Parameter(0, 65500,5 , 0, "Min Open Loop Amplitude:", "MIMP"),
                new Parameter(0, 200, 5, 0, "Integrational Favtor:", "INTF"),
                new Parameter(0, 1000000, 1, 0, "Index Error Limit:", "ILIM"),
                new Parameter(0, 1000000, 1, 0, "Error Saturation Limit:", "SLIM"),
                new Parameter(0, 65500, 1, 0, "Duty Cycle Factor:", "DUTY"),
                new Parameter(0, 1, 1, 0, "Duty Control:", "DUCO"),
                new Parameter(0, 1, 1, 0, "Phase Correction:", "PHAC"),
                new Parameter(0, 1000000, 1, 0, "Trigger Pitch:", "TRGP"),
                new Parameter(0, 1000000, 1, 0, "Trigger Width:", "TRGW"),
                new Parameter(0, 1000000, 1, 0, "Trigger Start:", "TRGS"),
                new Parameter(0, 1000000, 1, 0, "Trigger Count:", "TRGN"),
                new Parameter(0, 1000000, 1, 0, "Step Size:", "STPS"),
                //new Parameter(0, 1, 1, 1, "Enable Encoder:", "ENON"),
                new Parameter(0, 1, 1, 1, "Enable After Reboot:", "ENBR"),
                new Parameter(0, 1, 1, 1, "Enable:", "ENBL"),
                new Parameter(0, 1, 1, 0, "Disable Piezo Signals:", "ZERO"),
                new Parameter(0, 1, 1, 0, "Test Leds:", "TEST"),
                new Parameter(0, 1, 1, 1, "Stop For Error:", "BLCK"),
            };
            AddCommonAxisParameters(parameters, axisType);
            return parameters;
        }

        private static ObservableCollection<Parameter> CreateXDMParameters(string axisType)
        {
            var parameters = new ObservableCollection<Parameter>
            {
                new Parameter(0, 1, 0.001, 0, "Zone 1 Size:", "ZON1"),
                new Parameter(0, 1, 0.001, 0, "Zone 2 Size:", "ZON2"),
                new Parameter(0, 190000, 500, 0, "Zone 1 Frequency:", "FREQ"),
                new Parameter(0, 190000, 500, 0, "Zone 2 Frequency:", "FRQ2"),
                new Parameter(0, 190000, 500, 0, "Upper Frequency Limit:", "HFRQ"),
                new Parameter(0, 190000, 500, 0, "Lower Frequency Limit:", "LFRQ"),
                new Parameter(0, 200, 5, 0, "Zone 1 Proportional:", "PROP"),
                new Parameter(0, 200, 5, 0, "Zone 2 Proportional:", "PRO2"),
                new Parameter(0, 200, 2, 0, "Position Tolerance:", "PTOL"),
                new Parameter(0, 200, 2, 0, "Position Tolerance 2:", "PTO2"),
                new Parameter(0, 5000, 1000, 0, "Position Timout:", "TOUT"),
                new Parameter(0, 5000, 1000, 0, "Position Timout 2:", "TOUT2"),
                new Parameter(0, 5000, 1000, 0, "Position Timout 3:", "TOUT3"),
                new Parameter(0, 5000, 1000, 0, "Contrl Loop Dead Time:", "DTIM"),
                new Parameter(0, 1000, 5, 0, "Speed:", "SSPD"),
                new Parameter(0, 1000, 5, 0, "Indexing Speed:", "ISPD"),
                new Parameter(0, 65500, 1000, 0, "Acceleration:", "ACCE"),
                new Parameter(0, 255, 50, 0, "Deceleration:", "DECE"),
                new Parameter(0, 2000, 100, 0, "Mass:", "CFRQ"),
                new Parameter(0, 1, 1, 0, "Amplitude Control:", "AMPL"),
                new Parameter(-200, 0, 1, 0, "Left Soft Limit:", "LLIM"),
                new Parameter(0, 200, 1, 0, "Right Soft Limit:", "HLIM"),
                new Parameter(0, 1, 1, 0, "Phase Correction:", "PHAS"),
                new Parameter(0, 1000, 1, 0, "Error Limit:", "ELIM"),
                new Parameter(1, 1000, 5, 25, "Polling Interval:", "POLI"),
                new Parameter(1, 1000, 5, 25, "Position Delay:", "DLAY"),
                new Parameter(0, 76800, 2400, 0, "UART:", "UART"),
                new Parameter(0, 12, 1, 0, "GPIO Mode:", "GPIO"),
                new Parameter(0, 1000000, 1, 0, "Encoder Offset:", "ENCO"),
                new Parameter(0, 1, 1, 0, "Encoder Direction:", "ENCD"),
                new Parameter(0, 1, 1, 0, "Actuator Direction:", "ACTD"),
                new Parameter(0, 65500,5 , 0, "Open Loop Amplitude:", "AMPL"),
                new Parameter(0, 65500,5 , 0, "Max Open Loop Amplitude:", "MAMP"),
                new Parameter(0, 65500,5 , 0, "Min Open Loop Amplitude:", "MIMP"),
                new Parameter(0, 200, 5, 0, "Integrational Favtor:", "INTF"),
                new Parameter(0, 1000000, 1, 0, "Index Error Limit:", "ILIM"),
                new Parameter(0, 1000000, 1, 0, "Error Saturation Limit:", "SLIM"),
                new Parameter(0, 65500, 1, 0, "Duty Cycle Factor:", "DUTY"),
                new Parameter(0, 1, 1, 0, "Duty Control:", "DUCO"),
                new Parameter(0, 1, 1, 0, "Phase Correction:", "PHAC"),
                new Parameter(0, 1000000, 1, 0, "Trigger Pitch:", "TRGP"),
                new Parameter(0, 1000000, 1, 0, "Trigger Width:", "TRGW"),
                new Parameter(0, 1000000, 1, 0, "Trigger Start:", "TRGS"),
                new Parameter(0, 1000000, 1, 0, "Trigger Count:", "TRGN"),
                new Parameter(0, 1000000, 1, 0, "Step Size:", "STPS"),
                new Parameter(0, 1, 1, 1, "Enable Encoder:", "ENON"),
                new Parameter(0, 1, 1, 1, "Enable After Reboot:", "ENBR"),
                new Parameter(0, 1, 1, 1, "Enable:", "ENBL"),
                new Parameter(0, 1, 1, 0, "Disable Piezo Signals:", "ZERO"),
                new Parameter(0, 1, 1, 0, "Test Leds:", "TEST"),
                new Parameter(0, 1, 1, 1, "Stop For Error:", "BLCK"),
            }; 

            AddCommonAxisParameters(parameters, axisType);
            return parameters;
        }

        private static ObservableCollection<Parameter> CreateXD19Parameters(string axisType)
        {
            var parameters = new ObservableCollection<Parameter>
            {
                new Parameter(0, 1, 0.001, 0, "Zone 1 Size:", "ZON1"),
                new Parameter(0, 1, 0.001, 0, "Zone 2 Size:", "ZON2"),
                new Parameter(0, 190000, 500, 0, "Zone 1 Frequency:", "FREQ"),
                new Parameter(0, 190000, 500, 0, "Zone 2 Frequency:", "FRQ2"),
                new Parameter(0, 190000, 500, 0, "Upper Frequency Limit:", "HFRQ"),
                new Parameter(0, 190000, 500, 0, "Lower Frequency Limit:", "LFRQ"),
                new Parameter(0, 200, 5, 0, "Zone 1 Proportional:", "PROP"),
                new Parameter(0, 200, 5, 0, "Zone 2 Proportional:", "PRO2"),
                new Parameter(0, 200, 2, 0, "Position Tolerance:", "PTOL"),
                new Parameter(0, 200, 2, 0, "Position Tolerance 2:", "PTO2"),
                new Parameter(0, 5000, 1000, 0, "Position Timout:", "TOUT"),
                new Parameter(0, 5000, 1000, 0, "Position Timout 2:", "TOU2"),
                new Parameter(0, 5000, 1000, 0, "Position Timout 3:", "TOU3"),
                new Parameter(0, 5000, 1000, 0, "Contrl Loop Dead Time:", "DTIM"),
                new Parameter(0, 1000, 5, 0, "Speed:", "SSPD"),
                new Parameter(0, 1000, 5, 0, "Indexing Speed:", "ISPD"),
                new Parameter(0, 65500, 1000, 0, "Acceleration:", "ACCE"),
                new Parameter(0, 255, 50, 0, "Deceleration:", "DECE"),
                new Parameter(0, 2000, 100, 0, "Mass:", "CFRQ"),
                new Parameter(0, 1, 1, 0, "Amplitude Control:", "AMPL"),
                new Parameter(-200, 0, 1, 0, "Left Soft Limit:", "LLIM"),
                new Parameter(0, 200, 1, 0, "Right Soft Limit:", "HLIM"),
                new Parameter(0, 1, 1, 0, "Phase Correction:", "PHAS"),
                new Parameter(0, 1000, 1, 0, "Error Limit:", "ELIM"),
                new Parameter(1, 1000, 5, 25, "Polling Interval:", "POLI"),
                new Parameter(1, 1000, 5, 25, "Position Delay:", "DLAY"),
                new Parameter(0, 76800, 2400, 0, "UART:", "UART"),
                new Parameter(0, 12, 1, 0, "GPIO Mode:", "GPIO"),
                new Parameter(0, 1000000, 1, 0, "Encoder Offset:", "ENCO"),
                new Parameter(0, 1, 1, 0, "Encoder Direction:", "ENCD"),
                new Parameter(0, 1, 1, 0, "Actuator Direction:", "ACTD"),
                new Parameter(0, 65500,5 , 0, "Open Loop Amplitude:", "AMPL"),
                new Parameter(0, 65500,5 , 0, "Max Open Loop Amplitude:", "MAMP"),
                new Parameter(0, 65500,5 , 0, "Min Open Loop Amplitude:", "MIMP"),
                new Parameter(0, 200, 5, 0, "Integrational Favtor:", "INTF"),
                new Parameter(0, 1000000, 1, 0, "Index Error Limit:", "ILIM"),
                new Parameter(0, 1000000, 1, 0, "Error Saturation Limit:", "SLIM"),
                new Parameter(0, 65500, 1, 0, "Duty Cycle Factor:", "DUTY"),
                new Parameter(0, 1, 1, 0, "Duty Control:", "DUCO"),
                new Parameter(0, 1, 1, 0, "Phase Correction:", "PHAC"),
                new Parameter(0, 1000000, 1, 0, "Trigger Pitch:", "TRGP"),
                new Parameter(0, 1000000, 1, 0, "Trigger Width:", "TRGW"),
                new Parameter(0, 1000000, 1, 0, "Trigger Start:", "TRGS"),
                new Parameter(0, 1000000, 1, 0, "Trigger Count:", "TRGN"),
                new Parameter(0, 1000000, 1, 0, "Step Size:", "STPS"),
                new Parameter(0, 1, 1, 1, "Enable Encoder:", "ENON"),
                new Parameter(0, 1, 1, 1, "Enable After Reboot:", "ENBR"),
                new Parameter(0, 1, 1, 1, "Enable:", "ENBL"),
                new Parameter(0, 1, 1, 0, "Disable Piezo Signals:", "ZERO"),
                new Parameter(0, 1, 1, 0, "Test Leds:", "TEST"),
                new Parameter(0, 1, 1, 1, "Stop For Error:", "BLCK"),
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
                new Parameter(0, 1, 1, 1, "Shortest path:", "PATH");
            }
            else if (axisType == "Linear")
            {
                //parameters.Add(new Parameter(0, 100, 0.5, 50, "Travel Distance:"));
            }
        }
    }
}
