using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.AgvActions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Profiles;

[TestClass]
public class ActionProfileTest
{
    [TestMethod]
    public void Map_ShouldMapActionsInOrderBySequence_WhenActionGroupIsMappedToActionGroupDto()
    {
        var mapper = MappingHelper.GetMapper(new AgvProfile(), new ActionProfile());

        List<AgvAction> actions = AgvActionGenerator.GeneratePickDropActionGroup().Actions.OrderByDescending(x => x.Sequence).ToList();
        var actionGroup = AgvActionGenerator.GenerateActionGroup(actions);
        var actionGroupDto = mapper.Map<ActionGroupDto>(actionGroup);
        actionGroupDto.Actions.Select(a => a.Sequence).ShouldBeInOrder(SortDirection.Ascending);
    }

    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhenAgvActionIsMappedToAgvActionDto()
    {
        var mapper = MappingHelper.GetMapper(new ActionProfile());

        var action = AgvActionGenerator.GeneratePickDropActionGroup().Actions.First();
        var actionDto = mapper.Map<AgvActionDto>(action);
        
        actionDto.CorrelationId.ShouldBe(action.CorrelationId);
        actionDto.ParentId.ShouldBe(action.ParentId);
        actionDto.Sequence.ShouldBe(action.Sequence);
        actionDto.State.ShouldBe(action.State);
        actionDto.ActionType.ShouldBe(action.ActionType);
        actionDto.TargetNodeId.ShouldBe(action.TargetNodeId);
        actionDto.Nodes.ShouldBe(action.Nodes);
        actionDto.CreatedDate.ShouldBe(action.CreatedDate);
        actionDto.StartedDate.ShouldBe(action.StartedDate);
        actionDto.CompletedDate.ShouldBe(action.CompletedDate);
    }

    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhenActionGroupIsMappedToActionGroupDto()
    {
        var mapper = MappingHelper.GetMapper(new ActionProfile());

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var actionGroupDto = mapper.Map<ActionGroupDto>(actionGroup);
        
        actionGroupDto.CorrelationId.ShouldBe(actionGroup.CorrelationId);
        actionGroupDto.OrderId.ShouldBe(actionGroup.OrderId);
        actionGroupDto.AssignedAgv.ShouldBe(actionGroup.AssignedAgv);
        actionGroupDto.CreatedDate.ShouldBe(actionGroup.CreatedDate);
        actionGroupDto.StartedDate.ShouldBe(actionGroup.StartedDate);
        actionGroupDto.CompletedDate.ShouldBe(actionGroup.CompletedDate);
        actionGroupDto.State.ShouldBe(actionGroup.State);
        actionGroupDto.Actions.ShouldNotBeEmpty();
        actionGroupDto.Actions.Count.ShouldBe(actionGroup.Actions.Count);
        actionGroupDto.IsManual.ShouldBe(actionGroup.IsManual);
    }
}