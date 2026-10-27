using Gebhardt.Acc.Vda5050Simulator.Domain;

namespace Gebhardt.Acc.Vda5050Simulator.WebApi.Dtos;

/// <summary>
/// Maps the simulator state to the <see cref="Vda5050StateDto"/>.
/// </summary>
public static class Vda5050StateDtoMapper
{
    public static Vda5050StateDto Map(AgvIdentity id, AgvStateSnapshot snapshot, string version)
    {
        return new Vda5050StateDto
        {
            CorrelationId = id.Id,
            MessageCreatedDate = snapshot.Timestamp.UtcDateTime,
            HeaderId = (uint)snapshot.HeaderId,
            Timestamp = snapshot.Timestamp,
            Version = version,
            Manufacturer = id.Manufacturer,
            SerialNumber = id.SerialNumber,
            OrderId = snapshot.OrderId,
            OrderUpdateId = snapshot.OrderUpdateId,
            LastNodeId = snapshot.LastNodeId,
            LastNodeSequenceId = snapshot.LastNodeSequenceId,
            Driving = snapshot.Driving,
            OperatingMode = Enum.Parse<OperatingModeDto>(snapshot.OperatingMode.ToString()),
            NodeStates = snapshot.NodeStates
                .Select(n => new NodeStateDto { NodeId = n.NodeId, SequenceId = n.SequenceId, Released = n.Released })
                .ToList(),
            EdgeStates = snapshot.EdgeStates
                .Select(e => new EdgeStateDto { EdgeId = e.EdgeId, SequenceId = e.SequenceId, Released = e.Released })
                .ToList(),
            AgvPosition = snapshot.Position == null
                ? null
                : new Vda5050AgvPositionDto
                {
                    X = snapshot.Position.X,
                    Y = snapshot.Position.Y,
                    Theta = snapshot.Position.Theta,
                    MapId = snapshot.Position.MapId,
                    PositionInitialized = snapshot.Position.PositionInitialized
                },
            Loads = snapshot.Loads
                .Select(l => new LoadDto { LoadId = l.LoadId, LoadType = l.LoadType, LoadPosition = l.LoadPosition })
                .ToList(),
            ActionStates = snapshot.ActionStates
                .Select(a => new ActionStateDto
                {
                    ActionId = a.ActionId,
                    ActionType = a.ActionType,
                    ActionStatus = (ActionStatusDto)(int)a.Status
                })
                .ToList(),
            BatteryState = new BatteryStateDto { BatteryCharge = snapshot.BatteryCharge, Charging = snapshot.Charging },
            Errors = snapshot.Errors
                .Select(e => new ErrorDto
                {
                    ErrorType = e.ErrorType,
                    ErrorDescription = e.Description,
                    ErrorLevel = (ErrorLevelDto)(int)e.Level,
                    ErrorReferences = e.ErrorReferences?
                        .Select(r => new ErrorReferenceDto { ReferenceKey = r.Key, ReferenceValue = r.Value })
                        .ToList()
                })
                .ToList(),
            SafetyState = new SafetyStateDto { EStop = EStopDto.None }
        };
    }
}
