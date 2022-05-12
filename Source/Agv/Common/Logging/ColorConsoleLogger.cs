using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gebhardt.StoreWare.Agv.Common.Logging
{
	/// <summary>
	/// Implements a console color based logging class from ILogger
	/// </summary>
	public sealed class ColorConsoleLogger : ILogger
	{
		private readonly string _name;
		private readonly Func<ColorConsoleLoggerConfiguration> _getCurrentConfig;
		
		private readonly int _padRight;
		private readonly int _eventIdPaddingRight;


		/// <summary>
		/// Constructor of ColorConsoleLogger class
		/// </summary>
		/// <param name="name"></param>
		/// <param name="getCurrentConfig"></param>
		/// <param name="iOptionsMonitorConfig"></param>
		public ColorConsoleLogger(string name, Func<ColorConsoleLoggerConfiguration> getCurrentConfig, IOptionsMonitor<ColorConsoleLoggerConfiguration> iOptionsMonitorConfig)
		{
			_padRight = iOptionsMonitorConfig.CurrentValue.PadRight;
			_eventIdPaddingRight = iOptionsMonitorConfig.CurrentValue.EventIdPaddingRight;

			_name = name;
			_getCurrentConfig = getCurrentConfig;
		}

		/// <summary>
		/// Scarce resources allocated when using Log
		/// </summary>
		/// <typeparam name="TState"></typeparam>
		/// <param name="state"></param>
		/// <returns></returns>
		public IDisposable BeginScope<TState>(TState state) => default!;

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
				var originalColor = Console.ForegroundColor;

				Console.ForegroundColor = config.LogLevels[logLevel];
				
				var timeNow = DateTime.Now;
				string logMsgFirstPart = $"{ eventId.Id.ToString().PadRight(_eventIdPaddingRight) }|{ timeNow.ToString(LoggingConstants.LoggingDateFormat)} | { _name} | {logLevel} |";
				Console.WriteLine($"{logMsgFirstPart.PadRight(_padRight)} {formatter(state, exception)}");
				
				Console.ForegroundColor = originalColor;
				Console.WriteLine();
			}
		}
    }
}
