using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using ActionProfile = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles.ActionProfile;
using SortDirection = Shouldly.SortDirection;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class ActionProfileTest
{
    [TestMethod]
    public void Map_ActionGroupToActionGroupDto_MapsActionsInOrderBySequence()
    {
        var mapper = MappingHelper.GetMapper(new AgvGraphProfile(), new AgvProfile(), new TransportOrder.WebApi.Profiles.ActionProfile(), new AgvErrorProfile(), new ActionProfile());

        List<AgvAction> actions = AgvActionGenerator.GeneratePickDropActionGroup().Actions.OrderByDescending(x => x.Sequence).ToList();
        var actionGroup = AgvActionGenerator.GenerateActionGroup(actions);
        var actionGroupDto = mapper.Map<ActionGroupDto>(mapper.Map<ActionGroup>(actionGroup).ConvertFromActionGroupWithFakeNodeType());
        actionGroupDto.Actions.Select(a => a.Sequence).ShouldBeInOrder(SortDirection.Ascending);
    }

    [TestMethod]
    public void Map_ActionToActionDto_MapsAllPropertiesCorrectly()
    {
        var mapper = MappingHelper.GetMapper(new TransportOrder.WebApi.Profiles.ActionProfile());

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
        var mapper = MappingHelper.GetMapper(new TransportOrder.WebApi.Profiles.ActionProfile(), new ActionProfile());

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

    [TestMethod]
    public void Map_DurationStatistics_MapsAllPropertiesCorrectly()
    {
        var mapper = MappingHelper.GetMapper(new TransportOrder.WebApi.Profiles.ActionProfile());
        var durationStatistics = new DurationStatistics(ActionType.Pick, 12, 1, 42);

        var dto = mapper.Map<ActionDurationStatisticsDto>(durationStatistics);
       
        dto.ActionType.ShouldBe(durationStatistics.ActionType);
        dto.MaxDuration.ShouldBe(durationStatistics.MaxDuration);
        dto.MinDuration.ShouldBe(durationStatistics.MinDuration);
        dto.MedianDuration.ShouldBe(durationStatistics.MedianDuration);
    }
}