using System.Reflection;
using Gebhardt.StoreWare.Agv.Common.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static System.String;

namespace Gebhardt.StoreWare.Agv.Common
{
    /// <summary>
    /// Encapsulates startup procedure.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public static class GebhardtStartup
    {
        /// <summary>
        /// Create default host and create singleton worker implementation.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        [JetBrains.Annotations.PublicAPI]
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            string? serviceName = Assembly.GetEntryAssembly()?.GetName().Name;
            if (IsNullOrEmpty(serviceName))
            {
	            throw new InvalidOperationException("Service name information missing.");
            }

            var host = Host.CreateDefaultBuilder(args)
                .UseWindowsService(options =>
                {
                    options.ServiceName = serviceName;
                })
                .ConfigureServices(services =>
                {
                    services.Configure<Microsoft.Extensions.Logging.EventLog.EventLogSettings>(settings =>
                    {
                        settings.SourceName = serviceName;
                    });
                })
                .ConfigureLogging(conf =>
                {
	                // Add our custom logging providers - more than one possible
	                conf.AddTextLogger();
                });
            return host;
        }
    }
}
