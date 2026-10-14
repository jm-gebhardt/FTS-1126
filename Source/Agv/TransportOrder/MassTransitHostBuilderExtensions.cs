using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.TransportOrder.Core.Configurations;
using Gebhardt.Acc.TransportOrder.Messages.Filters;
using Gebhardt.Acc.TransportOrder.WebApi;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Gebhardt.Acc.TransportOrder;

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
            if (context.HostingEnvironment.IsEnvironment("SwaggerCli"))
            {
                initialLogger.LogInformation("Skipping MassTransit and tracing registration for SwaggerCli environment");
                return;
            }
            try
            {
                services.ConfigureServicesMassTransit(context.Configuration, x =>
                {
                    x.AddCoreConsumer()
                        .AddWebApiConsumer();

                    x.AddMediator(c => c.AddMediator());
                }, rabbitMqConfiguration: (ctx, cfg) =>
                {
                    cfg.UseConsumeFilter<ActionGroupTracingConsumeFilters>(ctx);
                    cfg.UseConsumeFilter<AgvTracingConsumeFilters>(ctx);
                });
                services.ConfigureTracing(context.Configuration);
            }
            catch (InvalidOperationException e)
            {
                initialLogger.LogError(e, "MassTransit could not be started");
                throw;
            }
        });

        return hostBuilder;
    }
}