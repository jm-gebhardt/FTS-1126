using System.Reflection;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using static System.String;
using Prometheus;

namespace Gebhardt.StoreWare.Agv.Common
{
    /// <summary>
    /// Encapsulates startup procedure.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public static class GebhardtStartup
    {
        /// <summary>
        /// Check if we are running in Docker Container
        /// </summary>
        public static bool InDocker => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

        /// <summary>
        /// Create default host and create singleton worker implementation.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        [JetBrains.Annotations.PublicAPI]
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            string? serviceName = entryAssembly?.GetName().Name;
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
                    services.ConfigureEventLogSettings(entryAssembly);
                })
                .ConfigureWebHost(host =>
                {
                    host.UseKestrel()
                        .Configure(app =>
                        {
                            app.UseMetricServer();
                        });
                })
                .ConfigureLogging(conf =>
                {
                    // Use json output when running in Docker
                    if (InDocker)
                    {
                        conf.AddJsonConsole();
                    }
                    // Disable info log for WebServer
                    conf.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
                });
            return host;
        }
    }
}
