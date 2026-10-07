using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.DeviceCommunication.Common.Messages.Factsheets;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.CommandHandlers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.EventHandlers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.MqttMessageHandlers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.RequestResponseHandler;
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
            configurator.AddConsumer<SimulatedAgvTypeConfigurationRequestHandler>();
            configurator.AddConsumer<FactsheetEventHandler>();
            configurator.AddConsumer<FactsheetCommandHandler>();
            configurator.AddRequestClient<GetFactsheet>();
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
            configurator.ConfigureMessageTopology<MqttFactsheet>();
            return configurator;
        }
    }
}