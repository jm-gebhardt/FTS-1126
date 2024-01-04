using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Profiles;

[TestClass]
public class ActionGroupProfileTest
{
    [TestMethod]
    public void Map_UnsortedActions_FirstNotStartedActionHasSequence0()
    {
        var mapper = MappingHelper.GetMapper(new ActionProfile());
        var correlationId = Guid.NewGuid();
        var actionGroup = new ActionGroup(correlationId, "orderId" , new List<AgvAction>{new(Guid.NewGuid(), ActionType.Drop, "D1", correlationId){Sequence = 1}, new(Guid.NewGuid(), ActionType.Pick, "S1", correlationId) { Sequence = 0} }, ActionGroupCreator.Wms);
        var mappedActionGroup = mapper.Map<TransportOrder.Domain.Models.ActionGroup.ActionGroup>(actionGroup);
        mappedActionGroup.FirstNotStartedAction!.ActionType.ShouldBe(ActionType.Pick);
        mappedActionGroup.FirstNotStartedAction.Sequence.ShouldBe(0);
    }
}