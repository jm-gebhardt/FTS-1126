using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.Provisioning.Services;

/// <inheritdoc />
[UsedImplicitly]
internal class AgvStatusSeeder : JsonSeeder<AgvStatusReceived>
{
    /// <inheritdoc />
    public AgvStatusSeeder(IBus bus, string basePath, ILogger<AgvStatusSeeder> logger) : base(bus, basePath, logger)
    {
    }

    protected override void Validate(AgvStatusReceived data)
    {
        EnsureNotNull(data.CorrelationId, nameof(data.CorrelationId));
        EnsureNotNull(data.AgvStatus, nameof(data.AgvStatus));
        EnsureNotNull(data.SerialNumber, nameof(data.SerialNumber));
    }

    /// <inheritdoc />
    protected override Task<object> GetMessageAsync(AgvStatusReceived data)
    {
        return Task.FromResult<object>(data);
    }
}