using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for the <see cref="GetStations" /> request.
/// </summary>
[UsedImplicitly]
public class GetStationsRequestHandler : IConsumer<GetStations>
{
    private readonly IAgvDb _agvDb;

    /// <inheritdoc cref="GetStationsRequestHandler" />
    public GetStationsRequestHandler(IAgvDb agvDb)
    {
        _agvDb = agvDb;
    }

    /// <summary>
    /// Handles a <see cref="GetStations" /> request.
    /// </summary>
    /// <param name="context"></param>
    /// <returns>Stations of the given map</returns>
    public async Task Consume(ConsumeContext<GetStations> context)
    {
        var mapCorrelationId = context.Message.CorrelationId;
        ICollection<Guid> agvCorrelationIds = await _agvDb.Graphs.GetAgvsRelatedToMap(mapCorrelationId);
        ICollection<AgvData> agvsData = await _agvDb.Data.GetSelectedAgvDataAsync(agvCorrelationIds);
        double maxWidth = agvsData.Max(a => a.Width);
        double maxLength = agvsData.Max(a => a.Length);
        const double padding = 0.2;

        List<Station> stations = (await _agvDb.Graphs.GetNodesAsync(n => n.Type != AgvGraph.NodeType.WayPoint))
            .Select(s => new Station(s.Id, maxLength + padding, maxWidth + padding)).ToList();

        await context.RespondAsync(new GotStations(stations));
    }
}