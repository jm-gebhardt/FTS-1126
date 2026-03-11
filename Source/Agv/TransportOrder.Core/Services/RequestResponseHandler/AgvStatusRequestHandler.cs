using Gebhardt.Acc.Common.Messages.System;
using Gebhardt.Acc.TransportOrder.Domain;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles requests on the agv status.
/// </summary>
[UsedImplicitly]
public class AgvStatusRequestHandler(IAgvDb agvDb):
    IConsumer<RequestAgvAvailabilityStatus>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RequestAgvAvailabilityStatus> context)
    {
        bool hasOnlineAgvs = await agvDb.Agvs.HasOnlineAgvs();
        await context.RespondAsync(new AgvAvailabilityStatusReceived(hasOnlineAgvs));
    }
}