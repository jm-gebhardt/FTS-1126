using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.Common.Logging
{
	/// <summary>
	/// Basic configuration for ColorConsoleLogger
	/// </summary>
	public class ColorConsoleLoggerConfiguration
	{
		/// <summary>
		/// Basic integer ID for every log message
		/// </summary>
		[UsedImplicitly]
		public int EventId { get; }

		/// <summary>
		/// Dictionary linking LogLevels to ConsoleColors
		/// </summary>
		/// <remarks>
		///	This is the hard coding manner of the linking. An alternative is given by appsettings.json
		/// </remarks>
		public Dictionary<LogLevel, ConsoleColor> LogLevels { get; set; } = new()
		{	
			[LogLevel.Error] = ConsoleColor.Red,
			[LogLevel.Critical] = ConsoleColor.DarkMagenta,
			[LogLevel.Warning] = ConsoleColor.DarkYellow,
			[LogLevel.Information] = ConsoleColor.Green,
			[LogLevel.Debug] = ConsoleColor.Blue,
			[LogLevel.Trace] = ConsoleColor.Magenta,
			[LogLevel.None] = ConsoleColor.Gray
		};

		/// <summary>
		/// Padding parameter for the log message's string
		/// </summary>
		public int PadRight = 40;

		/// <summary>
		/// Padding parameter for the log message's eventId
		/// </summary>
		public int EventIdPaddingRight = 5;
	}
}
