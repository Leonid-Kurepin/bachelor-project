using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SensorOutputParser.Exporting
{
    /// <summary>
    /// Creates Json-file from sensors outputs with parsed data.
    /// </summary>
    internal class JsonExport
    {
        private readonly int _testId;
        private readonly DateTime _leftTimeBorder;
        private readonly DateTime _rightTimeBorder;

        private readonly List<SensorOutputForExport> _sensorOutputForExport;

        public JsonExport()
        {
        }

        public JsonExport(
            int testId,
            DateTime leftTimeBorder,
            DateTime rightTimeBorder,
            List<SensorOutputForExport> sensorOutputForExport)
        {
            _testId = testId;
            _leftTimeBorder = leftTimeBorder;
            _rightTimeBorder = rightTimeBorder;
            _sensorOutputForExport = sensorOutputForExport;
        }

        public ExportedSensorOutputsFile GetSensorOutputsFile()
        {
            var content = CreateFileContent(_sensorOutputForExport);

            var fileName = GetFileName(
                _testId,
                _leftTimeBorder,
                _rightTimeBorder);

            return new ExportedSensorOutputsFile
            {
                ContentType = "application/json;charset=UTF-8",
                FileName = $"{fileName}.json",
                FileContents = content
            };
        }

        private byte[] CreateFileContent(List<SensorOutputForExport> sensorOutputsForExport)
        {
            var customJson = JsonConvert.SerializeObject(
                sensorOutputsForExport,
                Formatting.Indented,
                new SensorOutputsJsonConverter(typeof(SensorOutputForExport)));

            return Encoding.UTF8.GetBytes(customJson);
        }

        private string GetFileName(
            int testId,
            DateTime leftTimeBorder,
            DateTime rightTimeBorder)
        {
            var leftTimeBorderStr = leftTimeBorder.ToUniversalTime().ToString("yyyy-MM-dd_HH-mm-ss",
                CultureInfo.InvariantCulture);
            var rightTimeBorderStr = rightTimeBorder.ToUniversalTime().ToString("yyyy-MM-dd_HH-mm-ss",
                CultureInfo.InvariantCulture);

            return $"Test-{testId}-outputs_from_{leftTimeBorderStr}_to_{rightTimeBorderStr}";
        }
    }
}