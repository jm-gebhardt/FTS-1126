using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
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
public class AgvErrorRequestHandler :
    IConsumer<SolveResourceBlockingErrors>
{
    private readonly ILogger<AgvErrorRequestHandler> _logger;
    private readonly IBus _bus;
    private readonly INodeRepository _nodeRepository;
    private readonly IAgvDb _agvDb;
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Provides handlers for agv error requests.
    /// </summary>
    public AgvErrorRequestHandler(ILogger<AgvErrorRequestHandler> logger, IBus bus, INodeRepository nodeRepository, IAgvDb agvDb, ILoggingService loggingService)
    {
        _logger = logger;
        _bus = bus;
        _nodeRepository = nodeRepository;
        _agvDb = agvDb;
        _loggingService = loggingService;
    }

    /// <summary>
    /// Handles the <see cref="SolveResourceBlockingErrors"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<SolveResourceBlockingErrors> context)
    {
        (var agvCorrelationId, string user, bool ignoreErrorState) = context.Message;
        _logger.LogInformation("Received SolveResourceBlockingErrors request for agv {AgvCorrelationId} with ignoreErrorState={IgnoreErrorState}", agvCorrelationId, ignoreErrorState);
        ICollection<GraphNode> nodeLocks = await _nodeRepository.GetNodesForAgv(agvCorrelationId);
        IReadOnlyList<AgvError> agvErrors = await _agvDb.Errors.GetActiveErrorsAsync(agvCorrelationId);
        var resourceError = agvErrors.SingleOrDefault(e => e.IsResourceError());
        bool hasResourceErrors = resourceError != null;
        var queueBlockingError = agvErrors.SingleOrDefault(e => e.IsQueueBlockingError());
        bool hasQueueBlockingError = queueBlockingError != null;

        if (!ignoreErrorState)
        {
            if (hasResourceErrors)
            {
                foreach (var nodeLock in nodeLocks.Select(n => n.Lock))
                {
                    if (nodeLock == null)
                    {
                        continue;
                    }
                    _logger.LogInformation("Solving resource blocking errors for agv {AgvCorrelationId} and node {NodeId}", agvCorrelationId, nodeLock.NodeId);
                    await _bus.Publish(new FreeNode(nodeLock.NodeId));
                }

                await _bus.Publish(new ResolveSystemAgvError(agvCorrelationId, [resourceError!]));
            }
        }

        if (hasQueueBlockingError)
        {
            await _bus.Publish(new RemoveAgvFromQueue(agvCorrelationId, true));
        }

        await _bus.Publish(new SolveResourceErrorsReceived(agvCorrelationId));
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, agvCorrelationId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.AgvRemoveFromNode } });

        await context.RespondAsync(new Success());
    }
}