using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.Provisioning.Models;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Provisioning.Services;

/// <inheritdoc />
[UsedImplicitly]
public class QueueSeeder: JsonSeeder<Queue>
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
        var addQueue = new AddQueue(data.Name, data.Nodes.Select(n => new Common.Messages.Queue.QueueNode(n.NodeId, n.Position)).ToList());
        return await Task.FromResult(addQueue);
    }
}