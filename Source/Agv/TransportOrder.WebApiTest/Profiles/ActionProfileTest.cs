using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using TransportOrder.Domain.Profiles;
using TransportOrder.Domain.ReadModels;
using ActionGroup = TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Profiles;
[TestClass]
public class ActionProfileTest
{
    [TestMethod]
    public void Map_ActionGroupToActionGroupDto_MapsActionsInOrderBySequence()
    {
        var mapper = MappingHelper.GetMapper(new AgvProfile(), new ActionProfile(), new AgvErrorProfile(), new ActionGroupProfile());

        List<AgvAction> actions = AgvActionGenerator.GeneratePickDropActionGroup().Actions.OrderByDescending(x => x.Sequence).ToList();
        var actionGroup = AgvActionGenerator.GenerateActionGroup(actions);
        var actionGroupDto = mapper.Map<ActionGroupDto>(mapper.Map<ActionGroup>(actionGroup).ConvertFromActionGroupWithFakeNodeType());
        actionGroupDto.Actions.Select(a => a.Sequence).ShouldBeInOrder(SortDirection.Ascending);
    }

    [TestMethod]
    public void Map_ActionToActionDto_MapsAllPropertiesCorrectly()
    {
        var mapper = MappingHelper.GetMapper(new ActionProfile());

        var action = AgvActionGenerator.GeneratePickDropActionGroup().Actions.First().ConvertFromAgvActionWithFakeNodeType();
        var actionDto = mapper.Map<AgvActionDto>(action);
        actionDto.CorrelationId.ShouldBe(action!.CorrelationId);
        actionDto.ParentId.ShouldBe(action.ParentId);
        actionDto.Sequence.ShouldBe(action.Sequence);
        actionDto.State.ShouldBe(action.State);
        actionDto.ActionType.ShouldBe(action.ActionType);
        actionDto.TargetNodeId.ShouldBe(action.TargetNodeId);
        actionDto.Nodes.ShouldBe(action.Nodes);
        actionDto.CreatedDate.ShouldBe(action.CreatedDate);
        actionDto.StartedDate.ShouldBe(action.StartedDate!);
        actionDto.CompletedDate.ShouldBe(action.CompletedDate);
        actionDto.TargetNodeType.ShouldBe(action.TargetNodeType);

       
    }

    [TestMethod]
    public void Map_ActionGroupToActionGroupDto_MapsAllPropertiesCorrectly()
    {
        var mapper = MappingHelper.GetMapper(new ActionProfile(), new ActionGroupProfile());

        var actionGroup = mapper.Map<ActionGroup>(AgvActionGenerator.GeneratePickDropActionGroup()).ConvertFromActionGroupWithFakeNodeType();
        var actionGroupDto = mapper.Map<ActionGroupDto>(actionGroup);

        actionGroupDto.CorrelationId.ShouldBe(actionGroup!.CorrelationId);
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