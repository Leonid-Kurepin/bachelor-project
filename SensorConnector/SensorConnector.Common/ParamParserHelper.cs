using SensorConnector.Common.CommonClasses;
using System;
using System.Net;
using static SensorConnector.Common.AppSettings;

namespace SensorConnector.Common
{
    public class ParamParserHelper
    {
        public string InputParamsPattern { get; set; }

        public ParamParserHelper(string inputParamsPattern)
        {
            InputParamsPattern = inputParamsPattern;
        }

        public void CheckParamPassed(string[] inputParams, int indexToCheck, string paramName)
        {
            if (indexToCheck >= inputParams.Length)
            {
                throw new FormatException(
                    $"Expected \'{paramName}\' parameter.\r\n" +
                    $"Use the following pattern for the program executing: \r\n \'{InputParamsPattern}\'");
            }

            if (!inputParams[indexToCheck].Equals(
                paramName,
                StringComparison.InvariantCultureIgnoreCase))
            {
                throw new FormatException(
                    $"Expected \'{paramName}\' but found \'{inputParams[indexToCheck]}\'.");
            }
        }

        public void CheckParamValuePassed(string[] inputParams, int indexToCheck, string paramName)
        {
            if (indexToCheck >= inputParams.Length)
            {
                throw new FormatException(
                    $"No value were found for the \'{paramName}\'\r\n" +
                    $"Use the following pattern for the program executing: \r\n \'{InputParamsPattern}\'");
            }
        }

        /// <summary>
        /// Pattern for sensor param is: {sensorIpAddress}:{sensorPort}
        /// </summary>
        /// <param name="inputParamValue"></param>
        /// <returns></returns>
        public Sensor ParseSensorFromInput(string inputParamValue)
        {
            var splitSensorInfo = inputParamValue.Split(':');

            if (splitSensorInfo.Length != 2)
            {
                throw new FormatException(
                    $"Provided value \'{inputParamValue}\' is not a valid sensor info. " +
                    "Expected pattern is \'{sensorIpAddress}:{sensorPort}\'");
            }

            var sensorIpAddress = splitSensorInfo[0];
            var sensorPortStr = splitSensorInfo[1];

            if (!IPAddress.TryParse(sensorIpAddress, out _))
            {
                throw new FormatException(
                    $"Provided value \'{sensorIpAddress}\' is not a valid sensor IP-address.");
            }

            if (!int.TryParse(sensorPortStr, out var sensorPort))
            {
                throw new FormatException(
                    $"Provided value \'{sensorPortStr}\' is not a valid sensor port, because it can not be parsed to int.");
            }

            sensorPort = Math.Abs(sensorPort);

            if (sensorPort < MinPortValue || sensorPort > MaxPortValue)
            {
                throw new FormatException(
                    $"Provided value \'{sensorPort}\' for sensor port is not allowed. " +
                    $"Allowed values are from {MinPortValue} to {MaxPortValue}");
            }

            return new Sensor(sensorIpAddress, sensorPort);
        }
    }
}
