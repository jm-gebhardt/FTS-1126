using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;

/// <summary>
/// AGV Saga used by AgvStateMachine.s
/// This stores the current AGV state
/// </summary>
[PublicAPI]
public class AgvSaga : SagaStateMachineInstance, IAgv
{
    /// <inheritdoc cref="IAgv"/>
    public Guid CorrelationId { get; set; }

    /// <inheritdoc />
    public string AgvId { get; set; }

    /// <summary>
    /// Current State
    /// </summary>
    public string CurrentState { get; set; }

    /// <summary>
    /// Last Received AgvStatus
    /// </summary>
    public AgvStatus? AgvStatus { get; set; }

    /// <summary>
    /// UTC timestamp when AGV data was requested
    /// </summary>
    /// <remarks>Nullable declaration required to make data model mappable according to EF Core</remarks>
    public DateTime? AgvDataRequestedLastTime { get; set; }

    /// <summary>
    /// UTC timestamp when AGV graph was requested
    /// </summary>
    /// <remarks>Nullable declaration required to make data model mappable according to EF Core</remarks>
    public DateTime? AgvGraphRequestedLastTime { get; set; }

    /// <inheritdoc />
    public bool IsOnline { get; set; }
        
    /// <inheritdoc />
    public bool IsLocked { get; set; }
        
    /// <inheritdoc />
    public bool HasError { get; set; }

    /// <summary>
    /// Agv is ready.
    /// </summary>
    public bool IsReady => AgvStatus != null && AgvStatus!.LastNodeId != null && !HasError;

    /// <summary>
    /// Token used by the send status update schedule
    /// </summary>
    public Guid? AgvOfflineTokenId { get; set; }

    /// <inheritdoc />
    public DateTime TimeOfStateTransition { get; set; }

    /// <inheritdoc />
    public DateTime TimeOfLockStateChanged { get; set; }

    /// <inheritdoc />
    public bool GraphExists { get; set; }

    /// <summary>
    /// Default constructor required to connect.
    /// </summary>
    public AgvSaga(Guid correlationId,
        string agvId,
        string? currentState = null,
        AgvStatus? agvStatus = null,
        bool isLocked = false)
    {
        if (correlationId == Guid.Empty)
        {
            throw new InvalidOperationException($"{GetType()} requires correlation ID.");
        }
        CorrelationId = correlationId;
        AgvId = agvId;
        CurrentState = currentState ?? default!;
        AgvStatus = agvStatus;
        IsLocked = isLocked;
    }

    /// <summary>
    /// Get interval since last update of AgvDate
    /// </summary>
    /// <returns>Time span between now and last update</returns>
    public TimeSpan GetTimeSpanSinceLastRequest_AgvData()
    {
        if (AgvDataRequestedLastTime != null)
        {
            return (TimeSpan)(DateTime.UtcNow - AgvDataRequestedLastTime);
        }
        return TimeSpan.MaxValue;
    }

    /// <summary>
    /// Get interval since last update of AgvGraph
    /// </summary>
    /// <returns>Time span between now and last update</returns>
    public TimeSpan GetTimeSpanSinceLastRequest_AgvGraph()
    {
        if (AgvGraphRequestedLastTime != null)
        {
            return (TimeSpan)(DateTime.UtcNow - AgvGraphRequestedLastTime);
        }
        return TimeSpan.MaxValue;
    }

    /// <summary>
    /// Checks if the agv is available.
    /// </summary>
    /// <param name="ignoreCharging">If charging should be ignored.</param>
    /// <param name="ignoreLocked">If locked should be ignored.</param>
    public bool AgvIsAvailable(bool ignoreCharging = true, bool ignoreLocked = false)
    {
        bool unlocked = !IsLocked || ignoreLocked;
        bool notCharging = CurrentState is nameof(AgvState.Charging) && ignoreCharging;
        bool idle = CurrentState == nameof(AgvState.Idle);
        return IsReady && (idle || notCharging) && unlocked;
    }
}