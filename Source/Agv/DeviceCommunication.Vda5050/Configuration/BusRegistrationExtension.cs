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
            //configurator.AddMqttConsumer<MqttVisualizationConsumer>();
            return configurator;
        }

        /// <summary>
        /// Add consumers.
        /// </summary>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public IBusRegistrationConfigurator AddConsumers()
        {
          //  configurator.AddSagaStateMachine<AgvGraphBuilderStateMachine, AgvGraphBuilderSaga>().InMemoryRepository();
          //  configurator.AddConsumer<InstantActionsConsumer>();
            return configurator;
        }
    }
}