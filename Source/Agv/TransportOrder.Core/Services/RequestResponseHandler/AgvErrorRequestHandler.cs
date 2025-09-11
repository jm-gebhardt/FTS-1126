using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for agv error requests.
/// </summary>
[UsedImplicitly]
public class AgvErrorRequestHandler(ILogger<AgvErrorRequestHandler> logger, IBus bus, INodeStateRepository nodeStateRepository, IAgvDb agvDb) :
    IConsumer<SolveResourceBlockingErrors>
{
    /// <summary>
    /// Handles the <see cref="SolveResourceBlockingErrors"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<SolveResourceBlockingErrors> context)
    {
        (var agvCorrelationId, string user, bool ignoreErrorState) = context.Message;
        ICollection<NodeLock> nodeLocks = await nodeStateRepository.GetNodeLocksForAgv(agvCorrelationId);
        IReadOnlyList<AgvError> agvErrors = await agvDb.Errors.GetActiveErrorsAsync(agvCorrelationId);
        var resourceError = agvErrors.SingleOrDefault(e => e.IsResourceError());
        bool hasResourceErrors = resourceError != null;
        var queueBlockingError = agvErrors.SingleOrDefault(e => e.IsQueueBlockingError());
        bool hasQueueBlockingError = queueBlockingError != null;

        if (!ignoreErrorState)
        {
            if (hasResourceErrors)
            {
                foreach (var nodeLock in nodeLocks)
                {
                    logger.LogInformation("Solving resource blocking errors for agv {AgvCorrelationId} and node {NodeId}", agvCorrelationId, nodeLock.NodeId);
                    await bus.Publish(new FreeNode(nodeLock.NodeId));
                }

                await bus.Publish(new ResolveSystemAgvError(agvCorrelationId, [resourceError!]));
            }
        }

        if (hasQueueBlockingError)
        {
            await bus.Publish(new RemoveAgvFromQueue(agvCorrelationId));
        }

        await bus.Publish(new SolveResourceErrorsReceived(agvCorrelationId));
        logger.LogAgvInformation(LogCodes.AgvOperatorInteraction, agvCorrelationId, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.AgvRemoveFromNode} });

        await context.RespondAsync(new Success());
    }
}