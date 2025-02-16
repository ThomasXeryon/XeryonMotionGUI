using System.Collections.ObjectModel;

namespace XeryonMotionGUI.Classes
{
    public static class ParameterFactory
    {
        public static ObservableCollection<Parameter> CreateParameters(string controllerType, string axisType)
        {
            switch (controllerType)
            {
                case "XD-OEM Single Axis Controller":
                    return CreateOEMParameters(axisType);
                case "XD-C Single Axis Controller":
                    return CreateXDCParameters(axisType);
                case "XD-M Multi Axis Controller":
                    return CreateXDMParameters(axisType);
                case "XD-19 Multi Axis Controller":
                    return CreateXD19Parameters(axisType);
                case "XWS Multi Axis Controller":
                    return CreateXWSParameters(axisType);
                case "INTG Single Axis Integrated Controller":
                    return CreateINTGParameters(axisType);
                default:
                    return CreateDefaultParameters();
            }
        }

        private static ObservableCollection<Parameter> CreateOEMParameters(string axisType)
        {
            var parameters = new ObservableCollection<Parameter>
            {
                // For each parameter, set "Category" from your comment
                new Parameter(0, 1, 0.001, 0, "Zone 1 Size:", "ZON1",
                              category: "Advanced tuning",
                              explanation: "Defines how large the first zone is.") ,

                new Parameter(0, 1, 0.001, 0, "Zone 2 Size:", "ZON2",
                              category: "Advanced tuning",
                              explanation: "Defines how large the second zone is.") ,

                new Parameter(0, 190000, 500, 0, "Zone 1 Frequency:", "FREQ",
                              category: "Motion",
                              explanation: "Frequency used in the first zone.") ,

                new Parameter(0, 190000, 500, 0, "Zone 2 Frequency:", "FRQ2",
                              category: "Motion",
                              explanation: "Frequency used in the second zone.") ,

                new Parameter(0, 200, 5, 0, "Zone 1 Proportional:", "PROP",
                              category: "Motion") ,
                new Parameter(0, 200, 5, 0, "Zone 2 Proportional:", "PRO2",
                              category: "Motion") ,
                new Parameter(0, 200, 2, 0, "Position Tolerance:", "PTOL",
                              category: "Motion") ,
                new Parameter(0, 200, 2, 0, "Position Tolerance 2:", "PTO2",
                              category: "Motion") ,

                new Parameter(0, 5000, 1000, 0, "Position Timout:", "TOUT",
                              category: "Time outs and error handling") ,
                new Parameter(0, 5000, 1, 0, "Position Timout 2:", "TOU2",
                              category: "Time outs and error handling") ,
                new Parameter(0, 5000, 1000, 0, "Position Timout 3:", "TOU3",
                              category: "Time outs and error handling") ,

                new Parameter(0, 5000, 1000, 0, "Contrl Loop Dead Time:", "DTIM",
                              category: "Advanced tuning") ,

                new Parameter(0, 1000, 5, 0, "Speed:", "SSPD",
                              category: "Motion",
                              explanation: "Maximum speed in closed loop.") ,

                new Parameter(0, 1000, 5, 0, "Indexing Speed:", "ISPD",
                              category: "Motion") ,
                new Parameter(0, 65500, 1000, 0, "Acceleration:", "ACCE",
                              category: "Motion") ,
                new Parameter(0, 65500, 1000, 0, "Deceleration:", "DECE",
                              category: "Motion") ,
                new Parameter(0, 2000, 100, 0, "Mass:", "CFRQ",
                              category: "Motion") ,
                new Parameter(0, 1, 1, 0, "Amplitude Control:", "AMPL",
                              category: "Advanced tuning") ,

                new Parameter(-2000, 0, 1, 0, "Left Soft Limit:", "LLIM",
                              category: "Motion") ,
                new Parameter(0, 2000, 1, 0, "Right Soft Limit:", "HLIM",
                              category: "Motion") ,

                new Parameter(0, 1, 1, 0, "Phase Correction:", "PHAS",
                              category: "Advanced tuning") ,
                new Parameter(0, 1000, 1, 0, "Error Limit:", "ELIM",
                              category: "Time outs and error handling") ,
                new Parameter(1, 1000, 5, 25, "Polling Interval:", "POLI",
                              category: "Advanced tuning") ,
                new Parameter(1, 1000, 5, 25, "Position Delay:", "DLAY",
                              category: "Advanced tuning") ,
                new Parameter(0, 76800, 2400, 0, "UART:", "UART",
                              category: "GPIO") ,
                new Parameter(0, 12, 1, 0, "GPIO Mode:", "GPIO",
                              category: "GPIO") ,
                new Parameter(0, 1000000, 1, 0, "Encoder Offset:", "ENCO",
                              category: "Advanced tuning") ,
                new Parameter(0, 1, 1, 0, "Encoder Direction:", "ENCD",
                              category: "Advanced tuning") ,
                new Parameter(0, 1, 1, 0, "Actuator Direction:", "ACTD",
                              category: "Advanced tuning") ,
                new Parameter(0, 65500,5 , 0, "Open Loop Amplitude:", "AMPL",
                              category: "Advanced tuning") ,
                new Parameter(0, 65500,5 , 0, "Max Open Loop Amplitude:", "MAMP",
                              category: "Advanced tuning") ,
                new Parameter(0, 65500,5 , 0, "Min Open Loop Amplitude:", "MIMP",
                              category: "Advanced tuning") ,
                new Parameter(0, 200, 5, 0, "Integrational Factortor:", "INTF",
                              category: "Advanced tuning") ,
                new Parameter(0, 1000000, 1, 0, "Index Error Limit:", "ILIM",
                              category: "Time outs and error handling") ,
                new Parameter(0, 1000000, 1, 0, "Error Saturation Limit:", "SLIM",
                              category: "Time outs and error handling") ,
                //new Parameter(0, 65500, 1, 0, "Duty Cycle Factor:", "DUTY",
                              //category: "Advanced tuning") ,
                new Parameter(0, 1, 1, 0, "Duty Control:", "DUCO",
                              category: "Advanced tuning") ,
                new Parameter(0, 1, 1, 0, "Phase Correction:", "PHAC",
                              category: "Advanced tuning") ,
/*                new Parameter(0, 1000000, 1, 0, "Trigger Pitch:", "TRGP",
                              category: "Triggering" ,
                              explanation: "Defines how large the first zone is.") ,
                new Parameter(0, 1000000, 1, 0, "Trigger Width:", "TRGW",
                              category: "Triggering" ,
                explanation: "Defines how large the first zone is.") ,
                new Parameter(0, 1000000, 1, 0, "Trigger Start:", "TRGS",
                              category: "Triggering" ,
                explanation: "Defines how large the first zone is.") ,
                new Parameter(0, 1000000, 1, 0, "Trigger Count:", "TRGN",
                              category: "Triggering",
                explanation: "Defines how large the first zone is.") ,*/
                new Parameter(0, 1000000, 1, 0, "Step Size:", "STPS",
                              category: "GPIO") ,
                /*new Parameter(0, 1, 1, 1, "Enable Encoder:", "ENON",
                              category: "Time outs and error handling") ,*/
                new Parameter(0, 1, 1, 1, "Enable After Reboot:", "ENBR",
                              category: "Time outs and error handling") ,
                new Parameter(0, 1, 1, 1, "Enable:", "ENBL",
/*                              category: "Time outs and error handling") ,
                new Parameter(0, 1, 1, 0, "Disable Piezo Signals:", "ZERO",*/
                              category: "Time outs and error handling") ,
                new Parameter(0, 1, 1, 0, "Test Leds:", "TEST",
                              category: "Time outs and error handling") ,
                new Parameter(0, 1, 1, 1, "Stop For Error:", "BLCK",
                              category: "Time outs and error handling") ,
            };

            AddCommonAxisParameters(parameters, axisType);
            return parameters;
        }

        // -------------- Other CreateXDCParameters / CreateXDMParameters, etc. -------------
        // For each parameter in those methods, do the same: set Category = "Motion", "Advanced tuning", etc.

        private static ObservableCollection<Parameter> CreateXDCParameters(string axisType)
        {
            // Insert your code, also adding the category for each parameter
            // ...
            var parameters = new ObservableCollection<Parameter>
            {
                // For each parameter, set "Category" from your comment
                new Parameter(0, 1, 0.001, 0, "Zone 1 Size:", "ZON1",
                              category: "Advanced tuning",
                              explanation: "Defines how large the first zone is.") ,

                new Parameter(0, 1, 0.001, 0, "Zone 2 Size:", "ZON2",
                              category: "Advanced tuning",
                              explanation: "Defines how large the second zone is.") ,

                new Parameter(0, 190000, 500, 0, "Zone 1 Frequency:", "FREQ",
                              category: "Motion",
                              explanation: "Frequency used in the first zone.") ,

                new Parameter(0, 190000, 500, 0, "Zone 2 Frequency:", "FRQ2",
                              category: "Motion",
                              explanation: "Frequency used in the second zone.") ,

                new Parameter(0, 200, 5, 0, "Zone 1 Proportional:", "PROP",
                              category: "Motion") ,
                new Parameter(0, 200, 5, 0, "Zone 2 Proportional:", "PRO2",
                              category: "Motion") ,
                new Parameter(0, 200, 2, 0, "Position Tolerance:", "PTOL",
                              category: "Motion") ,
                new Parameter(0, 200, 2, 0, "Position Tolerance 2:", "PTO2",
                              category: "Motion") ,

                new Parameter(0, 5000, 1000, 0, "Position Timout:", "TOUT",
                              category: "Time outs and error handling") ,
                new Parameter(0, 5000, 1, 0, "Position Timout 2:", "TOU2",
                              category: "Time outs and error handling") ,
                new Parameter(0, 5000, 1000, 0, "Position Timout 3:", "TOU3",
                              category: "Time outs and error handling") ,

                new Parameter(0, 5000, 1000, 0, "Contrl Loop Dead Time:", "DTIM",
                              category: "Advanced tuning") ,

                new Parameter(0, 1000, 5, 0, "Speed:", "SSPD",
                              category: "Motion",
                              explanation: "Maximum speed in closed loop.") ,

                new Parameter(0, 1000, 5, 0, "Indexing Speed:", "ISPD",
                              category: "Motion") ,
                new Parameter(0, 65500, 1000, 0, "Acceleration:", "ACCE",
                              category: "Motion") ,
                new Parameter(0, 65500, 1000, 0, "Deceleration:", "DECE",
                              category: "Motion") ,
                new Parameter(0, 2000, 100, 0, "Mass:", "CFRQ",
                              category: "Motion") ,
                new Parameter(0, 1, 1, 0, "Amplitude Control:", "AMPL",
                              category: "Advanced tuning") ,

                new Parameter(-5000, 0, 1, 0, "Left Soft Limit:", "LLIM",
                              category: "Motion") ,
                new Parameter(0, 5000, 1, 0, "Right Soft Limit:", "HLIM",
                              category: "Motion") ,

                new Parameter(0, 1, 1, 0, "Phase Correction:", "PHAS",
                              category: "Advanced tuning") ,
                new Parameter(0, 1000, 1, 0, "Error Limit:", "ELIM",
                              category: "Time outs and error handling") ,
                new Parameter(1, 1000, 5, 25, "Polling Interval:", "POLI",
                              category: "Advanced tuning") ,
                new Parameter(1, 1000, 5, 25, "Position Delay:", "DLAY",
                              category: "Advanced tuning") ,
                new Parameter(0, 76800, 2400, 0, "UART:", "UART",
                              category: "GPIO") ,
                new Parameter(0, 12, 1, 0, "GPIO Mode:", "GPIO",
                              category: "GPIO") ,
                new Parameter(0, 1000000, 1, 0, "Encoder Offset:", "ENCO",
                              category: "Advanced tuning") ,
                new Parameter(0, 1, 1, 0, "Encoder Direction:", "ENCD",
                              category: "Advanced tuning") ,
                new Parameter(0, 1, 1, 0, "Actuator Direction:", "ACTD",
                              category: "Advanced tuning") ,
                new Parameter(0, 65500,5 , 0, "Open Loop Amplitude:", "AMPL",
                              category: "Advanced tuning") ,
                new Parameter(0, 65500,5 , 0, "Max Open Loop Amplitude:", "MAMP",
                              category: "Advanced tuning") ,
                new Parameter(0, 65500,5 , 0, "Min Open Loop Amplitude:", "MIMP",
                              category: "Advanced tuning") ,
                new Parameter(0, 200, 5, 0, "Integrational Factortor:", "INTF",
                              category: "Advanced tuning") ,
                new Parameter(0, 1000000, 1, 0, "Index Error Limit:", "ILIM",
                              category: "Time outs and error handling") ,
                new Parameter(0, 1000000, 1, 0, "Error Saturation Limit:", "SLIM",
                              category: "Time outs and error handling") ,
                //new Parameter(0, 65500, 1, 0, "Duty Cycle Factor:", "DUTY",
                              //category: "Advanced tuning") ,
                new Parameter(0, 1, 1, 0, "Duty Control:", "DUCO",
                              category: "Advanced tuning") ,
                new Parameter(0, 1, 1, 0, "Phase Correction:", "PHAC",
                              category: "Advanced tuning") ,
/*                new Parameter(0, 1000000, 1, 0, "Trigger Pitch:", "TRGP",
                              category: "Triggering" ,
                              explanation: "Defines how large the first zone is.") ,
                new Parameter(0, 1000000, 1, 0, "Trigger Width:", "TRGW",
                              category: "Triggering" ,
                explanation: "Defines how large the first zone is.") ,
                new Parameter(0, 1000000, 1, 0, "Trigger Start:", "TRGS",
                              category: "Triggering" ,
                explanation: "Defines how large the first zone is.") ,
                new Parameter(0, 1000000, 1, 0, "Trigger Count:", "TRGN",
                              category: "Triggering",
                explanation: "Defines how large the first zone is.") ,*/
                new Parameter(0, 1000000, 1, 0, "Step Size:", "STPS",
                              category: "GPIO") ,
                /*new Parameter(0, 1, 1, 1, "Enable Encoder:", "ENON",
                              category: "Time outs and error handling") ,*/
                new Parameter(0, 1, 1, 1, "Enable After Reboot:", "ENBR",
                              category: "Time outs and error handling") ,
                new Parameter(0, 1, 1, 1, "Enable:", "ENBL",
/*                              category: "Time outs and error handling") ,
                new Parameter(0, 1, 1, 0, "Disable Piezo Signals:", "ZERO",*/
                              category: "Time outs and error handling") ,
                new Parameter(0, 1, 1, 0, "Test Leds:", "TEST",
                              category: "Time outs and error handling") ,
                new Parameter(0, 1, 1, 1, "Stop For Error:", "BLCK",
                              category: "Time outs and error handling") ,
            };
            // add them with the "category" argument
            AddCommonAxisParameters(parameters, axisType);
            return parameters;
        }

        private static ObservableCollection<Parameter> CreateXDMParameters(string axisType)
        {
            // similarly
            var parameters = new ObservableCollection<Parameter>
            {
                // For each parameter, set "Category" from your comment
                new Parameter(0, 1, 0.001, 0, "Zone 1 Size:", "ZON1",
                              category: "Advanced tuning",
                              explanation: "Defines how large the first zone is.") ,

                new Parameter(0, 1, 0.001, 0, "Zone 2 Size:", "ZON2",
                              category: "Advanced tuning",
                              explanation: "Defines how large the second zone is.") ,

                new Parameter(0, 190000, 500, 0, "Zone 1 Frequency:", "FREQ",
                              category: "Motion",
                              explanation: "Frequency used in the first zone.") ,

                new Parameter(0, 190000, 500, 0, "Zone 2 Frequency:", "FRQ2",
                              category: "Motion",
                              explanation: "Frequency used in the second zone.") ,

                new Parameter(0, 200, 5, 0, "Zone 1 Proportional:", "PROP",
                              category: "Motion") ,
                new Parameter(0, 200, 5, 0, "Zone 2 Proportional:", "PRO2",
                              category: "Motion") ,
                new Parameter(0, 200, 2, 0, "Position Tolerance:", "PTOL",
                              category: "Motion") ,
                new Parameter(0, 200, 2, 0, "Position Tolerance 2:", "PTO2",
                              category: "Motion") ,

                new Parameter(0, 5000, 1000, 0, "Position Timout:", "TOUT",
                              category: "Time outs and error handling") ,
                new Parameter(0, 5000, 1, 0, "Position Timout 2:", "TOU2",
                              category: "Time outs and error handling") ,
                new Parameter(0, 5000, 1000, 0, "Position Timout 3:", "TOU3",
                              category: "Time outs and error handling") ,

                new Parameter(0, 5000, 1000, 0, "Contrl Loop Dead Time:", "DTIM",
                              category: "Advanced tuning") ,

                new Parameter(0, 1000, 5, 0, "Speed:", "SSPD",
                              category: "Motion",
                              explanation: "Maximum speed in closed loop.") ,

                new Parameter(0, 1000, 5, 0, "Indexing Speed:", "ISPD",
                              category: "Motion") ,
                new Parameter(0, 65500, 1000, 0, "Acceleration:", "ACCE",
                              category: "Motion") ,
                new Parameter(0, 65500, 1000, 0, "Deceleration:", "DECE",
                              category: "Motion") ,
                new Parameter(0, 2000, 100, 0, "Mass:", "CFRQ",
                              category: "Motion") ,
                new Parameter(0, 1, 1, 0, "Amplitude Control:", "AMPL",
                              category: "Advanced tuning") ,

new Parameter(-2000, 0, 1, 0, "Left Soft Limit:", "LLIM",
                              category: "Motion") ,
                new Parameter(0, 2000, 1, 0, "Right Soft Limit:", "HLIM",
                              category: "Motion") ,

                new Parameter(0, 1, 1, 0, "Phase Correction:", "PHAS",
                              category: "Advanced tuning") ,
                new Parameter(0, 1000, 1, 0, "Error Limit:", "ELIM",
                              category: "Time outs and error handling") ,
                new Parameter(1, 1000, 5, 25, "Polling Interval:", "POLI",
                              category: "Advanced tuning") ,
                new Parameter(1, 1000, 5, 25, "Position Delay:", "DLAY",
                              category: "Advanced tuning") ,
                new Parameter(0, 76800, 2400, 0, "UART:", "UART",
                              category: "GPIO") ,
                new Parameter(0, 12, 1, 0, "GPIO Mode:", "GPIO",
                              category: "GPIO") ,
                new Parameter(0, 1000000, 1, 0, "Encoder Offset:", "ENCO",
                              category: "Advanced tuning") ,
                new Parameter(0, 1, 1, 0, "Encoder Direction:", "ENCD",
                              category: "Advanced tuning") ,
                new Parameter(0, 1, 1, 0, "Actuator Direction:", "ACTD",
                              category: "Advanced tuning") ,
                new Parameter(0, 65500,5 , 0, "Open Loop Amplitude:", "AMPL",
                              category: "Advanced tuning") ,
                new Parameter(0, 65500,5 , 0, "Max Open Loop Amplitude:", "MAMP",
                              category: "Advanced tuning") ,
                new Parameter(0, 65500,5 , 0, "Min Open Loop Amplitude:", "MIMP",
                              category: "Advanced tuning") ,
                new Parameter(0, 200, 5, 0, "Integrational Factortor:", "INTF",
                              category: "Advanced tuning") ,
                new Parameter(0, 1000000, 1, 0, "Index Error Limit:", "ILIM",
                              category: "Time outs and error handling") ,
                new Parameter(0, 1000000, 1, 0, "Error Saturation Limit:", "SLIM",
                              category: "Time outs and error handling") ,
                //new Parameter(0, 65500, 1, 0, "Duty Cycle Factor:", "DUTY",
                              //category: "Advanced tuning") ,
                new Parameter(0, 1, 1, 0, "Duty Control:", "DUCO",
                              category: "Advanced tuning") ,
                new Parameter(0, 1, 1, 0, "Phase Correction:", "PHAC",
                              category: "Advanced tuning") ,
/*                new Parameter(0, 1000000, 1, 0, "Trigger Pitch:", "TRGP",
                              category: "Triggering" ,
                              explanation: "Defines how large the first zone is.") ,
                new Parameter(0, 1000000, 1, 0, "Trigger Width:", "TRGW",
                              category: "Triggering" ,
                explanation: "Defines how large the first zone is.") ,
                new Parameter(0, 1000000, 1, 0, "Trigger Start:", "TRGS",
                              category: "Triggering" ,
                explanation: "Defines how large the first zone is.") ,
                new Parameter(0, 1000000, 1, 0, "Trigger Count:", "TRGN",
                              category: "Triggering",
                explanation: "Defines how large the first zone is.") ,*/
                new Parameter(0, 1000000, 1, 0, "Step Size:", "STPS",
                              category: "GPIO") ,
                /*new Parameter(0, 1, 1, 1, "Enable Encoder:", "ENON",
                              category: "Time outs and error handling") ,*/
                new Parameter(0, 1, 1, 1, "Enable After Reboot:", "ENBR",
                              category: "Time outs and error handling") ,
                new Parameter(0, 1, 1, 1, "Enable:", "ENBL",
/*                              category: "Time outs and error handling") ,
                new Parameter(0, 1, 1, 0, "Disable Piezo Signals:", "ZERO",*/
                              category: "Time outs and error handling") ,
                new Parameter(0, 1, 1, 0, "Test Leds:", "TEST",
                              category: "Time outs and error handling") ,
                new Parameter(0, 1, 1, 1, "Stop For Error:", "BLCK",
                              category: "Time outs and error handling") ,
            };
            AddCommonAxisParameters(parameters, axisType);
            return parameters;
        }

        private static ObservableCollection<Parameter> CreateXD19Parameters(string axisType)
        {
            // similarly
            var parameters = new ObservableCollection<Parameter>
            {
                // For each parameter, set "Category" from your comment
                new Parameter(0, 1, 0.001, 0, "Zone 1 Size:", "ZON1",
                              category: "Advanced tuning",
                              explanation: "Defines how large the first zone is.") ,

                new Parameter(0, 1, 0.001, 0, "Zone 2 Size:", "ZON2",
                              category: "Advanced tuning",
                              explanation: "Defines how large the second zone is.") ,

                new Parameter(0, 190000, 500, 0, "Zone 1 Frequency:", "FREQ",
                              category: "Motion",
                              explanation: "Frequency used in the first zone.") ,

                new Parameter(0, 190000, 500, 0, "Zone 2 Frequency:", "FRQ2",
                              category: "Motion",
                              explanation: "Frequency used in the second zone.") ,

                new Parameter(0, 200, 5, 0, "Zone 1 Proportional:", "PROP",
                              category: "Motion") ,
                new Parameter(0, 200, 5, 0, "Zone 2 Proportional:", "PRO2",
                              category: "Motion") ,
                new Parameter(0, 200, 2, 0, "Position Tolerance:", "PTOL",
                              category: "Motion") ,
                new Parameter(0, 200, 2, 0, "Position Tolerance 2:", "PTO2",
                              category: "Motion") ,

                new Parameter(0, 5000, 1000, 0, "Position Timout:", "TOUT",
                              category: "Time outs and error handling") ,
                new Parameter(0, 5000, 1, 0, "Position Timout 2:", "TOU2",
                              category: "Time outs and error handling") ,
                new Parameter(0, 5000, 1000, 0, "Position Timout 3:", "TOU3",
                              category: "Time outs and error handling") ,

                new Parameter(0, 5000, 1000, 0, "Contrl Loop Dead Time:", "DTIM",
                              category: "Advanced tuning") ,

                new Parameter(0, 1000, 5, 0, "Speed:", "SSPD",
                              category: "Motion",
                              explanation: "Maximum speed in closed loop.") ,

                new Parameter(0, 1000, 5, 0, "Indexing Speed:", "ISPD",
                              category: "Motion") ,
                new Parameter(0, 65500, 1000, 0, "Acceleration:", "ACCE",
                              category: "Motion") ,
                new Parameter(0, 65500, 1000, 0, "Deceleration:", "DECE",
                              category: "Motion") ,
                new Parameter(0, 2000, 100, 0, "Mass:", "CFRQ",
                              category: "Motion") ,
                new Parameter(0, 1, 1, 0, "Amplitude Control:", "AMPL",
                              category: "Advanced tuning") ,

new Parameter(-2000, 0, 1, 0, "Left Soft Limit:", "LLIM",
                              category: "Motion") ,
                new Parameter(0, 2000, 1, 0, "Right Soft Limit:", "HLIM",
                              category: "Motion") ,

                new Parameter(0, 1, 1, 0, "Phase Correction:", "PHAS",
                              category: "Advanced tuning") ,
                new Parameter(0, 1000, 1, 0, "Error Limit:", "ELIM",
                              category: "Time outs and error handling") ,
                new Parameter(1, 1000, 5, 25, "Polling Interval:", "POLI",
                              category: "Advanced tuning") ,
                new Parameter(1, 1000, 5, 25, "Position Delay:", "DLAY",
                              category: "Advanced tuning") ,
                new Parameter(0, 76800, 2400, 0, "UART:", "UART",
                              category: "GPIO") ,
                new Parameter(0, 12, 1, 0, "GPIO Mode:", "GPIO",
                              category: "GPIO") ,
                new Parameter(0, 1000000, 1, 0, "Encoder Offset:", "ENCO",
                              category: "Advanced tuning") ,
                new Parameter(0, 1, 1, 0, "Encoder Direction:", "ENCD",
                              category: "Advanced tuning") ,
                new Parameter(0, 1, 1, 0, "Actuator Direction:", "ACTD",
                              category: "Advanced tuning") ,
                new Parameter(0, 65500,5 , 0, "Open Loop Amplitude:", "AMPL",
                              category: "Advanced tuning") ,
                new Parameter(0, 65500,5 , 0, "Max Open Loop Amplitude:", "MAMP",
                              category: "Advanced tuning") ,
                new Parameter(0, 65500,5 , 0, "Min Open Loop Amplitude:", "MIMP",
                              category: "Advanced tuning") ,
                new Parameter(0, 200, 5, 0, "Integrational Factortor:", "INTF",
                              category: "Advanced tuning") ,
                new Parameter(0, 1000000, 1, 0, "Index Error Limit:", "ILIM",
                              category: "Time outs and error handling") ,
                new Parameter(0, 1000000, 1, 0, "Error Saturation Limit:", "SLIM",
                              category: "Time outs and error handling") ,
                //new Parameter(0, 65500, 1, 0, "Duty Cycle Factor:", "DUTY",
                              //category: "Advanced tuning") ,
                new Parameter(0, 1, 1, 0, "Duty Control:", "DUCO",
                              category: "Advanced tuning") ,
                new Parameter(0, 1, 1, 0, "Phase Correction:", "PHAC",
                              category: "Advanced tuning") ,
/*                new Parameter(0, 1000000, 1, 0, "Trigger Pitch:", "TRGP",
                              category: "Triggering" ,
                              explanation: "Defines how large the first zone is.") ,
                new Parameter(0, 1000000, 1, 0, "Trigger Width:", "TRGW",
                              category: "Triggering" ,
                explanation: "Defines how large the first zone is.") ,
                new Parameter(0, 1000000, 1, 0, "Trigger Start:", "TRGS",
                              category: "Triggering" ,
                explanation: "Defines how large the first zone is.") ,
                new Parameter(0, 1000000, 1, 0, "Trigger Count:", "TRGN",
                              category: "Triggering",
                explanation: "Defines how large the first zone is.") ,*/
                new Parameter(0, 1000000, 1, 0, "Step Size:", "STPS",
                              category: "GPIO") ,
                /*new Parameter(0, 1, 1, 1, "Enable Encoder:", "ENON",
                              category: "Time outs and error handling") ,*/
                new Parameter(0, 1, 1, 1, "Enable After Reboot:", "ENBR",
                              category: "Time outs and error handling") ,
                new Parameter(0, 1, 1, 1, "Enable:", "ENBL",
/*                              category: "Time outs and error handling") ,
                new Parameter(0, 1, 1, 0, "Disable Piezo Signals:", "ZERO",*/
                              category: "Time outs and error handling") ,
                new Parameter(0, 1, 1, 0, "Test Leds:", "TEST",
                              category: "Time outs and error handling") ,
                new Parameter(0, 1, 1, 1, "Stop For Error:", "BLCK",
                              category: "Time outs and error handling") ,
            };
            AddCommonAxisParameters(parameters, axisType);
            return parameters;
        }

        private static ObservableCollection<Parameter> CreateXWSParameters(string axisType)
        {
            var parameters = new ObservableCollection<Parameter>
            {
                new Parameter(0, 1, 0.01, 0.1, "Zone 2 Size:", category:"Advanced tuning"),
                new Parameter(0, 64400, 1000, 32000, "Acceleration:", category:"Motion")
            };
            AddCommonAxisParameters(parameters, axisType);
            return parameters;
        }

        private static ObservableCollection<Parameter> CreateINTGParameters(string axisType)
        {
            var parameters = new ObservableCollection<Parameter>
            {
                new Parameter(0, 1, 1, 1, "Phase Correction:", category:"Advanced tuning"),
                new Parameter(-200, 0, 1, -100, "Left Soft Limit:", category:"Motion")
            };
            AddCommonAxisParameters(parameters, axisType);
            return parameters;
        }

        private static ObservableCollection<Parameter> CreateDefaultParameters()
        {
            return new ObservableCollection<Parameter>
            {
                /*new Parameter(0, 1, 0.01, 0.01, "Zone 1 Size:", category:"Motion"),
                new Parameter(0, 1, 0.01, 0.1, "Zone 2 Size:", category:"Motion"),
                new Parameter(0, 200, 5, 90, "Zone 1 Proportional:", category:"Motion"),
                new Parameter(0, 400, 5, 200, "Speed:", category:"Motion"),
                new Parameter(0, 64400, 1000, 32000, "Acceleration:", category:"Motion")*/
            };
        }

        private static void AddCommonAxisParameters(ObservableCollection<Parameter> parameters, string axisType)
        {
            if (axisType == "Rotational")
            {
                // e.g.
                parameters.Add(new Parameter(0, 1, 1, 1, "Shortest Path:", "PATH",
                                             category: "Motion",
                                             explanation: "Moves via the smallest rotation angle"));
            }
            else if (axisType == "Linear")
            {
                // e.g.
                // parameters.Add(new Parameter(..., category:"Motion"));
            }
        }
    }
}
