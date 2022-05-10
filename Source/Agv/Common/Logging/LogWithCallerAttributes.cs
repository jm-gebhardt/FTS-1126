using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.Common.Logging
{
	/// <summary>
	/// Extension class for every ILogger instance
	/// </summary>
	/// <remarks>
	///	Focus on method's caller attributes
	/// </remarks>
	[JetBrains.Annotations.PublicAPI]
	public static class LogWithCallerAttributes	
	{
		/// <summary>
		/// Log message with Caller Attributes
		/// </summary>
		/// <remarks>
		///	An ILogger implementation is needed
		/// </remarks>
		[JetBrains.Annotations.PublicAPI]
		public static void Write(ILogger? logger, string message, LogLevel logLevel = LogLevel.Information, int eventId = 0, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
		{
			if (logger != null)
			{
				logger.Log(logLevel, eventId, "Method:{MemberName}|Line:{LineNumber}|{FilePath}|{Message}", memberName, lineNumber, filePath, message);
			}
			else
			{
				Trace.WriteLine($"Logging for member {memberName} is not possible.");
				Trace.WriteLine($"Logger given at {filePath} on line {lineNumber} is NULL.");
			}
		}
	}
}
