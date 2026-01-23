using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfStationRepositoryTestFixture : ActionGroupRepositoryTestFixture
{
    private readonly SemaphoreProvider<IGraphRepositorySemaphore> _semaphoreProvider = new(new Mock<ILogger<SemaphoreProvider<IGraphRepositorySemaphore>>>().Object);
    private readonly List<(NodeLock, bool)> _nodeLocks = [];

    public EfStationRepository Repository { get; }

    public EfStationRepositoryTestFixture()
    {
        Repository = new EfStationRepository(ContextFactory, _semaphoreProvider, Mapper);
    }

    protected override IEnumerable<Profile> GetProfiles()
    {
        yield return new GraphProfile();
    }

    public async Task AddMap(Guid mapId, string mapName, List<Guid> agvs, List<GraphNode> nodes)
    {
        var map = MapGenerator.Generate(mapName: mapName,
            nodes: nodes,
            correlationId: mapId,
            mapAgvs: agvs.Select(a => new MapAgv(mapId, null, a, null)).ToList()
        );

        await AddAsync(map);
    }

    public EfStationRepositoryTestFixture WithNodeLockAsync(NodeLock nodeLock, bool addNode = true)
    {
        _nodeLocks.Add((nodeLock, addNode));

        return this;
    }

    public new async Task<EfStationRepositoryTestFixture> BuildAsync()
    {
        Build();
        foreach (var nl in _nodeLocks)
        {
            AddInitTask(Task.Run(async () =>
            {
                if (nl.Item2)
                {
                    await AddAsync(nl.Item1);
                }
                await AddNodesAsync([nl.Item1.Node]);
            }));
        }
        await base.BuildAsync();
        return this;
    }
}