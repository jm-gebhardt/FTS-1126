using Gebhardt.Acc.TransportOrder.TrafficManagement.EventHandler;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Configurations;

/// <summary>
/// IBusRegistrationExtensions extensions
/// </summary>
public static class TrafficManagementBusRegistrationExtensions
{
    /// <summary>
    /// Adds all the consumers for this project to the bus.
    /// </summary>
    /// <param name="busRegistrationConfigurator">The configurator</param>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IBusRegistrationConfigurator AddTrafficManagementConsumer(this IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        // Configures sequential processing of incoming messages.
        busRegistrationConfigurator.AddConsumer<TrafficManagementEventHandler>(consumer =>
                {
                    consumer.ConcurrentMessageLimit = 1;
                })
            .Endpoint(endpoint => endpoint.PrefetchCount = 10);

        return busRegistrationConfigurator;
    }
}