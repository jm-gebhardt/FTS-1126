using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Provisioning.Services;

/// <inheritdoc />
[UsedImplicitly]
internal class AgvGraphSeeder : JsonSeeder<AgvGraphReceived>
{
    /// <inheritdoc />
    public AgvGraphSeeder(IBus bus, string basePath, ILogger<AgvGraphSeeder> logger) : base(bus, basePath, logger)
    {
    }

    protected override void Validate(AgvGraphReceived data)
    {
        EnsureNotNull(data.CorrelationId, nameof(data.CorrelationId));
        EnsureNotNull(data.SerialNumber, nameof(data.SerialNumber));
        EnsureNotNull(data.Graph, nameof(data.Graph));
    }

    /// <inheritdoc />
    protected override Task<object> GetMessageAsync(AgvGraphReceived data)
    {
        return Task.FromResult<object>(data);
    }
}