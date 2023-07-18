using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services;

[TestClass]
public class AgvRegistryTest
{
    private const string MapName = "Map#1";

    [TestMethod]
    public void GetAgvStatusAndGraph_WhenCorrelationIdExists()
    {
        var agvInformation = AgvInformationGenerator.GetAgvInformation();
        var agvCorrelationId = agvInformation.AgvStatusReceived!.AgvStatus.CorrelationId;
        var fixture = new AgvRegistryTestFixture()
            .WithAgvInformation(agvInformation);
        var actualAgvInformation = fixture.AgvRegistry.GetAgvInformation(agvCorrelationId);

        VerifyAgvInformation(actualAgvInformation, agvInformation);
    }

    [TestMethod]
    public void GetAgvInformation_WhenCorrelationIdDoesNotExists_ReturnsNull()
    {
        var correlationId = Guid.NewGuid();
        var fixture = new AgvRegistryTestFixture();
        var actualAgvInformation = fixture.AgvRegistry.GetAgvInformation(correlationId);
        actualAgvInformation.ShouldBeNull();
    }

    [TestMethod]
    public void AddOrUpdate_WhenCorrelationIdExists_SuccessfullyUpdateEntry()
    {
        var agvInformation = AgvInformationGenerator.GetAgvInformation();
        var agvCorrelationId = agvInformation.AgvStatusReceived!.AgvStatus.CorrelationId;
        var fixture = new AgvRegistryTestFixture()
            .WithAgvInformation(agvInformation);
        var actualAgvInformation = fixture.AgvRegistry.GetAgvInformation(agvCorrelationId);

        VerifyAgvInformation(actualAgvInformation, agvInformation);
        var agvStatus = new AgvStatus(agvCorrelationId, null, false, true, 99, false, false, "CRG5");
        var agvGraph = new AgvGraph(agvCorrelationId, Array.Empty<GraphNode>(), Array.Empty<GraphEdge>(), MapName.ToGuid(), MapName);
        var agvAction = new AgvAction("Action#123".ToGuid(), ActionType.Move, "Pick88", Guid.Empty);
        agvInformation = AgvInformationGenerator.GetAgvInformation(agvStatus, agvGraph, agvAction);

        fixture.AgvRegistry.AddOrUpdate(agvCorrelationId, agvInformation);
        actualAgvInformation = fixture.AgvRegistry.GetAgvInformation(agvCorrelationId);
        VerifyAgvInformation(actualAgvInformation, agvInformation);
    }

    [TestMethod]
    public void AddOrUpdate_WhenCorrelationIdDoestNotExists_SuccessfullyAddEntry()
    {
        var agvInformation = AgvInformationGenerator.GetAgvInformation();
        var agvCorrelationId = agvInformation.AgvStatusReceived!.AgvStatus.CorrelationId;
        var fixture = new AgvRegistryTestFixture();

        fixture.AgvRegistry.AddOrUpdate(agvCorrelationId, agvInformation);
        var actualAgvInformation = fixture.AgvRegistry.GetAgvInformation(agvCorrelationId);
        actualAgvInformation.ShouldNotBeNull();
        VerifyAgvInformation(actualAgvInformation, agvInformation);
    }

    [TestMethod]
    public void AddOrUpdateAgvStatus_WhenCorrelationIdExist_SuccessfullyUpdateEntry()
    {
        var agvInformation = AgvInformationGenerator.GetAgvInformation();
        var agvCorrelationId = agvInformation.AgvStatusReceived!.AgvStatus.CorrelationId;
        var fixture = new AgvRegistryTestFixture()
            .WithAgvInformation(agvInformation);
        var actualAgvInformation = fixture.AgvRegistry.GetAgvInformation(agvCorrelationId);

        VerifyAgvInformation(actualAgvInformation, agvInformation);
        var agvStatus = new AgvStatus(agvCorrelationId, null, false, true, 89, false, false, "CRG6");
        var agvStatusReceived = AgvInformationGenerator.GetAgvStatusReceived(agvStatus);

        fixture.AgvRegistry.AddOrUpdateAgvStatus(agvCorrelationId, agvStatusReceived!);
        actualAgvInformation = fixture.AgvRegistry.GetAgvInformation(agvCorrelationId);
        VerifyAgvInformation(actualAgvInformation, agvInformation);
    }

    [TestMethod]
    public void AddOrUpdateAgvStatus_WhenCorrelationIdDoesNotExist_SuccessfullyAddEntry()
    {
        var agvInformation = AgvInformationGenerator.GetAgvInformation();
        var agvCorrelationId = agvInformation.AgvStatusReceived!.AgvStatus.CorrelationId;
        var fixture = new AgvRegistryTestFixture();
        var actualAgvInformation = fixture.AgvRegistry.GetAgvInformation(agvCorrelationId);
        actualAgvInformation.ShouldBeNull();

        var agvStatus = new AgvStatus(agvCorrelationId, null, false, true, 89, false, false, "CRG6");
        var agvStatusReceived = AgvInformationGenerator.GetAgvStatusReceived(agvStatus);

        fixture.AgvRegistry.AddOrUpdateAgvStatus(agvCorrelationId, agvStatusReceived!);
        actualAgvInformation = fixture.AgvRegistry.GetAgvInformation(agvCorrelationId);
        actualAgvInformation.ShouldNotBeNull();
        actualAgvInformation.AgvStatusReceived.ShouldBe(agvStatusReceived);
    }

    [TestMethod]
    public void AddOrUpdateAgvGraph_WhenCorrelationIdExists_SuccessfullyUpdateEntry()
    {
        var agvInformation = AgvInformationGenerator.GetAgvInformation();
        var agvCorrelationId = agvInformation.AgvStatusReceived!.AgvStatus.CorrelationId;
        var fixture = new AgvRegistryTestFixture()
            .WithAgvInformation(agvInformation);
        var actualAgvInformation = fixture.AgvRegistry.GetAgvInformation(agvCorrelationId);

        VerifyAgvInformation(actualAgvInformation, agvInformation);
        var testAgvGraph = new AgvGraph(agvCorrelationId, Array.Empty<GraphNode>(), Array.Empty<GraphEdge>(), MapName.ToGuid(), MapName);

        fixture.AgvRegistry.AddOrUpdateAgvGraph(agvCorrelationId, testAgvGraph);
        actualAgvInformation = fixture.AgvRegistry.GetAgvInformation(agvCorrelationId);
        VerifyAgvInformation(actualAgvInformation, agvInformation);
    }

    [TestMethod]
    public void AddOrUpdateAgvGraph_WhenCorrelationIdDoesNotExist_SuccessfullyAddEntry()
    {
        var agvInformation = AgvInformationGenerator.GetAgvInformation();
        var agvCorrelationId = agvInformation.AgvStatusReceived!.AgvStatus.CorrelationId;
        var agvGraph = agvInformation.AgvGraph!;
        var fixture = new AgvRegistryTestFixture();
        var actualAgvInformation = fixture.AgvRegistry.GetAgvInformation(agvCorrelationId);
        actualAgvInformation.ShouldBeNull();

        fixture.AgvRegistry.AddOrUpdateAgvGraph(agvCorrelationId, agvGraph);
        actualAgvInformation = fixture.AgvRegistry.GetAgvInformation(agvCorrelationId);
        actualAgvInformation.ShouldNotBeNull();
        actualAgvInformation.AgvGraph.ShouldBe(agvGraph);
    }

    [TestMethod]
    public void AddOrUpdateAgvAction_WhenCorrelationIdExist_SuccessfullyUpdateEntry()
    {
        var agvInformation = AgvInformationGenerator.GetAgvInformation();
        var agvCorrelationId = agvInformation.AgvStatusReceived!.AgvStatus.CorrelationId;
        var fixture = new AgvRegistryTestFixture()
            .WithAgvInformation(agvInformation);
        var actualAgvInformation = fixture.AgvRegistry.GetAgvInformation(agvCorrelationId);

        VerifyAgvInformation(actualAgvInformation, agvInformation);
        var testAgvAction = new AgvAction(Guid.NewGuid(), ActionType.Drop, "Charge00", Guid.NewGuid());

        fixture.AgvRegistry.AddOrUpdateAgvAction(agvCorrelationId, testAgvAction);
        actualAgvInformation = fixture.AgvRegistry.GetAgvInformation(agvCorrelationId);
        VerifyAgvInformation(actualAgvInformation, agvInformation);
    }

    [TestMethod]
    public void AddOrUpdateAgvAction_WhenCorrelationIdDoesNotExist_SuccessfullyAddEntry()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new AgvRegistryTestFixture();
        var actualAgvInformation = fixture.AgvRegistry.GetAgvInformation(agvCorrelationId);
        actualAgvInformation.ShouldBeNull();

        var testAgvAction = new AgvAction(Guid.NewGuid(), ActionType.Drop, "Charge00", Guid.NewGuid());

        fixture.AgvRegistry.AddOrUpdateAgvAction(agvCorrelationId, testAgvAction);
        actualAgvInformation = fixture.AgvRegistry.GetAgvInformation(agvCorrelationId);
        actualAgvInformation.ShouldNotBeNull();
        actualAgvInformation.AgvAction.ShouldBe(testAgvAction);
    }

    private static void VerifyAgvInformation(AgvInformation? actualAgvInformation, AgvInformation expectedAgvInformation)
    {
        actualAgvInformation.ShouldNotBeNull();

        actualAgvInformation.AgvStatusReceived!.ShouldBe(expectedAgvInformation.AgvStatusReceived);
        actualAgvInformation.AgvAction!.ShouldBeEquivalentTo(expectedAgvInformation.AgvAction);

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

            for (int i = 0; i < actualAgvInformation.AgvGraph!.Edges.Count; i++)
            {
                var actualEdge = actualAgvInformation.AgvGraph.Edges.ElementAt(i);
                var expectedEdge = expectedAgvInformation.AgvGraph.Edges.ElementAt(i);
                actualEdge.ShouldSatisfyAllConditions(
                    p => p.FromNodeId.ShouldBe(expectedEdge.FromNodeId),
                    p => p.Metric.ShouldBe(expectedEdge.Metric),
                    p => p.ToNodeId.ShouldBe(expectedEdge.ToNodeId));
            }

            for (int i = 0; i < actualAgvInformation.AgvGraph.Nodes.Count; i++)
            {
                var expectedNode = expectedAgvInformation.AgvGraph.Nodes.ElementAt(i);
                actualAgvInformation.AgvGraph.Nodes.ElementAt(i).ShouldSatisfyAllConditions(
                    p => p.Id.ShouldBe(expectedNode.Id),
                    p => p.Theta.ShouldBe(expectedNode.Theta),
                    p => p.Type.ShouldBe(expectedNode.Type),
                    p => p.X.ShouldBe(expectedNode.X),
                    p => p.Y.ShouldBe(expectedNode.Y),
                    p => p.Z.ShouldBe(expectedNode.Z));
            }
        }
    }
}