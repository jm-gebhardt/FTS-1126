using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace Gebhardt.StoreWare.Agv.Common.Logging
{
	/// <summary>
	/// Extends the logging provider ColorConsoleLogger
	/// </summary>
	/// <remarks>
	///	This extension enables the access to this provider when building the host service
	/// </remarks>
	[UsedImplicitly]
	public static class ColorConsoleLoggerExtensions
	{
		/// <summary>
		/// Adds the ColorConsoleLogger provider to hosted service
		/// </summary>
		/// <param name="builder"></param>
		/// <returns></returns>
		[UsedImplicitly]
		public static ILoggingBuilder AddColorConsoleLogger(
			this ILoggingBuilder builder)
		{
			builder.AddConfiguration();

			builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ColorConsoleLoggerProvider>());

			LoggerProviderOptions.RegisterProviderOptions<ColorConsoleLoggerConfiguration, ColorConsoleLoggerProvider>(builder.Services);

			return builder;
		}
    }
}
