using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.WebApi;

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
        busRegistrationConfigurator.AddConsumer<AgvActionGroupConsumer>();
        busRegistrationConfigurator.AddConsumer<ActionGroupChangedConsumer>();
        busRegistrationConfigurator.AddConsumer<ActionGroupCompletedConsumer>();
        busRegistrationConfigurator.AddConsumer<ActionStartedConsumer>();

        busRegistrationConfigurator.AddConsumer<AgvPositionChangedConsumer>();
        busRegistrationConfigurator.AddConsumer<AgvChangedConsumer>();
        busRegistrationConfigurator.AddConsumer<AgvBatteryStatusChangedConsumer>();
        busRegistrationConfigurator.AddConsumer<AgvOperatingDataStatusChangedConsumer>();

        busRegistrationConfigurator.AddConsumer<NodeChangedConsumer>();
        busRegistrationConfigurator.AddConsumer<TriggerConsumer>();
        busRegistrationConfigurator.AddConsumer<RestrictedAreaChangedConsumer>();
        busRegistrationConfigurator.AddConsumer<MapChangedConsumer>();
        busRegistrationConfigurator.AddConsumer<SystemChangedConsumer>();
        busRegistrationConfigurator.AddConsumer<QueueChangedConsumer>();
        busRegistrationConfigurator.AddConsumer<AgvTypeChangedConsumer>();
    }
}