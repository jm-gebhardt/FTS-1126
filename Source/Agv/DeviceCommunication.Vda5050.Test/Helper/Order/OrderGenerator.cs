using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using JetBrains.Annotations;
using OrderAction = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders.OrderAction;
using OrderActionParameter = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders.OrderActionParameter;
using OrderActionState = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders.OrderActionState;
using OrderEdge = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders.OrderEdge;
using OrderNode = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders.OrderNode;
using OrderState = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders.OrderState;
using Route = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders.Route;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Order;

/// <summary>
/// Provides a helper for generating <see cref="Order"/>s for unit testing.
/// </summary>
[PublicAPI]
public static class OrderGenerator
{
    public static Vda5050.Domain.Models.Orders.Order GeneratePickOrder(string startNodeId, string endNodeId, int nodeNo, Vda5050.Domain.Models.Agvs.Agv? assignedAgv=null)
    {
        List<OrderEdge> edges = [];
        string previous = startNodeId;
        List<OrderNode> nodes = [RouteGenerator.GenerateOrderNode(startNodeId, 0, [])];
        for (int iNode = 1; iNode < nodeNo - 1; iNode++)
        {
            var node = RouteGenerator.GenerateOrderNode(iNode.ToString(), iNode * 2, []);
            var edge = RouteGenerator.GenerateOrderEdge(iNode * 2 - 1, start: previous, end: node.NodeId, []);
            nodes.Add(node);
            edges.Add(edge);
            previous = node.NodeId;
        }
        edges.Add(RouteGenerator.GenerateOrderEdge(edges.Last().SequenceId + 2, start: nodes.Last().NodeId, end: endNodeId, []));
        nodes.Add(RouteGenerator.GenerateOrderNode(endNodeId, nodes.Last().SequenceId + 2, [GeneratePickAction()]));
        return new Vda5050.Domain.Models.Orders.Order()
        {
            OrderId = Guid.NewGuid().ToString(),
            OrderUpdateId = 0,
            AssignedAgv = assignedAgv ?? new Vda5050.Domain.Models.Agvs.Agv(Guid.NewGuid(), "manufacturer", "seriesName", "serialNumber"),
            PlannedRoute = new Route(nodes, edges),
            Route = new Route([], [])
        };
    }

    public static OrderAction GeneratePickAction()
    {
        return new OrderAction(Vda5050Specifications.ActionTypes.Pick, null, BlockingType.NONE, [
            new OrderActionParameter()
            {
                Key = Vda5050Specifications.Parameters.StationType, Value = "Pick"
            }
        ]);
    }

    public static OrderState GenerateOrderState(string orderId, int orderUpdateId, int lastNodeSequenceId, bool? newBaseRequest, List<OrderActionState>? actionStates=null, List<AgvError>? errors = null )
    {
        return new OrderState
        {
            OrderId = orderId,
            OrderUpdateId = orderUpdateId,
            NewBaseRequest = newBaseRequest,
            LastNodeSequenceId = lastNodeSequenceId,
            ActionStates = actionStates ?? [],
            Errors = errors ?? []
        };
    }

    public static AgvError GenerateAgvError(string errorType, List<ErrorReference>? errorReferences=null)
    {
        return new AgvError(
            Guid.NewGuid(), "error title", "error description", null,
            ErrorLevel.Fatal, errorType, ErrorCategory.General, errorReferences,
            DateTime.UtcNow, ErrorFlag.Agv, AgvErrorSource.Agv);
    }
}

