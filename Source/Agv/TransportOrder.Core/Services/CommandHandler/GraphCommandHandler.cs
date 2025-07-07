using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Consumer for <see cref="UpdateGraphRequest"/>.
/// </summary>
[UsedImplicitly]
public class GraphCommandHandler : IConsumer<UpdateGraphRequest>
{
    private readonly IGraphRepository _graphRepository;
    private readonly ILogger<GraphCommandHandler> _logger;

    /// <inheritdoc cref="GraphCommandHandler"/>
    public GraphCommandHandler(IGraphRepository graphRepository, ILogger<GraphCommandHandler> logger)
    {
        _graphRepository = graphRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<UpdateGraphRequest> context)
    {
        _logger.LogInformation("Update Graph");

        var graph = context.Message.Graph;
        graph.Validate();

        await _graphRepository.UpdateGraphAsync(graph);

        if (context.Message.Maps != null)
        {
            await context.Publish(new SaveMaps(context.Message.Maps));
        }

        _logger.LogInformation("Graph {CorrelationId} updated", context.Message.CorrelationId);
        await context.Publish(new GraphUpdated(context.Message.CorrelationId, graph, context.Message.Maps), context.CancellationToken);
        await context.RespondAsync(new Success());
    }
}