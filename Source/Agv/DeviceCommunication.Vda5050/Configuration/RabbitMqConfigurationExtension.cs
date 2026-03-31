using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Filters;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Configuration;

/// <summary>
/// Provides extension methods to <see cref="IRabbitMqBusFactoryConfigurator"/>
/// </summary>
public static class RabbitMqConfigurationExtension
{
    extension(IRabbitMqBusFactoryConfigurator configurator)
    {
        /// <summary>
        /// Add Mqtt message topologies.
        /// </summary>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public IRabbitMqBusFactoryConfigurator AddMqttMessages()
        {
            configurator.ConfigureMessageTopology<MqttInstantActions>();
            configurator.ConfigureMessageTopology<MqttState>();
            configurator.ConfigureMessageTopology<MqttConnection>();
            configurator.ConfigureMessageTopology<MqttFactsheet>();
            configurator.ConfigureMessageTopology<MqttVisualization>();
            configurator.ConfigureMessageTopology<MqttOrder>();
            return configurator;
        }

        /// <summary>
        /// Add message filters.
        /// </summary>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public IRabbitMqBusFactoryConfigurator AddFilters(IBusRegistrationContext registrationContext)
        {
            configurator.UseConsumeFilter<MqttMessageConsumeFilter>(registrationContext);
            return configurator;
        }
    }
}