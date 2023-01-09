using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gebhardt.StoreWare.Agv.Common.Logging
{
    /// <summary>
    /// Implements a text file based logging class from ILogger
    /// </summary>
    public sealed class TextLogger : ILogger
    {
        private readonly string _name;
        private readonly Func<TextLoggerConfiguration> _getCurrentConfig;

        private readonly int _padRight;
        private readonly int _eventIdPaddingRight;
        private static string? _folderPath;


        /// <summary>
        /// Constructor of TextLogger class
        /// </summary>
        /// <param name="name"></param>
        /// <param name="getCurrentConfig"></param>
        /// <param name="iOptionsMonitorConfig"></param>
        public TextLogger(string name, Func<TextLoggerConfiguration> getCurrentConfig, IOptionsMonitor<TextLoggerConfiguration> iOptionsMonitorConfig)
        {
            _padRight = iOptionsMonitorConfig.CurrentValue.PadRight;
            _eventIdPaddingRight = iOptionsMonitorConfig.CurrentValue.EventIdPaddingRight;
            _folderPath = iOptionsMonitorConfig.CurrentValue.LogFolderPath;

            _name = name;
            _getCurrentConfig = getCurrentConfig;
        }

        /// <summary>
        /// Scarce resources allocated when using Log
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="state"></param>
        /// <returns></returns>
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;

        /// <summary>
        /// Checks if Log level exists in dictionary keys
        /// </summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        public bool IsEnabled(LogLevel logLevel) => _getCurrentConfig().LogLevels.ContainsKey(logLevel);

        /// <summary>
        /// Implements the log creation
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="logLevel"></param>
        /// <param name="eventId"></param>
        /// <param name="state"></param>
        /// <param name="exception"></param>
        /// <param name="formatter"></param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var config = _getCurrentConfig();
            if (config.EventId == 0 || config.EventId == eventId.Id)
            {
                var timeNow = DateTime.Now;

                var logMessage = new StringBuilder();
                logMessage.Append($"{logLevel}|EventId:{eventId.Id}".PadRight(_eventIdPaddingRight));
                logMessage.Append($"|{timeNow.ToString(LoggingConstants.LoggingDateFormat)}|");
                logMessage.Append($"{_name.PadRight(_padRight)}|");
                logMessage.Append(formatter(state, exception));

                Write(logLevel, logMessage.ToString());
            }
        }


        private static void Write(LogLevel logLevel, string msg)
        {
            string? folderPath = _folderPath;
            string filePath;

            if (logLevel == LogLevel.Error)
            {
                filePath = DateTime.Now.ToString("dd") + LoggingConstants.ErrorFileSuffix;
            }
            else
            {
                filePath = DateTime.Now.ToString("dd") + LoggingConstants.NonErrorFileSuffix;
            }


            if (!File.Exists(folderPath + filePath))
            {
                using var fs = new FileStream(folderPath + filePath, FileMode.CreateNew);
                using var w = new BinaryWriter(fs);
                w.Write(msg + Environment.NewLine);
            }
            else
            {
                using var streamWriter = File.AppendText(folderPath + filePath);
                streamWriter.WriteLine(msg);
            }
        }
    }
}
