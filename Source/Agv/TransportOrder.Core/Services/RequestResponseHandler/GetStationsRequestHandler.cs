using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Filters;
using JetBrains.Annotations;
using MassTransit;
using GraphNode = Gebhardt.Acc.Common.Domain.Models.Graphs.GraphNode;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for station related requests.
/// </summary>
[UsedImplicitly]
public class GetStationsRequestHandler : IConsumer<GetMapStations>, IConsumer<GetStations>, IConsumer<GetStation>
{
    // additional padding for the stations
    private const double Padding = 0.2;

    private readonly IAgvDb _agvDb;
    private readonly IMapRepository _mapRepository;
    private readonly IGraphRepository _graphRepository;

    /// <inheritdoc cref="GetStationsRequestHandler" />
    public GetStationsRequestHandler(IAgvDb agvDb, IMapRepository mapRepository, IGraphRepository graphRepository)
    {
        _agvDb = agvDb;
        _mapRepository = mapRepository;
        _graphRepository = graphRepository;
    }

    private static Station GetStation(GraphNode stationNode, double maxLength, double maxWidth)
    {
        return new Station(stationNode.Id, maxLength + Padding, maxWidth + Padding, stationNode.Type);
    }

    private static List<Station> GetStations(IEnumerable<GraphNode> stationNodes, double maxLength, double maxWidth)
    {
        return stationNodes.Select(s => GetStation(s, maxLength, maxWidth)).ToList();
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetMapStations> context)
    {
        Task<IEnumerable<GraphNode>> stationNodesTask = _mapRepository.GetStationNodesForMapAsync(context.Message.CorrelationId);
        Task<(double maxWidth, double maxLength)> stationSizeTask = _agvDb.Data.GetLargestAgvDimensionAsync();

        IEnumerable<GraphNode> stationNodes = await stationNodesTask;
        (double maxWidth, double maxLength) = await stationSizeTask;

        List<Station> stations = GetStations(stationNodes, maxLength, maxWidth);
        await context.RespondAsync(new GotMapStations(stations));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetStations> context)
    {
        Task<IEnumerable<GraphNode>> stationNodesTask = _graphRepository.GetNodesAsync(NodeFilters.IsStation);
        Task<(double maxWidth, double maxLength)> stationSizeTask = _agvDb.Data.GetLargestAgvDimensionAsync();

        IEnumerable<GraphNode> stationNodes = await stationNodesTask;
        (double maxWidth, double maxLength) = await stationSizeTask;

        List<Station> stations = GetStations(stationNodes, maxLength, maxWidth);
        await context.RespondAsync(new GotStations(stations));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetStation> context)
    {
        Task<GraphNode> stationNodeTask = _graphRepository.GetNodeAsync(context.Message.CorrelationId);
        Task<(double maxWidth, double maxLength)> stationSizeTask = _agvDb.Data.GetLargestAgvDimensionAsync();

        var stationNode = await stationNodeTask;

        if (!stationNode.IsStation())
        {
            throw new InvalidNodeTypeException(stationNode.Id, stationNode.Type.ToString(), "station");
        }

        (double maxWidth, double maxLength) = await stationSizeTask;

        var station = GetStation(stationNode, maxLength, maxWidth);
        await context.RespondAsync(new GotStation(station));
    }
}