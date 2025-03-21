using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Messages.Triggers.Commands;
using Gebhardt.Acc.Provisioning.Models;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Provisioning.Services;

/// <inheritdoc />
[UsedImplicitly]
internal class TriggerLocationSeeder : JsonSeeder<TriggerLocation>
{
    /// <inheritdoc />
    public TriggerLocationSeeder(IBus bus, string basePath, ILogger<TriggerLocationSeeder> logger) : base(bus, basePath, logger)
    {
    }

    protected override void Validate(TriggerLocation data)
    {
        EnsureNotNull(data.TriggerUrn, nameof(data.TriggerUrn));
        EnsureNotNull(data.TriggerVersion, nameof(data.TriggerVersion));
    }

    /// <inheritdoc />
    protected override async Task<object> GetMessageAsync(TriggerLocation data)
    {
        var triggerGuid = TriggerProperties.GetTriggerId(data.TriggerUrn, data.TriggerVersion);
        
        ICollection<PolygonPoint> polygonPoints = data.Polygon ?? [];
        var addTriggerLocation = new AddTriggerLocation(triggerGuid, data.NodeId, data.EdgeId, polygonPoints, data.MapId, data.ConfigurationParameters);

        return await Task.FromResult(addTriggerLocation);
    }
}