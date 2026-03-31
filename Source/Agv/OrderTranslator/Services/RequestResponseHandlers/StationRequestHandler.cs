using Gebhardt.Acc.Common.Messages.Stations.Requests;
using Gebhardt.Acc.OrderTranslator.Domain.Models.Station;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.OrderTranslator.Services.RequestResponseHandlers;

/// <summary>
/// Handles station related requests.
/// </summary>
[UsedImplicitly]
public class StationRequestHandler(StationConfiguration configuration):
    IConsumer<GetStationLabels>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetStationLabels> context)
    {
        configuration.Reload();
        await context.RespondAsync(new GotStationLabels(configuration.GetLabels()));
    }
}