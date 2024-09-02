using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for station related requests.
/// </summary>
[UsedImplicitly]
public class GetStationsRequestHandler : IConsumer<GetMapStations>, IConsumer<GetStations>, IConsumer<GetStation>
{
    private readonly IMapRepository _mapRepository;
    private readonly IStationRepository _stationRepository;

    /// <inheritdoc cref="GetStationsRequestHandler" />
    public GetStationsRequestHandler(IMapRepository mapRepository, IStationRepository stationRepository)
    {
        _mapRepository = mapRepository;
        _stationRepository = stationRepository;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetMapStations> context)
    {
        var map = await _mapRepository.GetMapAsync(context.Message.CorrelationId);
        ICollection<Domain.Models.Station.Station> stations = await _stationRepository.GetStations(map.MapId);
        await context.RespondAsync(new GotMapStations(stations));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetStations> context)
    {
        ICollection<Domain.Models.Station.Station> stations = await _stationRepository.GetStations();
        await context.RespondAsync(new GotStations(stations));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetStation> context)
    {
        var station = await _stationRepository.GetStation(context.Message.CorrelationId);
        await context.RespondAsync(new GotStation(station));
    }
}