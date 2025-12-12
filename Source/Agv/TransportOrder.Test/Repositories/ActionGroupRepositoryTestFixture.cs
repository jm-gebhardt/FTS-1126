using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class ActionGroupRepositoryTestFixture : NodeTestFixture
{
    private readonly List<ActionGroup> _actionGroups = [];

    public EfActionGroupRepository ActionGroupRepository { get; }

    protected override IEnumerable<Profile> GetProfiles()
    {
        return [new GraphProfile()];
    }

    public ActionGroupRepositoryTestFixture()
    {
        ActionGroupRepository = new EfActionGroupRepository(new Mock<ILogger<EfActionGroupRepository>>().Object, ContextFactory);
    }

    public ActionGroupRepositoryTestFixture WithActionGroups(IEnumerable<ActionGroup> actionGroups)
    {
        foreach (var actionGroup in actionGroups)
        {
            WithActionGroup(actionGroup);
        }
        return this;
    }

    public ActionGroupRepositoryTestFixture WithActionGroup(ActionGroup actionGroup)
    {
        _actionGroups.Add(actionGroup);
        return this;
    }

    public new async Task<ActionGroupRepositoryTestFixture> BuildAsync()
    {
        Build();
        await AddActionGroupsAsync();
        await base.BuildAsync();
        return this;
    }

    private async Task AddActionGroupsAsync()
    {
        foreach (var actionGroup in _actionGroups)
        {
            await AddActionGroupAsync(actionGroup);
        }
    }

    public async Task AddActionGroupAsync(IEnumerable<ActionGroup> actionGroup, bool addStraightAway = false)
    {
        foreach (var actionGroupToAdd in actionGroup)
        {
            await AddActionGroupAsync(actionGroupToAdd, addStraightAway);
        }
    }

    public async Task AddActionGroupAsync(ActionGroup actionGroup, bool addStraightAway = false)
    {
        HashSet<GraphNode> nodes = await AddNodesAsync(actionGroup, addStraightAway);

        if (addStraightAway)
        {
            await ActionGroupRepository.AddAsync(actionGroup);
            LoadNodes(actionGroup, nodes);
        }
        else
        {
            AddInitTask(Task.Run(async () =>
            {
                bool result = await ActionGroupRepository.AddAsync(actionGroup);
                LoadNodes(actionGroup, nodes);
                return result;
            }));
        }
    }

    private void LoadNodes(ActionGroup actionGroup, HashSet<GraphNode> nodes)
    {
        foreach (var action in actionGroup.Actions)
        {
            if (action.Target.NodeId != null)
            {
                action.Target.Node = nodes.First(n => n.Id == action.Target.NodeId);
            }
            foreach (var actionNode in action.AgvActionNodes)
            {
                actionNode.Node = nodes.First(n => n.Id == actionNode.NodeId);
            }
        }
    }
}