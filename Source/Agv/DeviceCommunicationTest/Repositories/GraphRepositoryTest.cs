using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Repositories;

[TestClass]
public class GraphRepositoryTest
{
    [TestMethod]
    public async Task GetMqttNode_NodeExist_ReturnValue()
    {
        const string graphNodeId = "WP-001";
        var fixture = new GraphRepositoryTestFixture();
        await fixture.FillGraphRepository();
        var actualMqttNode = await fixture.GraphRepository.GetMqttNode(graphNodeId);

        actualMqttNode?.NodeId.ShouldBe(graphNodeId);
    }

    [TestMethod]
    public async Task GetMqttNode_UnknownNode_ReturnValue()
    {
        const string graphNodeId = "UN-001";
        var fixture = new GraphRepositoryTestFixture();
        await fixture.FillGraphRepository();
        var actualMqttNode = await fixture.GraphRepository.GetMqttNode(graphNodeId);

        actualMqttNode.ShouldBeNull();
    }

    [TestMethod]
    public async Task AddMqttNode_ValidData_ReturnValue()
    {
        var newMqttNode = new MqttNode { NodeId = "NEW-001", X = -2f, Y = 3f, Z = 0f };
        var fixture = new GraphRepositoryTestFixture();
        await fixture.FillGraphRepository();

        await fixture.GraphRepository.Add(newMqttNode.NodeId, newMqttNode);

        var actualMqttNode = await fixture.GraphRepository.GetMqttNode(newMqttNode.NodeId);

        actualMqttNode?.NodeId.ShouldBe(newMqttNode.NodeId);
    }

    [TestMethod]
    public async Task GetMqttNodes_ValidData_ReturnNodes()
    {
        const string graphNodeId = "WP-001";
        var fixture = new GraphRepositoryTestFixture();
        await fixture.FillGraphRepository();
        ICollection<MqttNode> actualMqttNodes = await fixture.GraphRepository.GetMqttNodes([graphNodeId]);

        actualMqttNodes.Count.ShouldBe(1);
        actualMqttNodes.Single().NodeId.ShouldBe(graphNodeId);
    }

    [TestMethod]
    public async Task GetMqttEdge_EdgeExist_ReturnValue()
    {
        const string mqttEdgeId = "E-1";

        var fixture = new GraphRepositoryTestFixture();
        await fixture.FillGraphRepository();

        var actualMqttEdge = await fixture.GraphRepository.GetMqttEdge(Guid.NewGuid());

        actualMqttEdge?.EdgeId.ShouldBe(mqttEdgeId);
    }

    [TestMethod]
    public async Task GetMqttEdge_UnknownEdge_ReturnValue()
    {
        var fixture = new GraphRepositoryTestFixture();
        await fixture.FillGraphRepository();
        var actualMqttEdge = await fixture.GraphRepository.GetMqttEdge(Guid.NewGuid());

        actualMqttEdge.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetMqttEdges_ValidData_ReturnEdges()
    {
        const string mqttEdgeId = "E-1";
        var fixture = new GraphRepositoryTestFixture();
        await fixture.FillGraphRepository();

        var graphEdgeId = fixture.GetGraphEdgeId(0);

        ICollection<MqttEdge> actualMqttEdges = await fixture.GraphRepository.GetMqttEdges([graphEdgeId]);

        actualMqttEdges.Count.ShouldBe(1);
        actualMqttEdges.Single().EdgeId.ShouldBe(mqttEdgeId);
    }

    [TestMethod]
    public async Task AddMqttEdge_ValidData_ReturnValue()
    {
        var newMqttEdge = new MqttEdge { EdgeId = "E-1", StartId = "S-1", EndId = "S-2", Bidirectional = false };
        var newGraphEdgeGuid = Guid.NewGuid();

        var fixture = new GraphRepositoryTestFixture();
        await fixture.FillGraphRepository();

        await fixture.GraphRepository.Add(newGraphEdgeGuid, newMqttEdge);

        var actualMqttEdge = await fixture.GraphRepository.GetMqttEdge(newGraphEdgeGuid);

        actualMqttEdge?.EdgeId.ShouldBe(newMqttEdge.EdgeId);
    }
}