using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Represents a handler for node-related requests.
/// </summary>
[UsedImplicitly]
public class NodeStateRequestHandler : IConsumer<GetMapNodeStates>
{
    private readonly INodeRepository _nodeRepository;

    /// <inheritdoc cref="NodeStateRequestHandler"/>>
    public NodeStateRequestHandler(INodeRepository nodeRepository)
    {
        _nodeRepository = nodeRepository;
    }

    /// <summary>
    /// Handles the <see cref="GetMapNodeStates"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetMapNodeStates> context)
    {
        var correlationId = context.Message.CorrelationId;

        var nodeStates = await _nodeRepository.GetNodeStatesForMapAsync(correlationId);

        await context.RespondAsync(new GotMapNodeStates(nodeStates));
    }
}