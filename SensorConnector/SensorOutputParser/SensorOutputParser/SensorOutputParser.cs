using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using SensorConnector.Common;
using SensorConnector.Common.CommonClasses;
using SensorConnector.Common.Entities;
using SensorConnector.Persistence;
using SensorOutputParser.Exporting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SensorOutputParser.SensorOutputParser
{
    /// <summary>
    /// Gets sensor's datatype Json-schema from db. <br/>
    /// Parses datatype schema and associate it with the sensor. <br/>
    /// Parses sensor outputs according to their datatypes.
    /// </summary>
    public static class SensorOutputParser
    {
        /// <summary>
        /// Stores all distinct sensors with their parsed datatype. 
        /// </summary>
        public static List<SensorWithParsedDatatype> SensorWithParsedDatatypes { get; set; } = new List<SensorWithParsedDatatype>();

        /// <summary>
        /// Gets sensor's datatype Json-schema from db. <br/>
        /// Puts distinct sensors with their parsed datatype in <i>SensorWithParsedDatatypes</i> list.
        /// </summary>
        /// <param name="sensors">List of sensors to retrieve from database and parse their datatypes.</param>
        /// <returns></returns>
        public static async Task ParseSensorsDatatypeAsync(List<Sensor> sensors)
        {
            foreach (var sensor in sensors)
            {
                if (SensorWithParsedDatatypes
                    .Any(x => x.IpAddress == sensor.IpAddress && x.Port == sensor.Port))
                {
                    continue;
                }

                await using var context = new DmsDbContext(AppSettings.SensorOutputParser.PostgresConnectionString);

                SensorConnector.Persistence.Entities.Sensor sensorWithDatatype = null;

                try
                {
                    sensorWithDatatype = await context.Sensors
                        .Include(x => x.DataType)
                        .Include(x => x.CommunicationProtocol)
                        .FirstOrDefaultAsync(x => x.IpAddress == sensor.IpAddress &&
                                                  x.Port == sensor.Port);
                }
                catch (Npgsql.PostgresException e)
                {
                    var composedErrorMessage = $"Failed to connect to the Postgres database.\r\n";

                    if (e.SqlState != null && e.SqlState.Equals("42P01"))
                    {
                        composedErrorMessage = composedErrorMessage + 
                                               $"There is no schema with the specified \'{AppSettings.SensorOutputParser.PostgresSchemaName}\' name in database.";

                        throw new Npgsql.PostgresException(composedErrorMessage, e.Severity, e.InvariantSeverity, e.SqlState);
                    }

                    composedErrorMessage = composedErrorMessage +
                                           $"{e.Message}";

                    throw new Npgsql.PostgresException(composedErrorMessage, e.Severity, e.InvariantSeverity, e.SqlState);
                }

                if (sensorWithDatatype == null)
                {
                    throw new NullReferenceException(($"Sensor {sensor.IpAddress}:{sensor.Port} was not found in database."));
                }

                var parsedJson = JObject.Parse(sensorWithDatatype.DataType.Schema);

                var sensorFieldDescriptions = new List<SensorFieldDescription>();

                foreach (var property in parsedJson.Properties())
                {
                    var fieldName = property.Name;
                    var fieldTypeName = property.Value.ToString();

                    sensorFieldDescriptions.Add(
                        new SensorFieldDescription(fieldName, fieldTypeName));
                }

                SensorWithParsedDatatypes.Add(
                    new SensorWithParsedDatatype()
                    {
                        SensorId = sensorWithDatatype.SensorId,
                        IpAddress = sensorWithDatatype.IpAddress,
                        Port = sensorWithDatatype.Port,
                        FieldDescriptions = sensorFieldDescriptions
                    });
            }
        }


        /// <summary>
        /// Parses raw data of given sensor outputs using the existing sensors binding to their datatypes. <br/>
        /// It is assumed that sensors outputs raw data a string where sensor's values are separated by comma. <br/>
        /// Pattern: <i>{fieldValue1},{fieldValue2}, ... ,{fieldValueN}</i>
        /// </summary>
        /// <param name="sensorOutputs">List of sensors outputs that were already retrieved from Influx database.</param>
        /// <returns>List of parsed sensors outputs according the sensors datatypes.</returns>
        public static List<SensorOutputForExport> ParseRetrievedData(List<SensorOutput> sensorOutputs)
        {
            var result = new List<SensorOutputForExport>();

            foreach (var sensorOutput in sensorOutputs)
            {
                var sensorFieldValues = new List<SensorFieldValue>();

                var relatedSensor = SensorWithParsedDatatypes
                    .FirstOrDefault(x =>
                        x.IpAddress.Equals(sensorOutput.SensorIpAddress) && x.Port.Equals(sensorOutput.SensorPort));

                if (relatedSensor == null)
                {
                    continue;
                }

                var rawData = sensorOutput.Data;
                var splitRawData = rawData.Split(',');

                if (splitRawData.Length != relatedSensor.FieldDescriptions.Count)
                {
                    continue;
                }

                for (var i = 0; i < splitRawData.Length; i++)
                {
                    var fieldDescription = relatedSensor.FieldDescriptions[i];

                    var fieldValue = new SensorFieldValue()
                    {
                        FieldName = fieldDescription.FieldName
                    };

                    switch (fieldDescription.FieldTypeName)
                    {
                        case AppSettings.SensorOutputParser.AllowedTypeNames.Int:
                            {
                                if (int.TryParse(splitRawData[i], out var intValue))
                                {
                                    fieldValue.FieldValue = intValue as int?;
                                }

                                break;
                            }

                        case AppSettings.SensorOutputParser.AllowedTypeNames.Double:
                            {
                                if (double.TryParse(
                                    splitRawData[i],
                                    NumberStyles.Any,
                                    CultureInfo.InvariantCulture,
                                    out var doubleValue))
                                {
                                    fieldValue.FieldValue = doubleValue as double?;
                                }

                                break;
                            }

                        case AppSettings.SensorOutputParser.AllowedTypeNames.String:
                            {
                                fieldValue.FieldValue = splitRawData[i] as string;

                                break;
                            }

                        case AppSettings.SensorOutputParser.AllowedTypeNames.Bool:
                            {
                                if (int.TryParse(splitRawData[i], out var boolAsIntValue))
                                {
                                    fieldValue.FieldValue = Convert.ToBoolean(boolAsIntValue);

                                    break;

                                }

                                if (bool.TryParse(splitRawData[i], out var boolValue))
                                {
                                    fieldValue.FieldValue = boolValue as bool?;
                                }

                                break;
                            }
                    }

                    sensorFieldValues.Add(fieldValue);
                }

                var outputForExport = new SensorOutputForExport()
                {
                    CollectedAt = sensorOutput.Timestamp,
                    SensorId = relatedSensor.SensorId,
                    IpAddress = sensorOutput.SensorIpAddress,
                    Port = sensorOutput.SensorPort,
                    ParsedData = sensorFieldValues
                };

                result.Add(outputForExport);
            }

            return result;
        }
    }
}
