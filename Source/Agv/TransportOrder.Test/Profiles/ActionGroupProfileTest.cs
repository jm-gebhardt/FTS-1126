using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Profiles;

[TestClass]
public class ActionGroupProfileTest
{
    [TestMethod]
    public void Map_UnsortedActions_FirstNotStartedActionHasSequence0()
    {
        var mapper = MappingHelper.GetMapper(new ActionProfile());
        var correlationId = Guid.NewGuid();
        var actionGroup = new ActionGroup(correlationId, "orderId", new List<AgvAction>
        {
            AgvActionGenerator.GenerateAction(targetNodeId: "D1", actionType: ActionType.Drop, parentId: correlationId, sequence: 1, isSkipped: true),
            AgvActionGenerator.GenerateAction(targetNodeId: "S1", actionType: ActionType.Pick, parentId: correlationId, sequence: 0)
        }, ActionGroupCreator.Wms, isQueued: true);
        var mappedActionGroup = mapper.Map<TransportOrder.Domain.Models.ActionGroup.ActionGroup>(actionGroup);
        mappedActionGroup.FirstNotStartedAction!.ActionType.ShouldBe(ActionType.Pick);
        mappedActionGroup.FirstNotStartedAction.Sequence.ShouldBe(0);
        mappedActionGroup.IsQueued.ShouldBe(actionGroup.IsQueued);
        mappedActionGroup.Actions[0].ShouldSkip.ShouldBeFalse();
        mappedActionGroup.Actions[1].ShouldSkip.ShouldBeTrue();
    }

    [TestMethod]
    public void Map_DomainActionGroupToMessages_KeepsUnsetNodeNull()
    {
        var mapper = MappingHelper.GetMapper(new ActionProfile());
        var correlationId = Guid.NewGuid();
        var actionGroup = new Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup(correlationId, "orderId", new List<Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction>
        {
            new(Guid.NewGuid(), ActionType.Drop, "D1".ToGraphNode(), correlationId) { Sequence = 0 },
            new(Guid.NewGuid(), ActionType.Pick, targetNode: null, correlationId) { Sequence = 2 }
        }, ActionGroupCreator.Wms, isQueued: true);

        var mappedActionGroup = mapper.Map<ActionGroup>(actionGroup);
        mappedActionGroup.Actions.Count.ShouldBe(2);
        mappedActionGroup.Actions.First(a => a.Sequence == 0).Target.Node.ShouldNotBeNull();
        mappedActionGroup.Actions.First(a => a.Sequence == 2).Target.Node.ShouldBeNull();
    }
}