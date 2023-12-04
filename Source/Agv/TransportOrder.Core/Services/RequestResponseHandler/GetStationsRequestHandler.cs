using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
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
    private readonly IAgvGraphRepository _agvGraphRepository;
    private readonly IAgvDataRepository _agvDataRepository;

    /// <inheritdoc cref="GetStationsRequestHandler" />
    /// <param name="agvGraphRepository">Repository for persisting the AgvGraph.</param>
    /// <param name="agvDataRepository">Repository to access the AgvData persistence storage.</param>
    public GetStationsRequestHandler(IAgvGraphRepository agvGraphRepository, IAgvDataRepository agvDataRepository)
    {
        _agvGraphRepository = agvGraphRepository;
        _agvDataRepository = agvDataRepository;
    }
    
    /// <summary>
    /// Handles a <see cref="GetStations" /> request.
    /// </summary>
    /// <param name="context"></param>
    /// <returns>Stations of the given map</returns>
    public async Task Consume(ConsumeContext<GetStations> context)
    {
        var mapCorrelationId = context.Message.CorrelationId;
        ICollection<Guid> agvCorrelationIds = await _agvGraphRepository.GetAgvsRelatedToMap(mapCorrelationId);
        ICollection<AgvData> agvsData = await _agvDataRepository.GetSelectedAgvData(agvCorrelationIds);
        double maxWidth = agvsData.Max(a => a.Width) ?? 0;
        double maxLength = agvsData.Max(a => a.Length) ?? 0;
        const double padding = 0.2;
        
        ICollection<AgvGraph> agvGraphs = await _agvGraphRepository.GetSelectedAgvGraphsAsync(agvCorrelationIds);
        List<Station> stations = agvGraphs.SelectMany(g => g.Nodes).Where(n => n.Type != AgvGraph.NodeType.WayPoint).DistinctBy(n => n.Id)
            .Select(s => new Station(s.Id, maxLength + padding, maxWidth + padding)).ToList();
        
        await context.RespondAsync(new GotStations(stations));
    }
}