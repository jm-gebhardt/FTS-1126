using AutoMapper;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Hubs;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="SimulatedAgvStateChanged"/> and publishes it to all clients.
/// </summary>
[UsedImplicitly]
public class SimulatedAgvStateChangedConsumer(IMapper mapper, IMessageFilter<Vda5050StateDto> filter, ISimulatedAgvStatePublisher publisher) : IConsumer<SimulatedAgvStateChanged>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<SimulatedAgvStateChanged> context)
    {
        var dto = mapper.Map<Vda5050StateDto>(context.Message.State) with { CorrelationId = context.Message.CorrelationId };
        if (await filter.ShouldActAsync(dto))
        {
            await publisher.SendStateChangedAsync(dto);
        }
    }
}