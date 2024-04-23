using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides handlers for node-related commands.
/// </summary>
[UsedImplicitly]
public class NodeCommandHandler : IConsumer<FreeNode>
{
    private readonly INodeStateRepository _nodeStateRepository;
    private readonly ILogger<NodeCommandHandler> _logger;

    /// <inheritdoc cref="NodeCommandHandler"/>>
    public NodeCommandHandler(INodeStateRepository nodeStateRepository, ILogger<NodeCommandHandler> logger)
    {
        _nodeStateRepository = nodeStateRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="FreeNode"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<FreeNode> context)
    {
        string nodeId = context.Message.NodeId;
        if (!await _nodeStateRepository.FreeNodeAsync(nodeId))
        {
            _logger.LogWarning("Could not free node {NodeId}, node is locked.", nodeId);
        }

        var nodeState = await _nodeStateRepository.GetNodeStateAsync(nodeId);
        if (nodeState == null)
        {
            _logger.LogWarning("Could not retrieve node state for node {NodeId}", nodeId);
            return;
        }
        await context.Publish(new NodeStateChanged(nodeState));
    }
}