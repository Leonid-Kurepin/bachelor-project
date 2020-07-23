using SensorConnector.Common;
using SensorConnector.Common.CommonClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using static SensorConnector.Common.AppSettings;
using static SensorConnector.Common.AppSettings.SensorListener;

namespace SensorListener.CommandLineArgsParser
{
    /// <summary>
    /// Parses app-execution input params.
    /// </summary>
    public static class CommandLineArgsParser
    {
        private static readonly ParamParserHelper _paramParserHelper = new ParamParserHelper(InputParamsPattern);

        /// <summary>
        /// Parses app-execution input params according to predefined pattern. <br/>
        /// Throws <i>FormatExceptions</i> if params provided to the application does not match the pattern.
        /// </summary>
        /// <param name="inputParams">App-execution input params represented as array os string.</param>
        /// <returns>All parsed params as one object.</returns>
        public static ParsedInputParams ParseInputParams(string[] inputParams)
        {
            var parsedInputParams = new ParsedInputParams();

            var i = 0;

            if (inputParams == null || !inputParams.Any())
            {
                throw new FormatException(
                    $"No executing params were provided. " +
                    $"Use the following pattern for the program executing: \r\n \'{InputParamsPattern}\'");
            }

            _paramParserHelper.CheckParamPassed(inputParams, i, TestIdParamName);

            i++;

            _paramParserHelper.CheckParamValuePassed(inputParams, i, TestIdParamName);


            if (!int.TryParse(inputParams[1], out var testId))
            {
                throw new FormatException(
                    $"Provided value \'{inputParams[1]}\' is not a valid TestId, because it can not be parsed to int.");
            }

            parsedInputParams.TestId = Math.Abs(testId);

            i++;

            _paramParserHelper.CheckParamPassed(inputParams, i, ExecutionTimeParamName);

            i++;

            _paramParserHelper.CheckParamValuePassed(inputParams, i, ExecutionTimeParamName);

            if (!int.TryParse(inputParams[3], out var executionTime))
            {
                throw new FormatException(
                    $"Provided value \'{inputParams[3]}\' is not a valid execution time, because it can not be parsed to int.");
            }

            executionTime = Math.Abs(executionTime);

            if (executionTime > MaxExecutionTime)
            {
                throw new FormatException(
                    $"Provided value {executionTime} sec. for execution time is too large. " +
                    $"Max allowed value is {MaxExecutionTime} sec.");
            }

            parsedInputParams.ProgramExecutionTime = executionTime;

            i++;

            _paramParserHelper.CheckParamPassed(inputParams, i, SensorsParamName);

            i++;

            while (i < inputParams.Length)
            {
                _paramParserHelper.CheckParamValuePassed(inputParams, i, SensorsParamName);
                var parsedSensor = _paramParserHelper.ParseSensorFromInput(inputParams[i]);

                parsedInputParams.Sensors.Add(parsedSensor);

                i++;
            }

            return parsedInputParams;
        }
    }
}
