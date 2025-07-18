using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;
using Gebhardt.Acc.KaeserSidecar.Services.CommandHandler;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.CommandHandler;

public class StationCommandHandlerTestFixture : MassTransitConsumerTestFixture<StationCommandHandler>
{
    public StationRepository StationRepository { get; }

    public List<StationOccupancy> Occupancies { get; private set; } = new();

    public StationCommandHandlerTestFixture()
    {
        AddServices(s =>
        {
            s.AddSingleton<IStationRepository, StationRepository>();
            s.AddSingleton(Mock.Of<ILogger<StationCommandHandler>>());
            s.AddSingleton(OrderGenerator.KaeserConfiguration);
        });
        Build();

        StationRepository = (Provider.GetRequiredService<IStationRepository>() as StationRepository)!;
    }

    public StationCommandHandlerTestFixture WithOccupancies(params (int Count, Guid? AgvId)[] occupiedCountAndAgv)
    {
        Occupancies = occupiedCountAndAgv.Select(o => new StationOccupancy($"Station-{Guid.NewGuid()}", o.Count, o.AgvId)).ToList();

        StationRepository.WithOccupancies(Occupancies);
        return this;
    }
}