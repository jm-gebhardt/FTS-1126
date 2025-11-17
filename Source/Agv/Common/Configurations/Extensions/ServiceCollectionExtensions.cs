using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Messages.Filters;
using JetBrains.Annotations;
using MassTransit;
using MassTransit.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.Common.Configurations.Extensions;

/// <summary>
/// MassTransit configurations
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// ConfigureServices
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="config">Configuration</param>
    /// <param name="busRegistrationConfiguration">xxx</param>
    /// <param name="rabbitMqConfiguration"></param>
    [PublicAPI]
    public static void ConfigureServicesMassTransit(this IServiceCollection services,
        IConfiguration config,
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
                cfg.ConfigureEndpoints(context, new DefaultEndpointNameFormatter(includeNamespace: true));
                cfg.Host(massTransitConfig.Url);
                cfg.UsePrometheusMetrics();
            });
            busRegistrationConfiguration?.Invoke(x);
        });
    }
}