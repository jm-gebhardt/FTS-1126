using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvErrorProfile = Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles.AgvErrorProfile;
using AgvProfile = Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles.AgvProfile;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Profiles;

[TestClass]
public class AgvProfileTest
{
    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvIsMappedToAgvDto()
    {
        var correlationId = Guid.NewGuid();
        const string agvId = "TestAgv-01";
        const string orderId = "TestOrder-01";
        const string lastNodeId = "WAY3";
        var stateTransitionTimestamp = DateTime.UtcNow;

        var actionGroup = new ActionGroupReadModel(correlationId, orderId, correlationId, stateTransitionTimestamp, null, null, TimeSpan.FromHours(99), ActionGroupState.Created, ActionGroupCreator.TransportOrderWebApi, new List<AgvActionReadModel>(), false, new List<Reason>());

        var agvMessageContent = new Messages.MessageContent.Agv(correlationId, agvId, AgvState.Idle, stateTransitionTimestamp, DateTime.MinValue, true, false, false, true, 50d, false, 15, 80, actionGroup, new List<AgvError>(), lastNodeId, new Messages.MessageContent.GraphNode {Id = lastNodeId, Type = AgvGraph.NodeType.WayPoint, X = 1.0f, Y = 2.0f, Z = 0.0f, Theta = 0.0f});

        var mapper = MappingHelper.GetMapper(new AgvProfile(), new ActionProfile(), new AgvErrorProfile(), new AgvGraphProfile());

        var agvDto = mapper.Map<AgvDto>(agvMessageContent);
        agvDto.AgvId.ShouldBe(agvId);
        agvDto.CorrelationId.ShouldBe(correlationId);
        agvDto.CurrentState.ShouldBe(AgvStateType.Idle);
        agvDto.TimeOfStateTransition.ShouldBe(stateTransitionTimestamp);
        agvDto.TimeOfLockStateChanged.ShouldBe(DateTime.MinValue);
        agvDto.IsOnline.ShouldBeTrue();
        agvDto.IsLocked.ShouldBeFalse();
        agvDto.IsDisturbed.ShouldBeFalse();
        agvDto.IsAutomatic.ShouldBeTrue();
        agvDto.BatteryCharge.ShouldBe(50d);
        agvDto.IsCharging.ShouldBeFalse();
        agvDto.BadBatteryLevel.ShouldBe(15);
        agvDto.GoodBatteryLevel.ShouldBe(80);
        agvDto.Errors.ShouldBeEmpty();
        agvDto.CurrentActionGroup.ShouldNotBeNull();
        agvDto.CurrentActionGroup.OrderId.ShouldBe(orderId);
        agvDto.CurrentActionGroup.AssignedAgv.ShouldBe(correlationId);
        agvDto.CurrentActionGroup.CreatedDate.ShouldBe(stateTransitionTimestamp);
        agvDto.CurrentActionGroup.StartedDate.ShouldBeNull();
        agvDto.CurrentActionGroup.CompletedDate.ShouldBeNull();
        agvDto.CurrentActionGroup.State.ShouldBe(ActionGroupState.Created);
        agvDto.CurrentActionGroup.Creator.ShouldBe(ActionGroupCreator.TransportOrderWebApi);
        agvDto.CurrentActionGroup.IsManual.ShouldBeFalse();
        agvDto.CurrentActionGroup.Actions.ShouldBeEmpty();
        agvDto.LastNode.ShouldNotBeNull();
        agvDto.LastNode.Id.ShouldBe(lastNodeId);
        agvDto.LastNode.Type.ShouldBe(AgvGraph.NodeType.WayPoint);
        agvDto.LastNode.X.ShouldBe(1.0f);
        agvDto.LastNode.Y.ShouldBe(2.0f);
        agvDto.LastNode.Z.ShouldBe(0.0f);
        agvDto.LastNode.Theta.ShouldBe(0.0f);
    }
}