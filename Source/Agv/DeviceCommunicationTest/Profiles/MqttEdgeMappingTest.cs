using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Profiles;

[TestClass]
public class MqttEdgeMappingTest
{
    [TestMethod]
    public void Map_ToMqttEdge_MapsAllProperties()
    {
        var mapper = MappingHelper.GetMapper(new MqttEdgeMappingProfile());
        var mqttEdgeMapping = new MqttEdgeMapping(Guid.NewGuid())
        {
            Attributes = [ new MqttAttribute()] ,
            Bidirectional = true,
            ControlPoints = [new MqttControlPoint(){Weight = 0.5f, X = 1f, Y = 1f }],
            EdgeId = "edge id",
            EndId = "end id",
            EdgeType = "edge type",
            ExitAngle = 1.4f,
            ExitDistance = 0.2f,
            MaxVelocity = 0.1f,
            StartId = "start id"
        };

        var mqttEdge = mapper.Map<MqttEdge>(mqttEdgeMapping);
       
        mqttEdge.Bidirectional.ShouldBe(mqttEdgeMapping.Bidirectional);
        mqttEdge.EdgeId.ShouldBe(mqttEdgeMapping.EdgeId);
        mqttEdge.EndId.ShouldBe(mqttEdgeMapping.EndId);
        mqttEdge.EdgeType.ShouldBe(mqttEdgeMapping.EdgeType);
        mqttEdge.ExitAngle.ShouldBe(mqttEdgeMapping.ExitAngle);
        mqttEdge.ExitDistance.ShouldBe(mqttEdgeMapping.ExitDistance);
        mqttEdge.MaxVelocity.ShouldBe(mqttEdgeMapping.MaxVelocity);
        mqttEdge.StartId.ShouldBe(mqttEdgeMapping.StartId);
        mqttEdge.ControlPoints.ShouldBeEquivalentTo(mqttEdgeMapping.ControlPoints);
        mqttEdge.Attributes.ShouldBeEquivalentTo(mqttEdgeMapping.Attributes);
    }

    [TestMethod]
    public void Map_ToMqttEdges_MapsAllProperties()
    {
        var mapper = MappingHelper.GetMapper(new MqttEdgeMappingProfile());
        var mqttEdgeMapping = new MqttEdgeMapping(Guid.NewGuid())
        {
            Attributes = [new MqttAttribute()],
            Bidirectional = true,
            ControlPoints = [new MqttControlPoint() { Weight = 0.5f, X = 1f, Y = 1f }],
            EdgeId = "edge id",
            EndId = "end id",
            EdgeType = "edge type",
            ExitAngle = 1.4f,
            ExitDistance = 0.2f,
            MaxVelocity = 0.1f,
            StartId = "start id"
        };

        var mappings = new List<MqttEdgeMapping> { mqttEdgeMapping };

        var mqttEdges = mapper.Map<ICollection<MqttEdge>>(mappings);

        var mqttEdge = mqttEdges.Single();

        mqttEdge.Bidirectional.ShouldBe(mqttEdgeMapping.Bidirectional);
        mqttEdge.EdgeId.ShouldBe(mqttEdgeMapping.EdgeId);
        mqttEdge.EndId.ShouldBe(mqttEdgeMapping.EndId);
        mqttEdge.EdgeType.ShouldBe(mqttEdgeMapping.EdgeType);
        mqttEdge.ExitAngle.ShouldBe(mqttEdgeMapping.ExitAngle);
        mqttEdge.ExitDistance.ShouldBe(mqttEdgeMapping.ExitDistance);
        mqttEdge.MaxVelocity.ShouldBe(mqttEdgeMapping.MaxVelocity);
        mqttEdge.StartId.ShouldBe(mqttEdgeMapping.StartId);
        mqttEdge.ControlPoints.ShouldBeEquivalentTo(mqttEdgeMapping.ControlPoints);
        mqttEdge.Attributes.ShouldBeEquivalentTo(mqttEdgeMapping.Attributes);
    }
}