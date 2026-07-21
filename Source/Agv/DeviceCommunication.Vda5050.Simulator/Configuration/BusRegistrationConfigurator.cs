using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.MqttMessageHandlers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Consumer;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Configuration;

/// <summary>
/// Provides extensions methods to <see cref="IBusRegistrationConfigurator"/>.
/// </summary>
public static class BusRegistrationConfigurator
{
    extension(IBusRegistrationConfigurator configurator)
    {
        /// <summary>
        /// Add Mqtt consumers.
        /// </summary>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public IBusRegistrationConfigurator AddMqttConsumers()
        {
            configurator.AddMqttConsumer<MqttInstantActionsConsumer>();
            configurator.AddMqttConsumer<MqttOrderConsumer>();
            return configurator;
        }

        /// <summary>
        /// Add consumers.
        /// </summary>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public IBusRegistrationConfigurator AddConsumers()
        {
            configurator.AddConsumer<SimulatedAgvStateChangedConsumer>();
            return configurator;
        }
    }

    extension(IRabbitMqBusFactoryConfigurator configurator)
    {
        /// <summary>
        /// Add Mqtt message topologies.
        /// </summary>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public IRabbitMqBusFactoryConfigurator AddMqttMessages()
        {
            configurator.ConfigureMessageTopology<MqttState>();
            configurator.ConfigureMessageTopology<MqttConnection>();
            configurator.ConfigureMessageTopology<MqttVisualization>();
            configurator.ConfigureMessageTopology<MqttInstantActions>();
            configurator.ConfigureMessageTopology<MqttOrder>();
            return configurator;
        }
    }
}