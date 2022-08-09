using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace Gebhardt.StoreWare.Agv.Common.Logging
{
    /// <summary>
    /// Extends the logging provider TextLogger
    /// </summary>
    /// <remarks>
    /// This extension enables the access to this provider when building the host service
    /// </remarks>
    public static class TextLoggerExtensions
    {
        /// <summary>
        /// Adds the TextLogger provider to hosted service
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        [UsedImplicitly]
        public static ILoggingBuilder AddTextLogger(
            this ILoggingBuilder builder)
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, TextLoggerProvider>());

            LoggerProviderOptions.RegisterProviderOptions<TextLoggerConfiguration, TextLoggerProvider>(builder.Services);

            return builder;
        }
    }
}
