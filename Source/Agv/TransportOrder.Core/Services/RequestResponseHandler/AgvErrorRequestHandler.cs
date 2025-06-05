using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Domain;
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
    IConsumer<SolveRessourceBlockingErrors>
{
    /// <summary>
    /// Handles the <see cref="SolveRessourceBlockingErrors"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<SolveRessourceBlockingErrors> context)
    {
        (var agvCorrelationId, bool ignoreErrorState) = context.Message;
        ICollection<NodeLock> nodeLocks = await nodeStateRepository.GetNodeLocksForAgv(agvCorrelationId);
        IList<AgvError> agvErrors = await agvDb.Errors.GetActiveErrorsAsync(agvCorrelationId);
        bool hasRessourceErrors = ignoreErrorState || agvErrors.Any(e => e.IsRessourceError());
        var position = agvDb.Positions.GetPosition(agvCorrelationId);

        if (hasRessourceErrors && nodeLocks.Count > 0)
        {
            foreach (var nodeLock in nodeLocks)
            {
                logger.LogInformation("Solving ressource blocking errors for agv {AgvCorrelationId} and node {NodeId}", agvCorrelationId, nodeLock.NodeId);
                await bus.Publish(new FreeNode(nodeLock.NodeId));
            }
        }

        if (!ignoreErrorState)
        {
            agvErrors = agvErrors.Where(e => !e.IsRessourceError()).ToList();
            await bus.Publish(new UpdateErrorStatus(agvCorrelationId, agvErrors.ToList(), position!.AgvPosition));
        }

        await bus.Publish(new SolveResourceErrorsReceived(agvCorrelationId));

        await context.RespondAsync(new Success());
    }
}