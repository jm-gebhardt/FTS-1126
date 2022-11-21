using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gebhardt.StoreWare.Agv.Common.Logging
{
    /// <summary>
    /// Provides the logger class TextLogger
    /// </summary>
    public sealed class TextLoggerProvider : ILoggerProvider
    {
        private readonly IDisposable? _onChangeToken;
        private TextLoggerConfiguration _currentConfig;
        private readonly ConcurrentDictionary<string, TextLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
        private readonly IOptionsMonitor<TextLoggerConfiguration> _optionsMonitorConfig;
        

        /// <summary>
        /// Setting up a logging provider instance of TextLogger
        /// </summary>
        /// <param name="config"></param>
        /// <param name="iConfiguration"></param>
        public TextLoggerProvider(IOptionsMonitor<TextLoggerConfiguration> config, IConfiguration iConfiguration)
        {
            _optionsMonitorConfig = config;
            _currentConfig = config.CurrentValue;
            _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);

            string? settingsFolderPath = iConfiguration.GetValue<string>(LoggingConstants.SettingKeyFolderPath);
            if (settingsFolderPath != null)
            {
                config.CurrentValue.LogFolderPath = settingsFolderPath;
            }

            if (!Directory.Exists(config.CurrentValue.LogFolderPath))
            {
                Directory.CreateDirectory(config.CurrentValue.LogFolderPath);
            }


            int settingsPaddingRight = iConfiguration.GetValue<int>(LoggingConstants.SettingKeyPaddingRight);
            if (settingsPaddingRight > 0)
            {
                config.CurrentValue.PadRight = settingsPaddingRight;
            }

            int eventIdPaddingRight = iConfiguration.GetValue<int>(LoggingConstants.SettingKeyEventIdPaddingRight);
            if (eventIdPaddingRight > 0)
            {
                config.CurrentValue.EventIdPaddingRight = eventIdPaddingRight;
            }
        }

        /// <summary>
        /// Setting an ILogger instance of its logging provider
        /// </summary>
        /// <param name="categoryName"></param>
        /// <returns></returns>
        public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, name => new TextLogger(name, GetCurrentConfig, _optionsMonitorConfig));


        private TextLoggerConfiguration GetCurrentConfig() => _currentConfig;

        /// <summary>
        /// Freeing up resources after using Log
        /// </summary>
        public void Dispose()
        {
            _loggers.Clear();
            _onChangeToken?.Dispose();
        }
    }
}
