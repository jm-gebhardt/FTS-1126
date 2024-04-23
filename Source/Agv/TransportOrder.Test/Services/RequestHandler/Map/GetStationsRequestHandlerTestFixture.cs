using System.Linq.Expressions;
using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using AgvData = Gebhardt.Acc.Common.Domain.Models.AgvData;
using Graph = Gebhardt.Acc.Common.Domain.Models.Graphs.Graph;
using GraphNode = Gebhardt.Acc.Common.Domain.Models.Graphs.GraphNode;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Map;

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