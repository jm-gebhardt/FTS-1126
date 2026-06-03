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
        var msg = context.Message;

        logger.LogInformation(
            "Updating housekeeping configuration for {EntityType}: outdated after {OutdatedValue} {OutdatedInterval}, schedule every {ScheduleValue} {ScheduleInterval} at {Hour:D2}:{Minute:D2}",
            msg.EntityType, msg.OutdatedValue, msg.OutdatedInterval,
            msg.ScheduleValue, msg.ScheduleInterval, msg.ScheduleHour, msg.ScheduleMinute);

        await housekeepingConfigurationRepository.AddOrUpdateAsync(new HousekeepingConfiguration(
            Guid.NewGuid(),
            msg.EntityType,
            msg.OutdatedValue,
            msg.OutdatedInterval,
            msg.ScheduleValue,
            msg.ScheduleInterval,
            msg.ScheduleHour,
            msg.ScheduleMinute));

        await context.RespondAsync(new Success());
    }
}
