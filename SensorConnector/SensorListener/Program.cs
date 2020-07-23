using SensorConnector.Common;
using SensorConnector.Common.Entities;
using SensorListener.CommandLineArgsParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vibrant.InfluxDB.Client;
using Timer = System.Timers.Timer;

namespace SensorListener
{
    public class Program
    {
        private static ParsedInputParams _parsedInputParams;

        private static InfluxClient _influxClient;

        private static string _measurementName;

        private static Timer _timer;

        static int Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        static async Task<int> MainAsync(string[] args)
        {
            try
            {
                #region Test

                /*
                // WARNING: Remove or comment out this part for production.
                var testArgs = AppSettings.SensorListener.ExecutionParamsStringExample.Split(' ');
                args = testArgs;
                */
                #endregion Test

                _parsedInputParams = CommandLineArgsParser.CommandLineArgsParser.ParseInputParams(args);
            }
            catch (Exception ex)
            {
                var errorMessage = "ERROR: execution params parsing failed.\r\n" + ex.Message;
                Console.WriteLine(errorMessage);

                return 1;
            }

            _measurementName = AppSettings.MeasurementNameBase + _parsedInputParams.TestId;

            _influxClient = new InfluxClient(new Uri(AppSettings.InfluxHost));

            // var databases = await _client.ShowDatabasesAsync();

            await _influxClient.CreateDatabaseAsync(AppSettings.InfluxDatabaseName); // Creates Influx database if not exist

            InitTimer(_parsedInputParams.ProgramExecutionTime);

            try
            {
                Console.WriteLine("Listening to port: {0}...", AppSettings.SensorListener.ListenPort);

                Thread receiveThread = new Thread(new ThreadStart(ReceiveMessage));
                receiveThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return 2;
            }

            return 0;
        }

        private static void ReceiveMessage()
        {
            UdpClient receiver = new UdpClient(AppSettings.SensorListener.ListenPort); // UdpClient for receiving incoming data

            IPEndPoint remoteIp = null; // Address of the sending server (NULL means Any)

            try
            {
                // Start the timer
                _timer.Enabled = true;

                while (true)
                {
                    byte[] data = receiver.Receive(ref remoteIp); // Receive data from the server

                    var senderIpAddress = remoteIp.Address.ToString();
                    var senderPort = remoteIp.Port;

                    Console.WriteLine($"Received broadcast from {remoteIp}");

                    if (IsTargetSensor(senderIpAddress, senderPort))
                    {
                        var dataAsStr = Encoding.ASCII.GetString(data, 0, data.Length);

                        WriteReceivedBatchToInfluxDbAsync(senderIpAddress, senderPort, dataAsStr)
                             .ContinueWith(t =>
                             {
                                 Console.WriteLine($"Wrote broadcast from {remoteIp} to {AppSettings.InfluxDatabaseName}.{_measurementName}");

                             });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static bool IsTargetSensor(string ip, int port)
        {
            return _parsedInputParams.Sensors.FirstOrDefault(
                x => x.IpAddress.Equals(ip) && x.Port == port) != null;
        }

        private static async Task WriteReceivedBatchToInfluxDbAsync(string sensorIpAddress, int sensorPort, string data)
        {
            // TODO: Implement buffered writing
            var sensorOutput = new SensorOutput
            {
                Timestamp = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                SensorIpAddress = sensorIpAddress,
                SensorPort = sensorPort,
                Data = data
            };

            var outputs = new List<SensorOutput>() { sensorOutput };

            // await Task.Delay(2000);

            await _influxClient.WriteAsync(AppSettings.InfluxDatabaseName, _measurementName, outputs);
        }

        /// <summary>
        /// Initializes timer to countdown program execution time.
        /// </summary>
        /// <param name="programExecutionTime">Amount of time in seconds for which program will listen to sensors.</param>
        private static void InitTimer(int programExecutionTime)
        {
            // Create a timer its countdown time in milliseconds.
            _timer = new Timer
            {
                Interval = programExecutionTime * 1000
            };

            // Hook up the Elapsed event for the timer. 
            _timer.Elapsed += OnTimedEvent;
        }

        private static void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine($"Execution time ended.");

            Environment.Exit(0);
        }
    }
}