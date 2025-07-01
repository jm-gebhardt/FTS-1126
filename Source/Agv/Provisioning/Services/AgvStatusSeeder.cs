using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Provisioning.Services;

/// <inheritdoc />
[UsedImplicitly]
public class AgvStatusSeeder : JsonSeeder<AgvStatusReceived>
{
    /// <inheritdoc />
    public AgvStatusSeeder(IBus bus, string basePath, ILogger<AgvStatusSeeder> logger) : base(bus, basePath, logger)
    {
    }

    /// <inheritdoc />
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