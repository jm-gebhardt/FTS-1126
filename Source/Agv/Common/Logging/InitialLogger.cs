using Microsoft.Extensions.Logging;


namespace Gebhardt.StoreWare.Agv.Common.Logging
{
    /// <summary>
    /// Factory to provide reliable logger instance used for inital logging
    /// instead of sending output directly to console.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public sealed class InitialLogger
    {
        /// <summary>
        /// Create console logger instance.
        /// </summary>
        /// <typeparam name="TCategoryName">Name of this type is used for logger category.</typeparam>
        /// <returns>ILogger instance.</returns>
        [JetBrains.Annotations.PublicAPI]
        public static ILogger Create<TCategoryName>()
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            return loggerFactory.CreateLogger<TCategoryName>();
        }
    }
}
