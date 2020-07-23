using SensorConnector.Common;
using SensorConnector.Common.CommonClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using static SensorConnector.Common.AppSettings;
using static SensorConnector.Common.AppSettings.SensorOutputParser;

namespace SensorOutputParser.CommandLineArgsParser
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
            ParsedInputParams ParsedInputParams = new ParsedInputParams
            {
                TestSensorsInfo = new List<ParsedTestSensorsInfo>()
            };

            var i = 0;

            if (inputParams == null || !inputParams.Any())
            {
                throw new FormatException(
                    $"No executing params were provided. " +
                    $"Use the following pattern for the program executing: \r\n \'{InputParamsPattern}\'");
            }

            _paramParserHelper.CheckParamPassed(inputParams, i, DirectoryPathParamName);

            i++;

            _paramParserHelper.CheckParamValuePassed(inputParams, i, DirectoryPathParamName);

            ParsedInputParams.DirectoryPath = inputParams[i];

            i++;

            _paramParserHelper.CheckParamPassed(inputParams, i, LeftTimeBorderParamName);

            i++;

            if (!DateTime.TryParse(inputParams[i], out var leftTimeBorder))
            {
                throw new FormatException(
                    $"Provided value \'{inputParams[i]}\' is not a valid DateTime value.\r\n" +
                    $"Provide time borders in ISO 8601 format. Example: 2020-05-10T07:32:29.186Z");
            }

            ParsedInputParams.LeftTimeBorder = leftTimeBorder;

            i++;

            _paramParserHelper.CheckParamPassed(inputParams, i, RightTimeBorderParamName);

            i++;

            _paramParserHelper.CheckParamValuePassed(inputParams, i, RightTimeBorderParamName);

            if (!DateTime.TryParse(inputParams[i], out var rightTimeBorder))
            {
                throw new FormatException(
                    $"Provided value \'{inputParams[i]}\' is not a valid DateTime value.\r\n" +
                    $"Provide time borders in ISO-8601 format. Example: 2020-05-10T07:32:29Z");
            }


            ParsedInputParams.RightTimeBorder = rightTimeBorder;

            // If left border is greater then right swap them
            if (ParsedInputParams.LeftTimeBorder > ParsedInputParams.RightTimeBorder)
            {
                var tmp = ParsedInputParams.RightTimeBorder;
                ParsedInputParams.RightTimeBorder = ParsedInputParams.LeftTimeBorder;
                ParsedInputParams.LeftTimeBorder = tmp;
            }

            i++;

            var testSensorsInfoIndex = 0;
            do
            {
                try
                {
                    _paramParserHelper.CheckParamPassed(inputParams, i, TestIdParamName);
                }
                catch (FormatException e)
                {
                    if (testSensorsInfoIndex == 0)
                    {
                        throw e;
                    }

                    throw new FormatException(
                        "Expected either  \'{sensorIpAddress}:{sensorPort}\' sensor info or \r\n" +
                        $"\'{TestIdParamName}\' parameter, but found {inputParams[i]}");
                }

                i++;

                _paramParserHelper.CheckParamValuePassed(inputParams, i, TestIdParamName);

                if (!int.TryParse(inputParams[i], out var testId))
                {
                    throw new FormatException(
                        $"Provided value \'{inputParams[i]}\' is not a valid TestId, because it can not be parsed to int.");
                }

                ParsedInputParams.TestSensorsInfo.Add(
                    new ParsedTestSensorsInfo()
                    {
                        TestId = Math.Abs(testId)
                    });

                i++;

                _paramParserHelper.CheckParamPassed(inputParams, i, SensorsParamName);

                i++;

                var isFirstSensorInput = true;
                do
                {

                    try
                    {
                        _paramParserHelper.CheckParamValuePassed(inputParams, i, SensorsParamName);
                    }
                    catch (FormatException e)
                    {
                        if (isFirstSensorInput)
                        {
                            throw e;
                        }

                        break;
                    }

                    Sensor parsedSensor = null;

                    try
                    {
                        parsedSensor = _paramParserHelper.ParseSensorFromInput(inputParams[i]);
                    }
                    catch (FormatException e)
                    {
                        if (isFirstSensorInput)
                        {
                            throw e;
                        }

                        break;
                    }

                    ParsedInputParams.TestSensorsInfo[testSensorsInfoIndex].Sensors.Add(parsedSensor);

                    i++;
                    isFirstSensorInput = false;

                } while (i < inputParams.Length);

                testSensorsInfoIndex++;

            } while (i < inputParams.Length);

            return ParsedInputParams;
        }
    }
}
