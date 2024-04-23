using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Provisioning.Services;

/// <inheritdoc />
[UsedImplicitly]
internal class AgvDataSeeder : JsonSeeder<AgvData>
{
    /// <inheritdoc />
    public AgvDataSeeder(IBus bus, string basePath, ILogger<AgvDataSeeder> logger) : base(bus, basePath, logger)
    {
    }

    protected override void Validate(AgvData data)
    {
        EnsureNotNull(data.CorrelationId, nameof(data.CorrelationId));
    }

    /// <inheritdoc />
    protected override Task<object> GetMessageAsync(AgvData data)
    {
        var agvDataReceived = new AgvDataReceived(data.CorrelationId, data.CorrelationId.ToString(), data);
        return Task.FromResult<object>(agvDataReceived);
    }
}