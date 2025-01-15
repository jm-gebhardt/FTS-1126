using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Map;

public class StationsRequestHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer, MockConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly Mock<IMapRepository> _mapRepository = new();
    private readonly Mock<IStationRepository> _stationRepository = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();

    public StationsRequestHandlerTestFixture<TRequestClient, TConsumer> WithStation(Station station)
    {
        _stationRepository.Setup(x => x.GetStations(It.IsAny<string?>())).ReturnsAsync([station]);
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(station);
        return this;
    }

    public StationsRequestHandlerTestFixture<TRequestClient, TConsumer> WithMap(TransportOrder.Domain.Models.Map.Map map)
    {
        _mapRepository.Setup(x => x.GetMapAsync(It.IsAny<Guid>())).ReturnsAsync(map);
        return this;
    }

    public StationsRequestHandlerTestFixture<TRequestClient, TConsumer> WithActionGroups(List<TransportOrder.Domain.Models.ActionGroup.ActionGroup> actionGroups)
    {
        _actionGroupRepository.Setup(x => x.GetCurrentActionGroupsForStation(It.IsAny<string>())).ReturnsAsync(actionGroups);
        _actionGroupRepository.Setup(x => x.GetPausedActionGroupsForStation(It.IsAny<string>())).ReturnsAsync(actionGroups.Where(g => g.State == ActionGroupState.Paused).ToList());
        return this;
    }

    public override StationsRequestHandlerTestFixture<TRequestClient, TConsumer> Build()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => _mapRepository.Object);
            sc.AddTransient(_ => _stationRepository.Object);
            sc.AddTransient(_ => _actionGroupRepository.Object);
        });
        base.Build();
        return this;
    }
}