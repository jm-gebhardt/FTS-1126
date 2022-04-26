using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static System.String;

namespace Gebhardt.StoreWare.Agv.Common
{
    /// <summary>
    /// Encapsulates startup procedure for <see cref="GebhardtBackgroundService"/>.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public static class GebhardtStartup
    {
        /// <summary>
        /// Create default host and create singleton worker implementation.
        /// </summary>
        /// <typeparam name="T">Service implementation type (need to be derived from <see cref="IWorker"/>).</typeparam>
        /// <param name="args">Command line arguments.</param>
        [JetBrains.Annotations.PublicAPI]
        public static IHost BuildHost<T>(string[] args) where T : IWorker
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
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton(typeof(IWorker), typeof(T));
                    services.AddHostedService<GebhardtBackgroundService>();
                    services.Configure<Microsoft.Extensions.Logging.EventLog.EventLogSettings>(settings =>
                    {
                        settings.SourceName = serviceName;
                    });

                    // Configure MassTransit with RabbitMQ if the worker requires it
                    if (typeof(IMassTransitWorker).IsAssignableFrom(typeof(T)))
                    {
                        ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration);
                    }
                })
                .Build();
            return host;
        }
    }
}
