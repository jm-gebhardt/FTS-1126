using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.Route;
using Gebhardt.Acc.DeviceCommunication.Common.Services;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Agv;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.EventHandler;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.MqttMessageHandler;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.RequestHandler;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Configuration;

/// <summary>
/// Provides extensions methods to <see cref="IBusRegistrationConfigurator"/>.
/// </summary>
public static class BusRegistrationExtension
{
    private const int PartitionCount = 16;

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
            configurator.AddMqttConsumer<MqttFactsheetConsumer>();
            return configurator;
        }

        /// <summary>
        /// Add consumers.
        /// </summary>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public IBusRegistrationConfigurator AddConsumers()
        {
            configurator.AddConsumer<OrderEventHandler>(c =>
            {
                var partitioner = c.CreatePartitioner(PartitionCount);
                partitioner.PartitionMessageById<OrderStateReceived, OrderEventHandler>(c);
            });
            configurator.AddConsumer<InstantActionsCommandHandler>();
            configurator.AddConsumer<ResendCommandHandler>();
            configurator.AddConsumer<AgvCommandHandler>(c =>
            {
                var partitioner = c.CreatePartitioner(PartitionCount);
                partitioner.PartitionMessageById<AddAgv, AgvCommandHandler>(c);
                partitioner.PartitionMessageById<UpdateAgvType, AgvCommandHandler>(c);
            });
            configurator.AddConsumer<RouteEventHandler>(c =>
            {
                var partitioner = c.CreatePartitioner(PartitionCount);
                c.Message<RoutePlanned>(x => x.UsePartitioner(partitioner, p => p.Message.RouteId));
                c.Message<RouteUpdated>(x => x.UsePartitioner(partitioner, p => p.Message.RouteId));
                c.Message<RouteStepsReleased>(x => x.UsePartitioner(partitioner, p => p.Message.RouteId));
                c.Message<RouteAborted>(x => x.UsePartitioner(partitioner, p => p.Message.RouteId));
            });
            configurator.AddConsumer<RouteStepEventHandler>(c =>
            {
                var partitioner = c.CreatePartitioner(PartitionCount);
                partitioner.PartitionMessageById<OrderStateReceived, RouteStepEventHandler>(c);
            });

            configurator.AddConsumer<LifCommandHandler>();
            configurator.AddConsumer<LifRequestHandler>();
            configurator.AddConsumer<FactsheetRequestHandler>();
            configurator.AddConsumer<FactsheetQueryHandler>();
            configurator.AddConsumer<AgvEventHandler>();
            configurator.AddConsumer<GraphMerger>();

            return configurator;
        }
    }
}