using Gebhardt.Acc.OrderTranslator.Domain.Repositories;
using Gebhardt.Acc.OrderTranslator.Messages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.OrderTranslator.Services.CommandHandler;

/// <summary>
/// Handles station related commands.
/// </summary>
[UsedImplicitly]
public class StationCommandHandler(IStationRepository stationRepository, ILogger<StationCommandHandler> logger):
    IConsumer<CreateStation>

{
    /// <inheritdoc/>>
    public async Task Consume(ConsumeContext<CreateStation> context)
    {
        var station = context.Message.Station;

        await stationRepository.AddAsync(station);
        logger.LogInformation("Station {Name} created", station.Name);
    }
}