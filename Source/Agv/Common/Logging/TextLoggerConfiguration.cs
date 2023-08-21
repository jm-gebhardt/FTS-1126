using Gebhardt.StoreWare.Agv.Common.Configurations.Attributes;
using Gebhardt.StoreWare.Agv.Common.Configurations.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.Common.Logging;

/// <summary>
/// Basic configuration for TextLogger
/// </summary>
[AutoSeedToDatabase(false)]
public record TextLoggerConfiguration : SettingsBase<TextLoggerConfiguration>
{
    /// <summary>
    /// Basic integer ID for every log message
    /// </summary>
    [UsedImplicitly]
    public int EventId { get; }

    /// <summary>
    /// Dictionary linking LogLevels to integers
    /// </summary>
    /// <remarks>
    /// The LogLevel to integer linking is inspired by common Gebhardt logging practice
    /// </remarks>
    public Dictionary<LogLevel, int> LogLevels { get; } = new()
    {
        [LogLevel.Error] = 0,
        [LogLevel.Critical] = 1,
        [LogLevel.Warning] = 2,
        [LogLevel.Information] = 3,
        [LogLevel.Debug] = 5,
        [LogLevel.Trace] = 7,
        [LogLevel.None] = 9
    };

    /// <summary>
    /// Padding parameter for the log message's string
    /// </summary>
    public int PadRight { get; set; } = 60;

    /// <summary>
    /// Padding parameter for the log message's eventId
    /// </summary>
    public int EventIdPaddingRight { get; set; } = 27;

    /// <summary>
    /// Default path of the text log folder
    /// </summary>
    public string LogFolderPath { get; set; } = @".\LOG\";
}