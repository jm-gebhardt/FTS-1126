using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services;

[TestClass]
public class GraphRepositoryTest
{
    [TestMethod]
    public void GetMqttNode_NodeExist_ReturnValue()
    {
        const string graphNodeId = "WP-001";
        var fixture = new GraphRepositoryTestFixture()
            .WithGraphRepository();
        var actualMqttNode = fixture.GraphRepository.GetMqttNode(graphNodeId);

        actualMqttNode?.NodeId.ShouldBe(graphNodeId);
    }

    [TestMethod]
    public void GetMqttNode_UnknownNode_ReturnValue()
    {
        const string graphNodeId = "UN-001";
        var fixture = new GraphRepositoryTestFixture()
            .WithGraphRepository();
        var actualMqttNode = fixture.GraphRepository.GetMqttNode(graphNodeId);

        actualMqttNode.ShouldBeNull();
    }

    [TestMethod]
    public void AddMqttNode_ValidData_ReturnValue()
    {
        var newMqttNode = new MqttNode { NodeId = "NEW-001", X = -2f, Y = 3f, Z = 0f };
        var fixture = new GraphRepositoryTestFixture()
            .WithGraphRepository();
 
        fixture.GraphRepository.Add(newMqttNode.NodeId, newMqttNode);

        var actualMqttNode = fixture.GraphRepository.GetMqttNode(newMqttNode.NodeId);

        actualMqttNode?.NodeId.ShouldBe(newMqttNode.NodeId);
    }

    [TestMethod]
    public void GetMqttEdge_EdgeExist_ReturnValue()
    {
        const string mqttEdgeId = "E-1";

        var fixture = new GraphRepositoryTestFixture()
            .WithGraphRepository();

        var graphEdgeId = fixture.GetGraphEdgeId(0);
        var actualMqttEdge = fixture.GraphRepository.GetMqttEdge(graphEdgeId);

        actualMqttEdge?.EdgeId.ShouldBe(mqttEdgeId);
    }

    [TestMethod]
    public void GetMqttEdge_UnknownEdge_ReturnValue()
    {
        const string graphEdgeId = "UN-1";
        var fixture = new GraphRepositoryTestFixture()
            .WithGraphRepository();
        var actualMqttEdge = fixture.GraphRepository.GetMqttNode(graphEdgeId);

        actualMqttEdge.ShouldBeNull();
    }

    [TestMethod]
    public void AddMqttEdge_ValidData_ReturnValue()
    {
        var newMqttEdge = new MqttEdge { EdgeId = "E-1", StartId = "S-1", EndId = "S-2", Bidirectional = false };
        var newGraphEdgeGuid = Guid.NewGuid();

        var fixture = new GraphRepositoryTestFixture()
            .WithGraphRepository();
        
        fixture.GraphRepository.Add(newGraphEdgeGuid, newMqttEdge);

        var actualMqttEdge = fixture.GraphRepository.GetMqttEdge(newGraphEdgeGuid);
        
        actualMqttEdge?.EdgeId.ShouldBe(newMqttEdge.EdgeId);
    }
    }