using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Cargo = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.Cargo;
using PolygonPoint = Gebhardt.Acc.TransportOrder.Domain.Models.Polygon.PolygonPoint;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

[TestClass]
public class TriggerEventHandlerTest
{
    [TestMethod]
    public async Task Consume_TriggerConfigured_ValidData_PublishesCreateTrigger()
    {
        var fixture = new TriggerEventHandlerTestFixture();

        await fixture.TestHarness.Bus.Publish(new TriggerConfigured(new TriggerProperties("urn", "name", "description", "version"), []));

        await fixture.ShouldHaveConsumedAsync<TriggerConfigured>();
        await fixture.ShouldHavePublishedAsync<CreateTrigger>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupStarted_ValidDataAreaTrigger_PublishesAgvStartedActionGroupOnTrigger()
    {
        var agv = AgvGenerator.GenerateIdleAgv();

        var actionGroup = new ActionGroup(Guid.NewGuid(), "order") { AssignedAgv = agv.CorrelationId };

        var cargo = new Cargo(Guid.NewGuid(), agv.CorrelationId, "load", null, new Point3D(1f, 1f, 1f, 1f), new Cuboid(1f, 1f, null), null, null);
        var agvData = new AgvData(agv.CorrelationId, "", "", "", "", 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1, []);
        var position = new AgvPositionStatus(agv.CorrelationId, agv.SerialNumber, new AgvPosition(true, 1, 1, 0, 0f));
        var location = new Location([new PolygonPoint(0, 0, 0)], Guid.NewGuid(), Guid.NewGuid(), [], []);

        var fixture = new TriggerEventHandlerTestFixture()
            .WithAgv(agv)
            .WithAgvData(agv.CorrelationId, agvData)
            .WithAgvErrors(agv.CorrelationId, [])
            .WithCargo(agv.CorrelationId, cargo)
            .WithPosition(agv.CorrelationId, position)
            .WithPolygonTriggerLocations([location]);

        await fixture.TestHarness.Bus.Publish(new ActionGroupStarted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow));

        await fixture.ShouldHaveConsumedAsync<ActionGroupStarted>();
        await fixture.ShouldHavePublishedAsync<AgvStartedActionGroupOnTrigger>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupStarted_ValidDataNodeTrigger_PublishesAgvStartedActionGroupOnTrigger()
    {
        const string nodeName = "lastNode";
        var node = new GraphNode(nodeName.ToGuid(), nodeName, 1, 1, 0, AgvGraph.NodeType.Charge, null, null, null);
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: nodeName);

        var actionGroup = new ActionGroup(Guid.NewGuid(), "order") { AssignedAgv = agv.CorrelationId };

        var cargo = new Cargo(Guid.NewGuid(), agv.CorrelationId, "load", null, new Point3D(1f, 1f, 1f, 1f), new Cuboid(1f, 1f, null), null, null);
        var agvData = new AgvData(agv.CorrelationId, "", "", "", "", 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1, []);
        var position = new AgvPositionStatus(agv.CorrelationId, agv.SerialNumber, new AgvPosition(true, 1, 1, 0, 0f));
        var location = new Location([new PolygonPoint(0, 0, 0)], Guid.NewGuid(), Guid.NewGuid(), [], []);

        var fixture = new TriggerEventHandlerTestFixture()
            .WithAgv(agv)
            .WithAgvData(agv.CorrelationId, agvData)
            .WithAgvErrors(agv.CorrelationId, [])
            .WithCargo(agv.CorrelationId, cargo)
            .WithPosition(agv.CorrelationId, position)
            .WithNode(node)
            .WithNodeTriggerLocations([location], nodeName)
            .WithPolygonTriggerLocations([]);

        await fixture.TestHarness.Bus.Publish(new ActionGroupStarted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow));

        await fixture.ShouldHaveConsumedAsync<ActionGroupStarted>();
        await fixture.ShouldHavePublishedAsync<AgvStartedActionGroupOnTrigger>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupStarted_NoAgvData_DoesNotPublishAgvStartedActionGroupOnTrigger()
    {
        var agv = AgvGenerator.GenerateIdleAgv();

        var actionGroup = new ActionGroup(Guid.NewGuid(), "order") { AssignedAgv = agv.CorrelationId };

        var cargo = new Cargo(Guid.NewGuid(), agv.CorrelationId, "load", null, new Point3D(1f, 1f, 1f, 1f), new Cuboid(1f, 1f, null), null, null);

        var position = new AgvPositionStatus(agv.CorrelationId, agv.SerialNumber, new AgvPosition(true, 1, 1, 0, 0f));
        var location = new Location([new PolygonPoint(0, 0, 0)], Guid.NewGuid(), Guid.NewGuid(), [], []);

        var fixture = new TriggerEventHandlerTestFixture()
            .WithAgv(agv)
            .WithAgvErrors(agv.CorrelationId, [])
            .WithCargo(agv.CorrelationId, cargo)
            .WithPosition(agv.CorrelationId, position)
            .WithAgvData(agv.CorrelationId, null)
            .WithPolygonTriggerLocations([location]);

        await fixture.TestHarness.Bus.Publish(new ActionGroupStarted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow));

        await fixture.ShouldHaveConsumedAsync<ActionGroupStarted>();
        await fixture.ShouldNotHavePublishedAsync<AgvStartedActionGroupOnTrigger>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupStarted_NoTriggerLocations_DoesNotPublishAgvStartedActionGroupOnTrigger()
    {
        var agv = AgvGenerator.GenerateIdleAgv();

        var actionGroup = new ActionGroup(Guid.NewGuid(), "order") { AssignedAgv = agv.CorrelationId };

        var cargo = new Cargo(Guid.NewGuid(), agv.CorrelationId, "load", null, new Point3D(1f, 1f, 1f, 1f), new Cuboid(1f, 1f, null), null, null);
        var agvData = new AgvData(agv.CorrelationId, "", "", "", "", 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1, []);
        var position = new AgvPositionStatus(agv.CorrelationId, agv.SerialNumber, new AgvPosition(true, 1, 1, 0, 0f));

        var fixture = new TriggerEventHandlerTestFixture()
            .WithAgv(agv)
            .WithAgvData(agv.CorrelationId, agvData)
            .WithAgvErrors(agv.CorrelationId, [])
            .WithCargo(agv.CorrelationId, cargo)
            .WithPosition(agv.CorrelationId, position)
            .WithPolygonTriggerLocations([]);

        await fixture.TestHarness.Bus.Publish(new ActionGroupStarted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow));

        await fixture.ShouldHaveConsumedAsync<ActionGroupStarted>();
        await fixture.ShouldNotHavePublishedAsync<AgvStartedActionGroupOnTrigger>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupStarted_NoAgv_oesNotPublishAgvStartedActionGroupOnTrigger()
    {
        var agv = AgvGenerator.GenerateIdleAgv();

        var actionGroup = new ActionGroup(Guid.NewGuid(), "order") { AssignedAgv = agv.CorrelationId };

        var cargo = new Cargo(Guid.NewGuid(), agv.CorrelationId, "load", null, new Point3D(1f, 1f, 1f, 1f), new Cuboid(1f, 1f, null), null, null);
        var agvData = new AgvData(agv.CorrelationId, "", "", "", "", 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1, []);
        var position = new AgvPositionStatus(agv.CorrelationId, agv.SerialNumber, new AgvPosition(true, 1, 1, 0, 0f));
        var location = new Location([new PolygonPoint(0, 0, 0)], Guid.NewGuid(), Guid.NewGuid(), [], []);

        var fixture = new TriggerEventHandlerTestFixture()
            .WithAgvData(agv.CorrelationId, agvData)
            .WithAgvErrors(agv.CorrelationId, [])
            .WithCargo(agv.CorrelationId, cargo)
            .WithPosition(agv.CorrelationId, position)
            .WithPolygonTriggerLocations([location]);

        await fixture.TestHarness.Bus.Publish(new ActionGroupStarted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow));

        await fixture.ShouldHaveConsumedAsync<ActionGroupStarted>();
        await fixture.ShouldNotHavePublishedAsync<AgvStartedActionGroupOnTrigger>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupStarted_NoAssignedAgv_DoesNotPublishAgvStartedActionGroupOnTrigger()
    {
        var agv = AgvGenerator.GenerateIdleAgv();

        var actionGroup = new ActionGroup(Guid.NewGuid(), "order");

        var cargo = new Cargo(Guid.NewGuid(), agv.CorrelationId, "load", null, new Point3D(1f, 1f, 1f, 1f), new Cuboid(1f, 1f, null), null, null);
        var agvData = new AgvData(agv.CorrelationId, "", "", "", "", 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1, []);
        var position = new AgvPositionStatus(agv.CorrelationId, agv.SerialNumber, new AgvPosition(true, 1, 1, 0, 0f));
        var location = new Location([new PolygonPoint(0, 0, 0)], Guid.NewGuid(), Guid.NewGuid(), [], []);

        var fixture = new TriggerEventHandlerTestFixture()
            .WithAgv(agv)
            .WithAgvData(agv.CorrelationId, agvData)
            .WithAgvErrors(agv.CorrelationId, [])
            .WithCargo(agv.CorrelationId, cargo)
            .WithPosition(agv.CorrelationId, position)
            .WithPolygonTriggerLocations([location]);

        await fixture.TestHarness.Bus.Publish(new ActionGroupStarted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow));

        await fixture.ShouldHaveConsumedAsync<ActionGroupStarted>();
        await fixture.ShouldNotHavePublishedAsync<AgvStartedActionGroupOnTrigger>();
    }
}