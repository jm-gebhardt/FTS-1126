using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Statistics.Services.EventHandler;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Statistics.Configuration;

/// <summary>
/// Provides extension methods for the bus registration.
/// </summary>
public static class BusRegistrationExtension
{
    private const int PartitionCount = 16;

    /// <summary>
    /// Add bus consumers.
    /// </summary>
    [PublicAPI]
    public static IBusRegistrationConfigurator AddBusConsumers(this IBusRegistrationConfigurator cfg)
    {
        cfg.AddConsumer<GridEventHandler>().Endpoint(c => c.InstanceId = nameof(GridEventHandler));
        cfg.AddConsumer<PositionEventHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            c.Message<AgvPositionReceived>(x => x.UsePartitioner(partitioner, p => p.Message.SerialNumber));
        });
        return cfg;
    }
}