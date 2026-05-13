using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator;

[PublicAPI]
public static class MqttOrderGenerator
{
    public static MqttOrder Generate(
        string serialNumber = "simagv0",
        string orderId = "order1",
        int orderUpdateId = 0,
        ICollection<MqttOrderNode>? nodes = null,
        ICollection<MqttOrderEdge>? edges = null)
    {
        return new MqttOrder
        {
            HeaderId = 1,
            Timestamp = DateTimeOffset.UtcNow,
            Version = Vda5050Specifications.Version,
            Manufacturer = "gebhardt",
            SerialNumber = serialNumber,
            OrderId = orderId,
            OrderUpdateId = orderUpdateId,
            Nodes = nodes ?? [GenerateNode()],
            Edges = edges ?? []
        };
    }

    public static MqttOrderNode GenerateNode(string nodeId = "node1", int sequenceId = 0, bool released = true)
    {
        return new MqttOrderNode
        {
            NodeId = nodeId,
            SequenceId = sequenceId,
            Released = released
        };
    }

    public static MqttOrderEdge GenerateEdge(
        string edgeId = "edge1",
        int sequenceId = 1,
        string startNodeId = "node1",
        string endNodeId = "node2",
        bool released = true)
    {
        return new MqttOrderEdge
        {
            EdgeId = edgeId,
            SequenceId = sequenceId,
            StartNodeId = startNodeId,
            EndNodeId = endNodeId,
            Released = released
        };
    }
}
