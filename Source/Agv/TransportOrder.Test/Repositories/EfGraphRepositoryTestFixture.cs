using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfGraphRepositoryTestFixture : TransportOrderDbFixture
{
    private readonly SemaphoreProvider<IGraphRepositorySemaphore> _semaphoreProvider =
        new(new Mock<ILogger<SemaphoreProvider<IGraphRepositorySemaphore>>>().Object);

    public EfGraphRepository GraphRepository { get; }

    public EfActionGroupRepository ActionGroupRepository { get; }

    protected override IEnumerable<Profile> GetProfiles() => [new GraphProfile()];

    public EfGraphRepositoryTestFixture()
    {
        GraphRepository = new EfGraphRepository(ContextFactory, Mapper, _semaphoreProvider, Mock.Of<ILogger<EfGraphRepository>>(), Mock.Of<ILoggingService>());
        ActionGroupRepository = new EfActionGroupRepository(Mock.Of<ILogger<EfActionGroupRepository>>(), ContextFactory);
    }

    public async Task<Guid> SetupGraphAsync(GraphNode[] nodes, GraphEdge[]? edges = null)
    {
        edges ??= [];
        await GraphRepository.UpdateGraphAsync(new Graph { Nodes = nodes, Edges = edges });
        var agvId = Guid.NewGuid();
        await GraphRepository.AddOrUpdateAgvGraphAsync(new AgvGraph(agvId, nodes, edges));
        return agvId;
    }

    public async Task AddActionGroupAsync(ActionGroup actionGroup)
    {
        var nodes = new HashSet<GraphNode>();
        foreach (var action in actionGroup.Actions)
        {
            if (action.Target.Node is not null)
            {
                nodes.Add(action.Target.Node);
            }
            foreach (var an in action.AgvActionNodes)
            {
                nodes.Add(an.Node);
            }
        }

        if (nodes.Count > 0)
        {
            await GraphRepository.UpdateGraphAsync(new Graph { Nodes = nodes.ToArray(), Edges = [] });
        }

        await ActionGroupRepository.AddAsync(actionGroup);
    }
}