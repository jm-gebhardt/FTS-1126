using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Zone;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Zones;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Handles <see cref="Zone"/> related commands.
/// </summary>
[UsedImplicitly]
public class ZoneCommandHandler(ILogger<ZoneCommandHandler> logger, IAgvDb agvDb, IMapper mapper):
    IConsumer<AddZone>

{
    /// <summary>
    /// Handles the <see cref="AddZone"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AddZone> context)
    {
        var agvZone = context.Message.Zone;
        var zone = mapper.Map<Zone>(agvZone);
        await agvDb.Zones.AddOrUpdateZone(zone);
        logger.LogInformation("Zone {ZoneId} stored", zone.ZoneId);
    }
}