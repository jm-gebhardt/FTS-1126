using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Map;

public class GetStationsRequestHandlerTestFixture : MediatorTestFixture<GetStations, GetStationsRequestHandler>
{
    private readonly Mock<IAgvGraphRepository> _agvGraphRepository = new();
    private readonly Mock<IAgvDataRepository> _agvDataRepository = new();

    public GetStationsRequestHandlerTestFixture WithAgvsRelatedToMap(Guid mapCorrelationId, List<Guid> agvCorrelationIds, List<AgvData> agvData)
    {
        _agvGraphRepository.Setup(x => x.GetAgvsRelatedToMap(mapCorrelationId)).ReturnsAsync(agvCorrelationIds);
        _agvDataRepository.Setup(x => x.GetSelectedAgvData(agvCorrelationIds)).ReturnsAsync(agvData);
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