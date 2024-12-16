using System.Text;
using Gebhardt.Acc.Common.Configurations.SettingsProviders;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Common.Logging;

/// <summary>
/// Implements a text file based logging class from ILogger
/// </summary>
public sealed class TextLogger : ILogger
{
    private readonly TextLoggerConfiguration _currentConfig;
    private readonly string _name;

    private string FolderPath => _currentConfig.LogFolderPath;

    private int PadRight => _currentConfig.PadRight;

    private int EventIdPaddingRight => _currentConfig.EventIdPaddingRight;

    private int EventId => _currentConfig.EventId;

    private IDictionary<LogLevel, int> LogLevels => _currentConfig.LogLevels;

    /// <summary>
    /// Constructor of TextLogger class
    /// </summary>
    /// <param name="name"></param>
    /// <param name="textLoggerSettingsProvider"></param>
    public TextLogger(string name, ISettingsProvider<TextLoggerConfiguration> textLoggerSettingsProvider)
    {
        _name = name;
        _currentConfig = textLoggerSettingsProvider.GetSettings();
    }

    /// <summary>
    /// Scarce resources allocated when using Log
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <param name="state"></param>
    /// <returns></returns>
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return null!;
    }

    /// <summary>
    /// Checks if Log level exists in dictionary keys
    /// </summary>
    /// <param name="logLevel"></param>
    /// <returns></returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        return LogLevels.ContainsKey(logLevel);
    }

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

        if (EventId == 0 || EventId == eventId.Id)
        {
            var timeNow = DateTime.Now;

            var logMessage = new StringBuilder();
            logMessage.Append($"{logLevel}|EventId:{eventId.Id}".PadRight(EventIdPaddingRight));
            logMessage.Append($"|{timeNow.ToString(LoggingConstants.LoggingDateFormat)}|");
            logMessage.Append($"{_name.PadRight(PadRight)}|");
            logMessage.Append(formatter(state, exception));

            Write(logLevel, logMessage.ToString());
        }
    }

    private void Write(LogLevel logLevel, string msg)
    {
        string filePath;

        if (logLevel == LogLevel.Error)
        {
            filePath = DateTime.Now.ToString("dd") + LoggingConstants.ErrorFileSuffix;
        }
        else
        {
            filePath = DateTime.Now.ToString("dd") + LoggingConstants.NonErrorFileSuffix;
        }

        if (!File.Exists(FolderPath + filePath))
        {
            using var fs = new FileStream(FolderPath + filePath, FileMode.CreateNew);
            using var w = new BinaryWriter(fs);
            w.Write(msg + Environment.NewLine);
        }
        else
        {
            using var streamWriter = File.AppendText(FolderPath + filePath);
            streamWriter.WriteLine(msg);
        }
    }
}