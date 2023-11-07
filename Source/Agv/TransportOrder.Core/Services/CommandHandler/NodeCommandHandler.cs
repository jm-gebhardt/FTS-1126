using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Node;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides handlers for node-related commands.
/// </summary>
[UsedImplicitly]
public class NodeCommandHandler: IConsumer<FreeNode>
{
    private readonly INodeStateRegistry _nodeStateRegistry;
    private readonly ILogger<NodeCommandHandler> _logger;

    /// <inheritdoc cref="NodeCommandHandler"/>>
    public NodeCommandHandler(INodeStateRegistry nodeStateRegistry, ILogger<NodeCommandHandler> logger)
    {
        _nodeStateRegistry = nodeStateRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="FreeNode"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<FreeNode> context)
    {
        string nodeId = context.Message.NodeId;
        if (!await _nodeStateRegistry.FreeNodeAsync(nodeId))
        {
          _logger.LogWarning("Could not free node {NodeId}, node is locked.", nodeId);
        }
    }
}