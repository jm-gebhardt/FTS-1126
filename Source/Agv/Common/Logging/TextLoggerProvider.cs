using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Configurations.SettingsProviders;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Common.Logging;

/// <summary>
/// Provides the logger class TextLogger
/// </summary>
public sealed class TextLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, TextLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ISettingsProvider<TextLoggerConfiguration> _textLoggerSettingsProvider;

    /// <summary>
    /// Setting up a logging provider instance of TextLogger
    /// </summary>
    /// <param name="textLoggerSettingsProvider"></param>
    public TextLoggerProvider(ISettingsProvider<TextLoggerConfiguration> textLoggerSettingsProvider)
    {
        _textLoggerSettingsProvider = textLoggerSettingsProvider;
    }

    /// <summary>
    /// Setting an ILogger instance of its logging provider
    /// </summary>
    /// <param name="categoryName"></param>
    /// <returns></returns>
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new TextLogger(name, _textLoggerSettingsProvider));
    }

    /// <summary>
    /// Freeing up resources after using Log
    /// </summary>
    public void Dispose()
    {
        _loggers.Clear();
    }
}