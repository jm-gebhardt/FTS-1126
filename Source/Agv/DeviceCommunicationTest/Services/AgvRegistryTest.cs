using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services;

[TestClass]
public class AgvRegistryTest
{
    private const string MapName = "Map#1";

    [TestMethod]
    [DataRow("AGV001", "ORDER001", "Idle", "SRC1", false, 0)]
    [DataRow("AGV002", "ORDER002", "Busy", "SRC2", false, 25)]
    [DataRow("AGV003", "ORDER003", "Busy", "CRG2", true, 50)]
    [DataRow("AGV004", "ORDER004", "Ready", "CRG1", false, 75)]
    [DataRow("AGV005", "ORDER005", "Ready", "CRG3", false, 100)]
    public void GetAgvStatusAndGraph_WhenCorrelationIdExists(string agvId, string expectedOrderId, string expectedState, string expectedLastNodeId, bool expectedIsCharging, int expectedBatteryCharge)
    {
        var correlationId = agvId.ToGuid();
        var actionCorrelationId = $"Action#{agvId}".ToGuid();
        var testFixture = new AgvRegistryTestFixture();
        var agvRegistry = testFixture.AgvRegistry;
        var actualAgvInformation = agvRegistry.GetAgvInformation(agvId.ToGuid());

        var expectedAgvStatus = AgvRegistryTestFixture.GetAgvStatusReceived(new AgvStatus(correlationId, expectedOrderId, expectedState == "Busy", true, expectedBatteryCharge, expectedIsCharging, false, expectedLastNodeId));
        var expectedAgvGraph = new AgvGraph(correlationId, actualAgvInformation!.AgvGraph!.Nodes, actualAgvInformation.AgvGraph.Edges, MapName.ToGuid(), MapName);
        var expectedAgvAction = new AgvAction(actionCorrelationId, agvId, ActionType.Pick, expectedOrderId, expectedOrderId.ToGuid(), "Drop5");
        var expectedAgvInformation = new AgvInformation { AgvStatusReceived = expectedAgvStatus, AgvGraph = expectedAgvGraph, AgvAction = expectedAgvAction };

        actualAgvInformation.ShouldNotBeNull();
        expectedAgvInformation.ShouldNotBeNull();
        TestCompareAgvStatusAndGraph(actualAgvInformation, expectedAgvInformation);
    }

    [TestMethod]
    public void GetAgvInformation_WhenCorrelationIdDoesNotExists_ReturnsNull()
    {
        var correlationId = Guid.NewGuid();
        var agvRegistry = new AgvRegistryTestFixture().AgvRegistry;
        var actualAgvInformation = agvRegistry.GetAgvInformation(correlationId);
        actualAgvInformation.ShouldBeNull();
    }

    [TestMethod]
    [DataRow("AGV001")]
    [DataRow("AGV002")]
    [DataRow("AGV003")]
    [DataRow("AGV004")]
    public void AddOrUpdate_WhenCorrelationIdExists_SuccessfullyUpdateEntry(string agvId)
    {
        var correlationId = agvId.ToGuid();
        var testFixture = new AgvRegistryTestFixture();
        var agvRegistry = testFixture.AgvRegistry;
        agvRegistry.GetAgvInformation(correlationId).ShouldNotBeNull();

        string orderId = $"Order#{agvId}";
        var agvStatus = AgvRegistryTestFixture.GetAgvStatusReceived(new AgvStatus(correlationId, null, false, true, 99, false, false, "CRG5"));
        var agvGraph = new AgvGraph(correlationId, Array.Empty<AgvGraph.Node>(), Array.Empty<AgvGraph.Edge>(), MapName.ToGuid(), MapName);
        var agvAction = new AgvAction($"Action#{agvId}".ToGuid(), agvId, ActionType.Move, orderId, orderId.ToGuid(), "Pick88");
        var testAgvInformation = new AgvInformation { AgvStatusReceived = agvStatus, AgvGraph = agvGraph, AgvAction = agvAction };
        agvRegistry.AddOrUpdate(correlationId, testAgvInformation);

        var actualAgvInformation = agvRegistry.GetAgvInformation(correlationId);
        actualAgvInformation.ShouldNotBeNull();
        TestCompareAgvStatusAndGraph(actualAgvInformation, testAgvInformation);
    }

    [TestMethod]
    public void AddOrUpdate_WhenCorrelationIdDoestNotExists_SuccessfullyAddEntry()
    {
        var correlationId = Guid.NewGuid();
        var testFixture = new AgvRegistryTestFixture();
        var agvRegistry = testFixture.AgvRegistry;
        agvRegistry.GetAgvInformation(correlationId).ShouldBeNull();

        string orderId = $"OrderFor{correlationId}";
        var agvStatus = AgvRegistryTestFixture.GetAgvStatusReceived(new AgvStatus(correlationId, null, false, true, 99, false, false, "CRG5"));
        var agvGraph = new AgvGraph(correlationId, Array.Empty<AgvGraph.Node>(), Array.Empty<AgvGraph.Edge>(), MapName.ToGuid(), MapName);
        var agvAction = new AgvAction($"Action#{correlationId}".ToGuid(), correlationId.ToString(), ActionType.Move, orderId, orderId.ToGuid(), "Pick88");
        var testAgvInformation = new AgvInformation { AgvStatusReceived = agvStatus, AgvGraph = agvGraph, AgvAction = agvAction };
        agvRegistry.AddOrUpdate(correlationId, testAgvInformation);

        var actualAgvInformation = agvRegistry.GetAgvInformation(correlationId);
        actualAgvInformation.ShouldNotBeNull();
        TestCompareAgvStatusAndGraph(actualAgvInformation, testAgvInformation);
    }

    [TestMethod]
    [DataRow("AGV001")]
    [DataRow("AGV002")]
    [DataRow("AGV003")]
    [DataRow("AGV004")]
    public void AddOrUpdateAgvStatus_WhenCorrelationIdExist_SuccessfullyUpdateEntry(string agvId)
    {
        var correlationId = agvId.ToGuid();
        var testFixture = new AgvRegistryTestFixture();
        var agvRegistry = testFixture.AgvRegistry;
        var agvInformation = agvRegistry.GetAgvInformation(correlationId);
        agvInformation.ShouldNotBeNull();

        var agvStatus = new AgvStatus(correlationId, null, false, true, 89, false, false, "CRG6");
        var agvStatusReceived = AgvRegistryTestFixture.GetAgvStatusReceived(agvStatus);
        var expectedAgvInformation = agvInformation with { AgvStatusReceived = agvStatusReceived };
        agvRegistry.AddOrUpdateAgvStatus(correlationId, agvStatusReceived!);

        var actualAgvInformation = agvRegistry.GetAgvInformation(correlationId);
        actualAgvInformation.ShouldNotBeNull();
        TestCompareAgvStatusAndGraph(actualAgvInformation, expectedAgvInformation);
    }

    [TestMethod]
    public void AddOrUpdateAgvStatus_WhenCorrelationIdDoesNotExist_SuccessfullyAddEntry()
    {
        var correlationId = Guid.NewGuid();
        var testFixture = new AgvRegistryTestFixture();
        var agvRegistry = testFixture.AgvRegistry;
        var agvInformation = agvRegistry.GetAgvInformation(correlationId);
        agvInformation.ShouldBeNull();

        var agvStatus = new AgvStatus(correlationId, null, false, true, 89, false, false, "CRG6");
        var agvStatusReceived = AgvRegistryTestFixture.GetAgvStatusReceived(agvStatus);
        var expectedAgvInformation = new AgvInformation { AgvStatusReceived = agvStatusReceived, AgvGraph = agvInformation?.AgvGraph, AgvAction = agvInformation?.AgvAction };
        agvRegistry.AddOrUpdateAgvStatus(correlationId, agvStatusReceived!);

        var actualAgvInformation = agvRegistry.GetAgvInformation(correlationId);
        actualAgvInformation.ShouldNotBeNull();
        TestCompareAgvStatusAndGraph(actualAgvInformation, expectedAgvInformation);
    }

    [TestMethod]
    [DataRow("AGV001")]
    [DataRow("AGV002")]
    [DataRow("AGV003")]
    [DataRow("AGV004")]
    public void AddOrUpdateAgvGraph_WhenCorrelationIdExists_SuccessfullyUpdateEntry(string agvId)
    {
        var correlationId = agvId.ToGuid();
        var agvRegistry = new AgvRegistryTestFixture().AgvRegistry;
        var agvInformation = agvRegistry.GetAgvInformation(correlationId);
        agvInformation.ShouldNotBeNull();

        var testAgvGraph = new AgvGraph(correlationId, Array.Empty<AgvGraph.Node>(), Array.Empty<AgvGraph.Edge>(), MapName.ToGuid(), MapName);
        var expectedAgvInformation = agvInformation with { AgvGraph = testAgvGraph };
        agvRegistry.AddOrUpdateAgvGraph(correlationId, testAgvGraph);

        var actualAgvInformation = agvRegistry.GetAgvInformation(correlationId);
        actualAgvInformation.ShouldNotBeNull();
        TestCompareAgvStatusAndGraph(actualAgvInformation, expectedAgvInformation);
    }

    [TestMethod]
    public void AddOrUpdateAgvGraph_WhenCorrelationIdDoesNotExist_SuccessfullyAddEntry()
    {
        var correlationId = Guid.NewGuid();
        var agvRegistry = new AgvRegistryTestFixture().AgvRegistry;
        var agvInformation = agvRegistry.GetAgvInformation(correlationId);
        agvInformation.ShouldBeNull();

        var testAgvGraph = new AgvGraph(correlationId, Array.Empty<AgvGraph.Node>(), Array.Empty<AgvGraph.Edge>(), MapName.ToGuid(), MapName);
        var expectedAgvInformation = new AgvInformation { AgvStatusReceived = agvInformation?.AgvStatusReceived, AgvGraph = testAgvGraph, AgvAction = agvInformation?.AgvAction };
        agvRegistry.AddOrUpdateAgvGraph(correlationId, testAgvGraph);

        var actualAgvInformation = agvRegistry.GetAgvInformation(correlationId);
        actualAgvInformation.ShouldNotBeNull();
        TestCompareAgvStatusAndGraph(actualAgvInformation, expectedAgvInformation);
    }

    [TestMethod]
    [DataRow("AGV001")]
    [DataRow("AGV002")]
    [DataRow("AGV003")]
    [DataRow("AGV004")]
    public void AddOrUpdateAgvAction_WhenCorrelationIdExist_SuccessfullyUpdateEntry(string agvId)
    {
        var correlationId = agvId.ToGuid();
        var actionCorrelationId = Guid.NewGuid();
        var agvRegistry = new AgvRegistryTestFixture().AgvRegistry;
        var agvInformation = agvRegistry.GetAgvInformation(correlationId);
        agvInformation.ShouldNotBeNull();

        string orderId = $"TestOrder#{agvId}";
        var testAgvAction = new AgvAction(actionCorrelationId, agvId, ActionType.Drop, orderId, orderId.ToGuid(), "Charge00");
        var expectedAgvInformation = agvInformation with { AgvAction = testAgvAction };
        agvRegistry.AddOrUpdateAgvAction(correlationId, testAgvAction);

        var actualAgvInformation = agvRegistry.GetAgvInformation(correlationId);
        actualAgvInformation.ShouldNotBeNull();
        TestCompareAgvStatusAndGraph(actualAgvInformation, expectedAgvInformation);
    }

    [TestMethod]
    public void AddOrUpdateAgvAction_WhenCorrelationIdDoesNotExist_SuccessfullyAddEntry()
    {
        var correlationId = Guid.NewGuid();
        var actionCorrelationId = Guid.NewGuid();
        var agvRegistry = new AgvRegistryTestFixture().AgvRegistry;
        var agvInformation = agvRegistry.GetAgvInformation(correlationId);
        agvInformation.ShouldBeNull();

        string orderId = $"TestOrder#{correlationId}";
        var testAgvAction = new AgvAction(actionCorrelationId, correlationId.ToString(), ActionType.Drop, orderId, orderId.ToGuid(), "Charge00");
        var expectedAgvInformation = new AgvInformation { AgvStatusReceived = agvInformation?.AgvStatusReceived, AgvGraph = agvInformation?.AgvGraph, AgvAction = testAgvAction };
        agvRegistry.AddOrUpdateAgvAction(correlationId, testAgvAction);

        var actualAgvInformation = agvRegistry.GetAgvInformation(correlationId);
        actualAgvInformation.ShouldNotBeNull();
        TestCompareAgvStatusAndGraph(actualAgvInformation, expectedAgvInformation);
    }

    private static void TestCompareAgvStatusAndGraph(AgvInformation actualAgvInformation, AgvInformation expectedAgvInformation)
    {
        actualAgvInformation.AgvStatusReceived!.ShouldBe(expectedAgvInformation.AgvStatusReceived);
        actualAgvInformation.AgvAction!.ShouldBe(expectedAgvInformation.AgvAction);

        if (expectedAgvInformation.AgvGraph == null)
        {
            actualAgvInformation.AgvGraph!.ShouldBe(expectedAgvInformation.AgvGraph);
        }
        else
        {
            actualAgvInformation.AgvGraph.ShouldSatisfyAllConditions(
                p => p?.CorrelationId.ShouldBe(expectedAgvInformation.AgvGraph.CorrelationId),
                p => p?.RelatedMapId.ShouldBe(expectedAgvInformation.AgvGraph.RelatedMapId),
                p => p?.RelatedMapName.ShouldBe(expectedAgvInformation.AgvGraph.RelatedMapName),
                p => p?.Usability.ShouldBe(expectedAgvInformation.AgvGraph.Usability)
            );

            for (int i = 0; i < actualAgvInformation.AgvGraph!.Edges.Length; i++)
            {
                actualAgvInformation.AgvGraph.Edges[i].ShouldSatisfyAllConditions(
                    p => p.From.ShouldBe(expectedAgvInformation.AgvGraph.Edges[i].From),
                    p => p.IsBlocked.ShouldBe(expectedAgvInformation.AgvGraph.Edges[i].IsBlocked),
                    p => p.Metric.ShouldBe(expectedAgvInformation.AgvGraph.Edges[i].Metric),
                    p => p.To.ShouldBe(expectedAgvInformation.AgvGraph.Edges[i].To));
            }

            for (int i = 0; i < actualAgvInformation.AgvGraph.Nodes.Length; i++)
            {
                actualAgvInformation.AgvGraph.Nodes[i].ShouldSatisfyAllConditions(
                    p => p.Enabled.ShouldBe(expectedAgvInformation.AgvGraph.Nodes[i].Enabled),
                    p => p.Id.ShouldBe(expectedAgvInformation.AgvGraph.Nodes[i].Id),
                    p => p.State.ShouldBe(expectedAgvInformation.AgvGraph.Nodes[i].State),
                    p => p.Theta.ShouldBe(expectedAgvInformation.AgvGraph.Nodes[i].Theta),
                    p => p.Type.ShouldBe(expectedAgvInformation.AgvGraph.Nodes[i].Type),
                    p => p.X.ShouldBe(expectedAgvInformation.AgvGraph.Nodes[i].X),
                    p => p.Y.ShouldBe(expectedAgvInformation.AgvGraph.Nodes[i].Y),
                    p => p.Z.ShouldBe(expectedAgvInformation.AgvGraph.Nodes[i].Z));
            }
        }
    }
}