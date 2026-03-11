using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.EventHandler;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.MqttMessageHandler;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Configuration;

/// <summary>
/// Provides extensions methods to <see cref="IBusRegistrationConfigurator"/>.
/// </summary>
public static class BusRegistrationExtension
{
    extension(IBusRegistrationConfigurator configurator)
    {
        /// <summary>
        /// Add Mqtt consumers.
        /// </summary>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public IBusRegistrationConfigurator AddMqttConsumers()
        {
            configurator.AddMqttConsumer<MqttConnectionConsumer>();
            configurator.AddMqttConsumer<MqttStateConsumer>();
            configurator.AddMqttConsumer<MqttVisualizationConsumer>();
            configurator.AddMqttConsumer<MqttFactSheetConsumer>();
            return configurator;
        }

        /// <summary>
        /// Add consumers.
        /// </summary>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public IBusRegistrationConfigurator AddConsumers()
        {
            configurator.AddConsumer<ResendCommandHandler>();
            configurator.AddConsumer<ActionCommandHandler>();
            configurator.AddConsumer<ActionEventHandler>();
            return configurator;
        }
    }
}