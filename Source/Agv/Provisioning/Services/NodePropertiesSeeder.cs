using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Graphs;
using Gebhardt.Acc.Provisioning.Models;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Provisioning.Services;

/// <inheritdoc cref="JsonSeeder{NodeProperties}" />
[UsedImplicitly]
public class NodePropertiesSeeder : JsonSeeder<List<NodeProperties>>
{
    /// <inheritdoc cref="JsonSeeder{NodeProperties}" />
    public NodePropertiesSeeder(IBus bus, string basePath, ILogger logger, string fileName) : base(bus, basePath, logger, fileName)
    {
    }

    /// <inheritdoc />
    protected override void Validate(List<NodeProperties> data)
    {
        foreach (var property in data)
        {
            EnsureNotNull(property.NodeId, nameof(property.NodeId));
        }
    }

    /// <inheritdoc />
    protected override Task<object> GetMessageAsync(List<NodeProperties> data)
    {
        var seedingTasks = data.Select(d => new InitializeNodeApproachingProperties(d.NodeId.ToGuid(), d.PickLiftHeight, d.DropLiftHeight, d.DockingDirection)).ToList();
        return Task.FromResult<object>(seedingTasks);
    }
}