using SensorConnector.Common.Entities;
using SensorConnector.Common.SensorExtensions;
using SensorOutputParser.CommandLineArgsParser;
using SensorOutputParser.Exporting;
using SensorOutputParser.Queries;
using System;
using System.Threading.Tasks;
using Vibrant.InfluxDB.Client;
using static SensorConnector.Common.AppSettings;
using static SensorConnector.Common.AppSettings.SensorOutputParser;
using static SensorOutputParser.CommandLineArgsParser.CommandLineArgsParser;
using static SensorOutputParser.SensorOutputParser.SensorOutputParser;

namespace SensorOutputParser
{
    public class Program
    {
        private static ParsedInputParams _parsedInputParams;

        private static InfluxClient _influxClient;

        static int Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        static async Task<int> MainAsync(string[] args)
        {
            _influxClient = new InfluxClient(new Uri(InfluxHost));

            await _influxClient.CreateDatabaseAsync(InfluxDatabaseName); // Creates Influx database if not exist

            try
            {
                #region Test

                var isDebug = false;

                if (isDebug)
                {
                    // WARNING: Remove or comment out this part for production.
                    var testArgs = ExecutionParamsStringExample.Split(' ');
                    args = testArgs;
                }
              
                #endregion Test

                _parsedInputParams = ParseInputParams(args);
            }
            catch (Exception ex)
            {
                var errorMessage = "ERROR: execution params parsing failed.\r\n" + ex.Message;
                Console.WriteLine(errorMessage);

                return 1;
            }

            foreach (var testSensorsInfo in _parsedInputParams.TestSensorsInfo)
            {
                var query = QueryMaker.GetSensorOutputsForTest(
                    testSensorsInfo.TestId,
                    _parsedInputParams.LeftTimeBorder,
                    _parsedInputParams.RightTimeBorder,
                    testSensorsInfo.Sensors);

                var resultSet = await _influxClient.ReadAsync<SensorOutput>(InfluxDatabaseName, query);

                var results = resultSet.Results[0];
                var series = results.Series;

                if (series.Count < 1)
                {
                    Console.WriteLine(
                        $"WARNING: There were no outputs found for the specified parameters: \r\n" +
                        GetSearchConditionsString(_parsedInputParams.LeftTimeBorder, _parsedInputParams.RightTimeBorder,
                            testSensorsInfo));

                    continue;
                }

                var retrievedOutputs = series[0].Rows;

                try
                {
                    await ParseSensorsDatatypeAsync(testSensorsInfo.Sensors);
                }
                catch (Npgsql.PostgresException postgresException)
                {
                    Console.WriteLine("ERROR:" + postgresException.Message);
                    return 1;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);

                    continue;
                }

                var outputsForExport = ParseRetrievedData(retrievedOutputs);

                var jsonExport = new JsonExport(
                    testSensorsInfo.TestId,
                    _parsedInputParams.LeftTimeBorder,
                    _parsedInputParams.RightTimeBorder,
                    outputsForExport);

                var exportedFile = jsonExport.GetSensorOutputsFile();

                // var directoryPath = DefaultDirectoryPath;
                var directoryPath = _parsedInputParams.DirectoryPath;

                try
                {
                    FileWriter.WriteFileToDirectory(directoryPath, exportedFile.FileContents, exportedFile.FileName);

                    WriteSuccessMessage(
                        _parsedInputParams.LeftTimeBorder,
                        _parsedInputParams.RightTimeBorder,
                        testSensorsInfo,
                        exportedFile.FileName
                    );
                }
                catch (Exception e)
                {
                    Console.Write("ERROR: Writing to file failed with the following message: ");
                    Console.WriteLine(e);
                }
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();

            return 0;
        }

        private static string GetSearchConditionsString(DateTime leftTimeBorder,
            DateTime rightTimeBorder,
            ParsedTestSensorsInfo testSensorsInfo)
        {
            return $"{nameof(ParsedInputParams.LeftTimeBorder)}: {leftTimeBorder} \r\n" +
                   $"{nameof(ParsedInputParams.RightTimeBorder)}: {rightTimeBorder} \r\n" +
                   $"{nameof(ParsedTestSensorsInfo.TestId)}: {testSensorsInfo.TestId} \r\n" +
                   $"{nameof(ParsedTestSensorsInfo.Sensors)}: {testSensorsInfo.Sensors.ListOfSensorsToString()} \r\n";
        }

        private static void WriteSuccessMessage(
            DateTime leftTimeBorder,
            DateTime rightTimeBorder,
            ParsedTestSensorsInfo testSensorsInfo,
            string fileName)
        {
            Console.WriteLine(
                "SUCCESS: Outputs for the specified parameters: \r\n" +
                GetSearchConditionsString(leftTimeBorder, rightTimeBorder, testSensorsInfo) +
                $"Were successfully written to the file \'{fileName}\'"
                );
        }
    }
}