using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using ValidationException = FluentValidation.ValidationException;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.RequestResponseHandler;

/// <summary>
/// Handles requests for simulated agv management
/// </summary>
[UsedImplicitly]
public class SimulatedAgvRequestHandler(ILogger<SimulatedAgvRequestHandler> logger, IAgvCache agvCache, IBus bus):
    IConsumer<RequestOperatingModeSwitch>,
    IConsumer<RequestSetToPosition>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RequestOperatingModeSwitch> context)
    {
        var (agvCorrelationId, operatingMode) = context.Message;

        if (!await agvCache.ExistsAsync(agvCorrelationId))
        {
            throw new AgvNotFoundException(agvCorrelationId);
        }

        logger.LogInformation("Operation mode switch to {OperationMode} requested for agv {AgvCorrelationId}", operatingMode, agvCorrelationId);
        await bus.Publish(new SwitchOperatingMode(agvCorrelationId, operatingMode), context.CancellationToken);
        await context.RespondAsync(new Success());
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RequestSetToPosition> context)
    {
        var (agvCorrelationId, position) = context.Message;
        var mode = await agvCache.GetOperatingModeAsync(agvCorrelationId);
        if (mode == AgvOperatingMode.Automatic)
        {
            throw new ValidationException("Moving agv not allowed in automatic mode");
        }
        logger.LogInformation("SetPosition to position to {Position} requested for agv {AgvCorrelationId}", position, agvCorrelationId);
        await bus.Publish(new SetToPosition(agvCorrelationId, position), context.CancellationToken);
        await context.RespondAsync(new Success());
    }
}