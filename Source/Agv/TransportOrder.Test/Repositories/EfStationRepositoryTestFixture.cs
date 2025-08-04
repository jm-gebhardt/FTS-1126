using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfStationRepositoryTestFixture : TransportOrderDbFixture
{
    private readonly SemaphoreProvider<IGraphRepositorySemaphore> _semaphoreProvider = new(new Mock<ILogger<SemaphoreProvider<IGraphRepositorySemaphore>>>().Object);

    public EfStationRepository Repository { get; }

    public EfStationRepositoryTestFixture()
    {
        Repository = new EfStationRepository(ContextFactory, _semaphoreProvider);
    }

    public async Task AddMap(string mapName, List<Guid> agvs, List<EfNode> nodes)
    {
        var map = MapGenerator.Generate(mapId: mapName,
            nodes: nodes,
            correlationId: mapName.ToGuid(),
            mapAgvs: agvs.Select(a => new MapAgv(mapName.ToGuid(), null, a, null)).ToList()
        );

        await AddAsync(map);
    }
}