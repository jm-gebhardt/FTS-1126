using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Gebhardt.Acc.TransportOrder.Domain.Services.Station;
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
    private readonly List<(NodeLock Lock, string Name, bool AddNode)> _nodeLocks = [];

    private readonly IStationOnlineRepository _stationOnlineRepository = new StationOnlineRepository(new StationOnlineDictionaryProvider());

    public EfStationRepository Repository { get; }

    public EfStationRepositoryTestFixture()
    {
        Repository = new EfStationRepository(ContextFactory, _semaphoreProvider, Mapper, _stationOnlineRepository);
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

    public EfStationRepositoryTestFixture WithNodeLockAsync(NodeLock nodeLock, string nodeName, bool addNode = true)
    {
        _nodeLocks.Add((nodeLock, nodeName, addNode));

        return this;
    }

    public new async Task<EfStationRepositoryTestFixture> BuildAsync()
    {
        Build();
        foreach (var nl in _nodeLocks)
        {
            AddInitTask(Task.Run(async () =>
            {
                var node = new GraphNode
                {
                    Id = nl.Lock.NodeId,
                    Name = nl.Name
                };
                if (nl.AddNode)
                {
                    node.Lock = nl.Lock;
                }
                await AddNodesAsync([node]);
            }));
        }
        await base.BuildAsync();
        return this;
    }
}