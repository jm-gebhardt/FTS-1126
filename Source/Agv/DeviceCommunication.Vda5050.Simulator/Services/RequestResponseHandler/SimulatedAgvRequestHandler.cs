using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Common.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.RequestResponseHandler;

/// <summary>
/// Handles requests for simulated agv management
/// </summary>
[UsedImplicitly]
public class SimulatedAgvRequestHandler(ILogger<SimulatedAgvRequestHandler> logger, IAgvCache agvCache, IBus bus, IAgvStateCache stateCache) :
    IConsumer<RequestOperatingModeSwitch>,
    IConsumer<RequestSetToPosition>,
    IConsumer<RequestAgvState>,
    IConsumer<RequestAddLoad>,
    IConsumer<RequestRemoveLoad>
{
    private async Task RequireOperatingModeAsync(Guid agvCorrelationId, AgvOperatingMode requiredMode)
    {
        var mode = await agvCache.GetOperatingModeAsync(agvCorrelationId);
        if (mode != requiredMode)
        {
            throw new AgvOperatingModeNotAllowedException(agvCorrelationId, mode, requiredMode);
        }
    }

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
            throw new AgvOperatingModeNotAllowedException(agvCorrelationId, mode);
        }
        logger.LogInformation("SetPosition to position to {Position} requested for agv {AgvCorrelationId}", position, agvCorrelationId);
        await bus.Publish(new SetToPosition(agvCorrelationId, position), context.CancellationToken);
        await context.RespondAsync(new Success());
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RequestAgvState> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        if (!await agvCache.ExistsAsync(agvCorrelationId))
        {
            throw new AgvNotFoundException(agvCorrelationId);
        }

        var state = await stateCache.TryGetAsync(agvCorrelationId);
        await context.RespondAsync(new AgvStateResponse(state));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RequestAddLoad> context)
    {
        (var agvCorrelationId, string loadId, string? loadType, string? loadPosition) = context.Message;
        await RequireOperatingModeAsync(agvCorrelationId, AgvOperatingMode.Manual);
        var state = await stateCache.TryGetAsync(agvCorrelationId);
        if (state?.Loads?.Any(l => l.LoadPosition == loadPosition) == true)
        {
            throw new LoadPositionOccupiedException(loadPosition);
        }
        logger.LogInformation("Add load {LoadId} requested for agv {AgvCorrelationId}", loadId, agvCorrelationId);
        await bus.Publish(new AddLoad(agvCorrelationId, loadId, loadType, loadPosition), context.CancellationToken);
        await context.RespondAsync(new Success());
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RequestRemoveLoad> context)
    {
        (var agvCorrelationId, string loadId) = context.Message;
        await RequireOperatingModeAsync(agvCorrelationId, AgvOperatingMode.Manual);
        var state = await stateCache.TryGetAsync(agvCorrelationId);
        if (state?.Loads?.Any(l => l.LoadId == loadId) != true)
        {
            throw new LoadNotFoundException(loadId);
        }
        logger.LogInformation("Remove load {LoadId} requested for agv {AgvCorrelationId}", loadId, agvCorrelationId);
        await bus.Publish(new RemoveLoad(agvCorrelationId, loadId), context.CancellationToken);
        await context.RespondAsync(new Success());
    }
}