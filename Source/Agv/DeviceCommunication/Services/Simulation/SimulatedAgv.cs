using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using JetBrains.Annotations;

// ReSharper disable InvalidUsage
namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;

/// <summary>
/// Represents a simulated agv.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="AgvId">The agv id.</param>
/// <param name="CurrentState">The current state o the agv.</param>
/// <param name="IsOnline">Flag indicating if the agv is online.</param>
/// <param name="IsLocked">Flag indicating if the agv is locked.</param>
/// <param name="HasError">Flag indicating if the agv has an error.</param>
/// <param name="GraphExists">Flag indicating if the graph of the agv exists.</param>
/// <param name="TimeOfStateTransition">The time stamp of the last state transition.</param>
/// <param name="TimeOfLockStateChanged">The time stamp of the last lock state change.</param>
/// <param name="AgvStatus">The agv status.</param>
/// <param name="AgvGraph">The agv graph.</param>
/// <param name="AgvData">The agv data.</param>
[PublicAPI]
public record SimulatedAgv(Guid CorrelationId,
    string AgvId,
    string CurrentState,
    bool IsOnline,
    bool IsLocked,
    bool HasError,
    bool GraphExists,
    DateTime TimeOfStateTransition,
    DateTime TimeOfLockStateChanged,
    AgvStatus? AgvStatus = null,
    AgvGraph? AgvGraph = null,
    AgvData? AgvData = null
);