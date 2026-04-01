using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Models.Housekeeping;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Housekeeping;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles housekeeping related requests.
/// </summary>
[UsedImplicitly]
public class HousekeepingRequestHandler(ILogger<HousekeepingRequestHandler> logger, IHousekeepingConfigurationRepository housekeepingConfigurationRepository) :
    IConsumer<UpdateHousekeepingConfiguration>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<UpdateHousekeepingConfiguration> context)
    {
        var (value, interval) = context.Message;

        logger.LogInformation("Updating agv error housekeeping to {Value} {Interval}", value, interval.ToString());

        await housekeepingConfigurationRepository.AddOrUpdateAsync(new HousekeepingConfiguration(Guid.NewGuid(), value, interval));

        await context.RespondAsync(new Success());
    }
}