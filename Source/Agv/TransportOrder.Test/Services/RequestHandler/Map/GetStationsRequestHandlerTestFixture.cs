using System.Linq.Expressions;
using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using AgvData = Gebhardt.StoreWare.Agv.Common.Domain.Models.AgvData;
using Graph = Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs.Graph;
using GraphNode = Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs.GraphNode;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.RequestHandler.Map;

public class GetStationsRequestHandlerTestFixture : MediatorTestFixture<GetStations, GetStationsRequestHandler>
{
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<IMapRepository> _mapRepository = new();
    private readonly IMapper _mapper = MappingHelper.GetMapper(new GraphProfile());

    public GetStationsRequestHandlerTestFixture WithAgvsRelatedToMap(Guid mapCorrelationId, List<Guid> agvCorrelationIds, List<AgvData> agvData)
    {
        _agvDb.Setup(x => x.Graphs.GetAgvsRelatedToMap(mapCorrelationId)).ReturnsAsync(agvCorrelationIds);
        _agvDb.Setup(x => x.Data.GetSelectedAgvDataAsync(agvCorrelationIds)).ReturnsAsync(agvData);
        _agvDb.Setup(x => x.Data.GetLargestAgvDimensionAsync()).ReturnsAsync((agvData.Max(d => d.Width), agvData.Max(d => d.Length)));
        return this;
    }

    public GetStationsRequestHandlerTestFixture WithGraph(Graph graph)
    {
        _agvDb.Setup(x => x.Graphs.GetNodesAsync(It.IsAny<Expression<Func<EfNode, bool>>>()))
            .ReturnsAsync((Expression<Func<EfNode, bool>> filter) =>
            {
                Func<EfNode, bool> func = filter.Compile();
                return graph.Nodes.Where(n => func(_mapper.Map<EfNode>(n))).ToList();
            });

        _agvDb.Setup(x => x.Graphs.GetNodesAsync(It.IsAny<Guid>(), It.IsAny<Expression<Func<GraphNode, bool>>>())).ReturnsAsync(graph.Nodes);
        _agvDb.Setup(x => x.Graphs.ExistsAsync()).ReturnsAsync(true);
        _agvDb.Setup(x => x.Data.GetLargestAgvDimensionAsync()).ReturnsAsync(( 0,0));
        _mapRepository.Setup(x => x.GetStationsForMapAsync(It.IsAny<Guid>())).ReturnsAsync(graph.Nodes.Where(n => n.Type != AgvGraph.NodeType.WayPoint));
        return this;
    }

    public GetStationsRequestHandlerTestFixture WithAgvGraphs(List<Guid> agvCorrelationIds, List<AgvGraph> agvGraphs)
    {
        _agvDb.Setup(x => x.Graphs.GetSelectedAgvGraphsAsync(agvCorrelationIds)).ReturnsAsync(agvGraphs);
        return this;
    }

    public override GetStationsRequestHandlerTestFixture Build()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => _agvDb.Object);
            sc.AddTransient(_ => _agvDb.Object);
            sc.AddTransient(_ => _mapRepository.Object);
        });
        base.Build();
        return this;
    }
}