using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Queue;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Queue = Gebhardt.Acc.Provisioning.Models.Queue;

namespace Gebhardt.Acc.Provisioning.Services;

/// <inheritdoc />
[UsedImplicitly]
public class QueueSeeder : JsonSeeder<Queue>
{
    /// <inheritdoc />
    public QueueSeeder(IBus bus, string basePath, ILogger<QueueSeeder> logger) : base(bus, basePath, logger)
    {
    }

    /// <inheritdoc />
    protected override void Validate(Queue data)
    {
        EnsureNotNull(data.Name, nameof(data.Name));
        EnsureNotNull(data.Nodes, nameof(data.Nodes));
    }

    /// <inheritdoc />
    protected override async Task<object> GetMessageAsync(Queue data)
    {
        var addQueue = new AddQueue(data.Name, data.Nodes.Select(n => new QueueNode(n.NodeId.ToBaseNode(), n.Position, null, null)).ToList());
        return await Task.FromResult(addQueue);
    }
}