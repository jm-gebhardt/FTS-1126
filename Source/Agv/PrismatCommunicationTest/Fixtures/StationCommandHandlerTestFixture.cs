using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Services.CommandHandler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.PrismatCommunicationTest.Fixtures;

public class StationCommandHandlerTestFixture : MassTransitConsumerTestFixture<StationCommandHandler>, IDisposable
{
    private readonly Mock<IStationRepository> _stationRepository = new();

    public StationCommandHandlerTestFixture()
    {
        _stationRepository.Setup(x => x.AddOrUpdateStationsOfGroup(It.IsAny<ICollection<Station>>(), It.IsAny<Guid>())).Verifiable();
        AddServices(s =>
        {
            s.AddTransient(_ => _stationRepository.Object);
            s.AddTransient(_ => new Mock<ILogger<StationCommandHandler>>().Object);
        });
        Build();
    }

    public StationCommandHandlerTestFixture WithStation(Station? station)
    {
        _stationRepository.Setup(x => x.GetStationByOccupyingAgv(It.Is<Guid>(g => station != null && g == station.AgvCorrelationId))).ReturnsAsync(station);
        _stationRepository.Setup(x => x.GetStationByNode(It.IsAny<string>())).ReturnsAsync(station);
        _stationRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(station!);
        return this;
    }

    public void VerifyStationGroupWasAdded(Times times)
    {
        _stationRepository.Verify(x => x.AddOrUpdateStationsOfGroup(It.IsAny<ICollection<Station>>(), It.IsAny<Guid>()), times);
    }

    public void Dispose()
    {
        _stationRepository.Invocations.Clear();
    }
}