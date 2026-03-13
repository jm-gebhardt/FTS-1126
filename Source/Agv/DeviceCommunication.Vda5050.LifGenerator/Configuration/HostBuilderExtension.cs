using Gebhardt.Acc.Common.Configurations.Extensions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.LifGenerator.Configuration;

/// <summary>
/// Provides extensions methods to <see cref="IHostBuilder"/>.
/// </summary>
public static class HostBuilderExtension
{
    /// <summary>
    /// Extension block for <see cref="IHostBuilder"/>.
    /// </summary>
    extension(IHostBuilder hostBuilder)
    {
        /// <summary>
        /// Add service configuration.
        /// </summary>
        public IHostBuilder AddService(ILogger initialLogger)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                try
                {
                    services.ConfigureServicesMassTransit(context.Configuration,
                        busRegistrationConfiguration: configurator =>
                        {
                            configurator.AddConsumer<GraphEventHandler>();
                        });
                    services.AddOptions<MassTransitHostOptions>().Configure(options => { options.WaitUntilStarted = true; });
                }
                catch (InvalidOperationException e)
                {
                    initialLogger.LogError(e, "MassTransit could not be started");
                    throw;
                }
                services.AddAutoMapper(_ => { }, AppDomain.CurrentDomain.GetAssemblies());
            });

            return hostBuilder;
        }
    }
}