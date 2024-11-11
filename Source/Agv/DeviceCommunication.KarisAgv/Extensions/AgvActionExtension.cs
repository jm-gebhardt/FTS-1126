using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Extensions;

/// <summary>
/// Provides extension methods for <see cref="AgvAction"/>.
/// </summary>
public static class AgvActionExtension
{
    /// <summary>
    /// Convert the action to <see cref="MqttOrder"/>.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="orderId">The id of the order.</param>
    /// <param name="recordType">The record type of the order.</param>
    /// <param name="agvSerialNumber">The serial number of the agv.</param>
    /// <param name="nodes">The route nodes.</param>
    /// <param name="edges">The route edges.</param>
    /// <returns> The order as used in the mqtt message to the agv.</returns>
    public static MqttOrder ConvertToOrder(this AgvAction action, string orderId, OrderRecordType recordType, string agvSerialNumber, List<object> nodes, List<object> edges)
    {
        if (action.IsMoveToCoordinate)
        {
            return action.ConvertToMoveToCoordinateOrder(orderId, recordType, agvSerialNumber);
        }
        return action.ConvertToMoveToStationsOrder(orderId, recordType, agvSerialNumber, nodes, edges);
    }

    /// <summary>
    /// Convert the action to <see cref="MqttOrder"/> with coordinates.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="orderId">The id of the order.</param>
    /// <param name="recordType">The record type of the order.</param>
    /// <param name="agvSerialNumber">The serial number of the agv.</param>
    /// <returns>The order as used in the mqtt message to the agv.</returns>
    public static MqttOrder ConvertToMoveToCoordinateOrder(this AgvAction action, string orderId, OrderRecordType recordType, string agvSerialNumber)
    {
        if (!action.IsMoveToCoordinate)
        {
            throw new InvalidOperationException($"Can't handle action target {action.Target} for move to coordinate");
        }
        return new MqttOrder(action.CorrelationId.ToString())
        {
            SerialNumber = agvSerialNumber,
            OrderType = OrderType.Move,
            OrderId = orderId,
            RecordType = recordType,
            Coordinates = new Coordinates
            {
                ToTheta = action.Target.Position!.Theta,
                ToX = action.Target.Position!.X,
                ToY = action.Target.Position!.Y
            }
        };
    }

    /// <summary>
    /// Convert the action to <see cref="MqttOrder"/> with stations.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="orderId">The id of the order.</param>
    /// <param name="recordType">The record type of the order.</param>
    /// <param name="agvSerialNumber">The serial number of the agv.</param>
    /// <param name="nodes">The route nodes.</param>
    /// <param name="edges">The route edges.</param>
    /// <returns> The order as used in the mqtt message to the agv.</returns>
    public static MqttOrder ConvertToMoveToStationsOrder(this AgvAction action, string orderId, OrderRecordType recordType, string agvSerialNumber, List<object> nodes, List<object> edges)
    {
        if (action.IsMoveToCoordinate)
        {
            throw new InvalidOperationException($"Can't handle action target {action.Target} for move to station");
        }
        var orderType = action.ActionType.ToOrderType();

        return new MqttOrder(action.CorrelationId.ToString())
        {
            SerialNumber = agvSerialNumber,
            OrderType = orderType,
            OrderId = orderId,
            RecordType = recordType,
            Stations = new Stations
            {
                FromStation = action.GetSourceStation(),
                ToStation =  action.GetDestinationStation()
            },
            Route = new Route()
            {
                Edges = edges,
                Nodes = nodes
            }
        };
    }

    /// <summary>
    /// Get the source station information from the action.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <returns><see cref="StationInfo"/> for the source of the action.</returns>
    public static StationInfo? GetSourceStation(this AgvAction action)
    {
        if (action.IsMoveToCoordinate)
        {
            throw new InvalidOperationException($"Can't handle action target {action.Target} for deriving source");
        }
        bool agvShouldRotate = action.InfoToAgv?.NeedsRotation() ?? false;
        double? liftHeight = action.InfoToAgv?.GetLiftHeight() ?? null;
        if (action.ActionType == ActionType.Pick)
        {
            return new StationInfo
            {
                Id = action.Target.NodeId!,
                Rotate = agvShouldRotate,
                LiftHeight = liftHeight
            };
        }
        return null;
    }

    /// <summary>
    /// Get the destination station information from the action.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <returns><see cref="StationInfo"/> for the destination of the action.</returns>
    public static StationInfo? GetDestinationStation(this AgvAction action)
    {
        if (action.IsMoveToCoordinate)
        {
            throw new InvalidOperationException($"Can't handle action target {action.Target} for deriving destination");
        }
        bool agvShouldRotate = action.InfoToAgv?.NeedsRotation() ?? false;
        double? liftHeight = action.InfoToAgv?.GetLiftHeight() ?? null;
        string targetNodeId = action.Target.NodeId!;
        if (action.ActionType == ActionType.Pick)
        {
            return null;
        }

        var destinationToSend = action.ActionType switch
        {
            ActionType.Drop => new StationInfo { Id = targetNodeId, LiftHeight = liftHeight },
            ActionType.Move => new StationInfo { Id = targetNodeId, Rotate = agvShouldRotate },
            ActionType.Charge => new StationInfo { Id = targetNodeId },
            _ => throw new InvalidOperationException($"Can't handle {action.ActionType} yet")
        };
        return destinationToSend;
    }
}