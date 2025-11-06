using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Messages.Simulation;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Speed;

/// <summary>
/// Consumer responsible for handling simulation speed adjustment commands for simulated AGVs.
/// </summary>
[UsedImplicitly]
public class SetSimulationSpeedConsumer :
    IConsumer<SimulatedAgvStateMachine.CreateAgv>,
    IConsumer<SetSimulationSpeed>
{
    private readonly IPublishEndpoint _publish;
    private static readonly ConcurrentBag<Guid> _agvCorrelationIds = [];

    /// <inheritdoc cref="SetSimulationSpeedConsumer"/>
    public SetSimulationSpeedConsumer(IPublishEndpoint publish)
    {
        _publish = publish;
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<SimulatedAgvStateMachine.CreateAgv> context)
    {
        _agvCorrelationIds.Add(context.Message.CorrelationId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<SetSimulationSpeed> context)
    {
        foreach (var agvCorrelationId in _agvCorrelationIds)
        {
            await _publish.Publish(new SetSimulationSpeedForAgv(agvCorrelationId, context.Message.SimulationSpeed));
        }
    }
}