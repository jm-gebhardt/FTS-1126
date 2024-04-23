using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.TransportOrder.Core.Configurations;
using Gebhardt.Acc.TransportOrder.Messages.Filters;
using Gebhardt.Acc.TransportOrder.WebApi;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

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
    /// <param name="quartzSchedulerName">Quartz scheduler name</param>
    /// <param name="quartzSchedulerId">Quartz scheduler id</param>
    /// <param name="quartzQueueName">Quartz queue name</param>
    /// <returns><see cref="IHostBuilder"/></returns>
    public static IHostBuilder AddMassTransit(this IHostBuilder hostBuilder, ILogger initialLogger, string quartzSchedulerName, string quartzSchedulerId, string quartzQueueName)
    {
        hostBuilder.ConfigureServices((context, services) =>
        {
            try
            {
                services.ConfigureServicesMassTransit(context.Configuration, x =>
                {
                    x.AddCoreConsumer()
                        .AddWebApiConsumer();

                    x.AddMediator(c => c.AddMediator());
                    x.AddPublishMessageScheduler();
                    x.AddQuartzConsumers(options => options.QueueName = quartzQueueName);
                }, rabbitMqConfiguration: (ctx, cfg) =>
                {
                    cfg.UsePublishMessageScheduler();
                    cfg.UseConsumeFilter<ActionGroupTracingConsumeFilters>(ctx);
                    cfg.UseConsumeFilter<AgvTracingConsumeFilters>(ctx);
                });

                services.AddQuartz(q =>
                {
                    q.SchedulerName = quartzSchedulerName;
                    q.SchedulerId = quartzSchedulerId;

                    // convert time zones using converter that can handle Windows/Linux differences
                    q.UseTimeZoneConverter();
                });
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