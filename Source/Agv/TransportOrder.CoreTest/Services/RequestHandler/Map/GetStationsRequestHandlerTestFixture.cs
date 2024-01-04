using System.Linq.Expressions;
using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Map;

public class GetStationsRequestHandlerTestFixture : MediatorTestFixture<GetStations, GetStationsRequestHandler>
{
    private readonly Mock<IGraphRepository> _agvGraphRepository = new();
    private readonly Mock<IAgvDataRepository> _agvDataRepository = new();
    private readonly IMapper _mapper = MappingHelper.GetMapper(new GraphProfile());

    public GetStationsRequestHandlerTestFixture WithAgvsRelatedToMap(Guid mapCorrelationId, List<Guid> agvCorrelationIds, List<AgvData> agvData)
    {
        _agvGraphRepository.Setup(x => x.GetAgvsRelatedToMap(mapCorrelationId)).ReturnsAsync(agvCorrelationIds);
        _agvDataRepository.Setup(x => x.GetSelectedAgvDataAsync(agvCorrelationIds)).ReturnsAsync(agvData);
        return this;
    }

    public GetStationsRequestHandlerTestFixture WithGraph(Graph graph)
    {
        _agvGraphRepository.Setup(x => x.GetNodesAsync(It.IsAny<Expression<Func<EfNode, bool>>>()))
            .ReturnsAsync((Expression<Func<EfNode, bool>> filter) =>
            {
                Func<EfNode, bool> func = filter.Compile();
                return graph.Nodes.Where(n => func(_mapper.Map<EfNode>(n))).ToList();
            });

        _agvGraphRepository.Setup(x => x.GetNodesAsync(It.IsAny<Guid>(), It.IsAny<Expression<Func<GraphNode, bool>>>())).ReturnsAsync(graph.Nodes);
        _agvGraphRepository.Setup(x => x.ExistsAsync()).ReturnsAsync(true);
        return this;
    }

    public GetStationsRequestHandlerTestFixture WithAgvGraphs(List<Guid> agvCorrelationIds, List<AgvGraph> agvGraphs)
    {
        _agvGraphRepository.Setup(x => x.GetSelectedAgvGraphsAsync(agvCorrelationIds)).ReturnsAsync(agvGraphs);
        return this;
    }

    public override GetStationsRequestHandlerTestFixture Build()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => _agvGraphRepository.Object);
            sc.AddTransient(_ => _agvDataRepository.Object);
        });
        base.Build();
        return this;
    }
}