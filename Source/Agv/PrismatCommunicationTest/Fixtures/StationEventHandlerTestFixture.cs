using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Services.EventHandler;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.PrismatCommunicationTest.Fixtures;

public class StationEventHandlerTestFixture : MassTransitConsumerTestFixture<StationEventHandler>, IDisposable
{
    private readonly Mock<IShortLoopWeTargetStationRepository> _targetStationRepository = new();
    public readonly KaeserGraphConfiguration Configuration = OrderGenerator.KaeserConfiguration;
    private readonly Mock<IStationRepository> _stationRepository = new();

    public StationEventHandlerTestFixture()
    {
        _targetStationRepository.Setup(x => x.UpdateAsync(It.IsAny<ShortLoopWeTargetStation>())).Verifiable();
        AddServices(s =>
        {
            s.AddTransient(_ => _targetStationRepository.Object);
            s.AddTransient(_ => _stationRepository.Object);
            s.AddSingleton(_ => Configuration);
        });
        Build();
    }

    public void Dispose()
    {
        _targetStationRepository.Invocations.Clear();
    }

    public StationEventHandlerTestFixture WithShortLoopTarget(ShortLoopWeTargetStation target)
    {
        _targetStationRepository.Setup(x => x.GetTargetStation()).ReturnsAsync(target);
        return this;
    }

    public void VerifyShortLoopTargetUpdated(Times times)
    {
        _targetStationRepository.Verify(x => x.UpdateAsync(It.IsAny<ShortLoopWeTargetStation>()), times);
    }

    public StationEventHandlerTestFixture WithStation(Station station)
    {
        _stationRepository.Setup(x => x.GetStationByNodeId(station.NodeId)).ReturnsAsync(station);
        _stationRepository.Setup(x => x.GetStationById(station.Id)).ReturnsAsync(station);
        return this;
    }

    public StationEventHandlerTestFixture WithOccupiedStation(Station? station)
    {
        _stationRepository.Setup(x => x.GetStationByOccupyingAgv(It.IsAny<Guid>())).ReturnsAsync(station);
        return this;
    }
}