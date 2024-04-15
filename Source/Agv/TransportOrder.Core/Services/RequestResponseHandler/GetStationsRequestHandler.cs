using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using JetBrains.Annotations;
using MassTransit;
using GraphNode = Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs.GraphNode;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for the <see cref="GetStations" /> request.
/// </summary>
[UsedImplicitly]
public class GetStationsRequestHandler : IConsumer<GetStations>
{
    private readonly IAgvDb _agvDb;

    private readonly IMapRepository _mapRepository;

    /// <inheritdoc cref="GetStationsRequestHandler" />
    public GetStationsRequestHandler(IAgvDb agvDb, IMapRepository mapRepository)
    {
        _agvDb = agvDb;
        _mapRepository = mapRepository;
    }

    /// <summary>
    /// Handles a <see cref="GetStations" /> request.
    /// </summary>
    /// <param name="context"></param>
    /// <returns>Stations of the given map</returns>
    public async Task Consume(ConsumeContext<GetStations> context)
    {
        const double padding = 0.2;
        var mapCorrelationId = context.Message.CorrelationId;
        IEnumerable<GraphNode> graphNodeStations = await _mapRepository.GetStationsForMapAsync(mapCorrelationId);
        List<Station> stations = graphNodeStations.Select(s => new Station(s.Id, 0, 0)).ToList();

        (double maxWidth, double maxLength) = await _agvDb.Data.GetLargestAgvDimensionAsync();
 
        stations = stations.Select(s => new Station(s.Id, maxLength + padding, maxWidth + padding)).ToList();
 
        await context.RespondAsync(new GotStations(stations));
    }
}