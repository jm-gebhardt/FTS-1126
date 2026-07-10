using Gebhardt.Acc.Common.Domain.Models.Stations;
using Gebhardt.Acc.Common.Messages.Stations.Requests;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles cross-service requests for station node types.
/// </summary>
[UsedImplicitly]
public class StationTypeRequestHandler(IStationRepository stationRepository) : IConsumer<GetStationTypes>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetStationTypes> context)
    {
        ICollection<Station> stations = await stationRepository.GetStations();
        List<StationTypeInfo> stationTypes = stations
            .Select(s => new StationTypeInfo(s.Name, s.PrimaryType, s.AdditionalTypes))
            .ToList();
        await context.RespondAsync(new GotStationTypes(stationTypes));
    }
}