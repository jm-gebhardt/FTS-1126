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
            new(Guid.NewGuid(), ActionType.Drop, "D1", correlationId) { Sequence = 1, IsSkipped = true}, 
            new(Guid.NewGuid(), ActionType.Pick, "S1", correlationId) { Sequence = 0 }
        }, ActionGroupCreator.Wms, isQueued: true);
        var mappedActionGroup = mapper.Map<TransportOrder.Domain.Models.ActionGroup.ActionGroup>(actionGroup);
        mappedActionGroup.FirstNotStartedAction!.ActionType.ShouldBe(ActionType.Pick);
        mappedActionGroup.FirstNotStartedAction.Sequence.ShouldBe(0);
        mappedActionGroup.IsQueued.ShouldBe(actionGroup.IsQueued);
        mappedActionGroup.Actions[0].ShouldSkip.ShouldBeFalse();
        mappedActionGroup.Actions[1].ShouldSkip.ShouldBeTrue();
    }
}