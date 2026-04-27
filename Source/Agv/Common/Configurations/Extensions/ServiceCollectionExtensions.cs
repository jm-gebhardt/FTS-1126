using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Messages.Filters;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Core.Observability;
using Gebhardt.Core.Observability.Options;
using JetBrains.Annotations;
using MassTransit;
using MassTransit.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client.Exceptions;

namespace Gebhardt.Acc.Common.Configurations.Extensions;

/// <summary>
/// MassTransit configurations
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <param name="services">Service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// ConfigureServices
        /// </summary>
        /// <param name="config">Configuration</param>
        /// <param name="busRegistrationConfiguration">xxx</param>
        /// <param name="rabbitMqConfiguration"></param>
        [PublicAPI]
        public void ConfigureServicesMassTransit(IConfiguration config,
            Action<IBusRegistrationConfigurator>? busRegistrationConfiguration = null,
            Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? rabbitMqConfiguration = null
        )
        {
            // Enable serialization/deserialization of enum values as string.
            if (!SystemTextJsonMessageSerializer.Options.Converters.IsReadOnly)
            {
                SystemTextJsonMessageSerializer.Options.Converters.Add(new JsonStringEnumConverter());
            }
            else
            {
                Console.WriteLine($"Warning: Could not add {nameof(JsonStringEnumConverter)} during MassTransit service configuration.");
            }

            string sectionName = ConfigurationTypeManager.Instance.GetSectionName<MassTransitConfiguration>();
            var massTransitConfig = config.GetRequiredSection(sectionName).Get<MassTransitConfiguration>();

            if (massTransitConfig == null)
            {
                throw new NullReferenceException("MassTransit config was not found");
            }

            services.AddSingleton(_ => massTransitConfig);

            services.AddMassTransit(x =>
            {
                x.DisableUsageTelemetry();
                x.UsingRabbitMq((context, cfg) =>
                {
                    rabbitMqConfiguration?.Invoke(context, cfg);
                    cfg.UseConsumeFilter<AgvTracingConsumeFilters>(context);
                    cfg.UseConsumeFilter<ActionGroupTracingConsumeFilters>(context);

                    cfg.UseMessageRetry(r =>
                    {
                        r.Intervals(100, 200, 500);
                        r.Handle<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>();
                        r.Handle<AlreadyClosedException>();
                        r.Handle<RabbitMqConnectionException>();
                        r.Handle<BrokerUnreachableException>();
                    });

                    // In case RMQ is down longer than retries, re-deliver
                    cfg.UseDelayedRedelivery(r =>
                        {
                            r.Intervals(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30));
                            r.Handle<AlreadyClosedException>();
                            r.Handle<RabbitMqConnectionException>();
                            r.Handle<BrokerUnreachableException>();
                        }
                    );

                    cfg.ConfigureEndpoints(context, new DefaultEndpointNameFormatter(includeNamespace: true));
                    cfg.Host(massTransitConfig.Url);
                    cfg.UsePrometheusMetrics();
                });
                busRegistrationConfiguration?.Invoke(x);
            });
        }

        /// <summary>
        /// Configure tracing.
        /// </summary>
        /// <param name="config">Configuration</param>
        public void ConfigureTracing(IConfiguration config)
        {
            var observabilityOptions = config.GetObservabilityOptions();

            services.AddObservability<TracingService>(
                observabilityOptions,
                opts => new TracingService(opts.ServiceName, opts.ServiceName)
            );
        }
    }
}