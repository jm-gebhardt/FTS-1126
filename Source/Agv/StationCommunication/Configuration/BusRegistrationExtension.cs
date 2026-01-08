using Gebhardt.Acc.StationCommunication.Messages;
using Gebhardt.Acc.StationCommunication.Messages.JamState;
using Gebhardt.Acc.StationCommunication.Services.CommandHandler;
using Gebhardt.Acc.StationCommunication.Services.EventHandler;
using MassTransit;

namespace Gebhardt.Acc.StationCommunication.Configuration;

/// <summary>
/// Provides extension methods for the bus registration.
/// </summary>
public static class BusRegistrationExtension
{
    private const int PartitionCount = 16;

    /// <summary>
    /// Provides extension methods for the bus registration.
    /// </summary>
    extension(IBusRegistrationConfigurator cfg)
    {
        /// <summary>
        /// Adds bus consumers
        /// </summary>
        public void AddBusConsumers()
        {
            cfg.AddConsumer<JamStateEventHandler>();
            cfg.AddConsumer<JamStateCommandHandler>(c =>
            {
                var partitioner = c.CreatePartitioner(PartitionCount);
                c.Message<UpdateJamState>(x => x.UsePartitioner(partitioner, p => p.Message.CorrelationId));
                c.Message<RequestJamState>(x => x.UsePartitioner(partitioner, p => p.Message.CorrelationId));
            });
            cfg.AddConsumer<OrderEventHandler>();
            cfg.AddConsumer<OrderCommandHandler>(c =>
            {
                var partitioner = c.CreatePartitioner(PartitionCount);
                c.Message<ReportOrder>(x => x.UsePartitioner(partitioner, p => p.Message.CorrelationId));
                c.Message<ReportCargo>(x => x.UsePartitioner(partitioner, p => p.Message.CorrelationId));
                c.Message<ReportPalletDrop>(x => x.UsePartitioner(partitioner, p => p.Message.CorrelationId));
            });
        }
    }
}