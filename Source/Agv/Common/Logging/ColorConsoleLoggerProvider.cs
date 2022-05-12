using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gebhardt.StoreWare.Agv.Common.Logging
{
	/// <summary>
	/// Provides the logger class ColorConsoleLogger
	/// </summary>
	[ProviderAlias("ColorConsole")] // Look at "app settings.json" following the according section 
	public sealed class ColorConsoleLoggerProvider : ILoggerProvider
	{
		private readonly IDisposable _onChangeToken;
		private ColorConsoleLoggerConfiguration _currentConfig;
		private readonly ConcurrentDictionary<string, ColorConsoleLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
		private readonly IOptionsMonitor<ColorConsoleLoggerConfiguration> _optionsMonitorConfig;

		/// <summary>
		/// Setting up a logging provider instance of ColorConsoleLogger
		/// </summary>
		/// <param name="config">Incorporates the provider's initial configuration</param>
		/// <param name="iConfiguration">Incorporates the appsettings configuration</param>
		public ColorConsoleLoggerProvider(IOptionsMonitor<ColorConsoleLoggerConfiguration> config, IConfiguration iConfiguration)
		{
			_optionsMonitorConfig = config;
			_currentConfig = config.CurrentValue;
			_onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);

			int settingsPaddingRight = iConfiguration.GetValue<int>(LoggingConstants.SettingKeyPaddingRight);
			if (settingsPaddingRight > 0)
			{
				_currentConfig.PadRight = settingsPaddingRight;
			}

			int eventIdPaddingRight = iConfiguration.GetValue<int>(LoggingConstants.SettingKeyEventIdPaddingRight);
			if (eventIdPaddingRight > 0)
			{
				_currentConfig.EventIdPaddingRight = eventIdPaddingRight;
			}
		}

		/// <summary>
		/// Setting an ILogger instance of its logging provider
		/// </summary>
		/// <param name="categoryName"></param>
		/// <returns></returns>
		public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, name => new ColorConsoleLogger(name, GetCurrentConfig, _optionsMonitorConfig));


		private ColorConsoleLoggerConfiguration GetCurrentConfig() => _currentConfig;

		/// <summary>
		/// Freeing up resources after using Log
		/// </summary>
		public void Dispose()
		{
			_loggers.Clear();
			_onChangeToken.Dispose();
		}
    }
}
