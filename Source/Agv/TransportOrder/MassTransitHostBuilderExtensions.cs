using Gebhardt.StoreWare.Agv.Common.Configurations.Extensions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Gebhardt.StoreWare.Agv.TransportOrder
{
    /// <summary>
    /// IHostBuilder extensions
    /// </summary>
    public static class MassTransitHostBuilderExtensions
    {
        /// <summary>
        /// Configured the HostBuilder to use MassTransit <paramref name="hostBuilder" />
        /// </summary>
        /// <param name="hostBuilder"><see cref="IHostBuilder"/></param>
        /// <param name="initialLogger"><see cref="ILogger"/></param>
        /// <returns><see cref="IHostBuilder"/></returns>
        public static IHostBuilder AddMassTransit(this IHostBuilder hostBuilder, ILogger initialLogger)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                try
                {
                    services.ConfigureServicesMassTransit(context.Configuration, x =>
                    {
                        x.AddCoreConsumer();
                        x.AddWebApiConsumer();
                    }
                    , rabbitMqConfiguration: (_, cfg) => { cfg.UseDelayedMessageScheduler(); 
                    });
                }
                catch (InvalidOperationException e)
                {
                    initialLogger.LogError(e, "MassTransit could not be started");
                    // ReSharper disable PossibleIntendedRethrow
                    throw e;
                }
            });

            return hostBuilder;
        }
    }
}
