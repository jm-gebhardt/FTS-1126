using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi;

/// <summary>
/// IBusRegistrationExtensions extensions
/// </summary>
public static class WebApiBusRegistrationExtensions
{
    /// <summary>
    /// Add WebApi consumer.
    /// </summary>
    /// <param name="busRegistrationConfigurator"></param>
    /// <returns></returns>
    public static void AddWebApiConsumer(this IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        busRegistrationConfigurator.AddConsumer<ActionGroupCreatedConsumer>();
        busRegistrationConfigurator.AddConsumer<ActionGroupStartedConsumer>();
        busRegistrationConfigurator.AddConsumer<ActionGroupChangedConsumer>();
        busRegistrationConfigurator.AddConsumer<ActionGroupCompletedConsumer>();
        busRegistrationConfigurator.AddConsumer<ActionStartedConsumer>();

        busRegistrationConfigurator.AddConsumer<AgvPositionChangedConsumer>();
        busRegistrationConfigurator.AddConsumer<AgvChangedConsumer>();
        busRegistrationConfigurator.AddConsumer<AgvBatteryChargeChangedConsumer>();

        busRegistrationConfigurator.AddConsumer<NodeStateChangedConsumer>();
    }
}