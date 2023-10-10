using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Contracts;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;

/// <summary>
/// Represents the Agv
/// </summary>
public class Agv: IAgvId
{
    /// <summary>
    /// The correlation id of the agv.
    /// </summary>
    public Guid CorrelationId { get; private set; }

    /// <inheritdoc />
    public string AgvId { get; set; }

    /// <summary>
    /// Last received AgvStatus
    /// </summary>
    public AgvStatus? AgvStatus { get; set; }


    private AgvState _currentState;
    /// <summary>
    /// The current state of the agv.
    /// </summary>
    public AgvState CurrentAgvState
    {
        get => _currentState;
        private set
        {
            bool hasChanged = value != _currentState;

            if (hasChanged)
            {
                PreviousAgvState = _currentState;
                TimeOfStateTransition = DateTime.UtcNow;
            }
            _currentState = value;

        }
    }

    /// <summary>
    ///Gets the previous state of the agv.
    /// </summary>
    public AgvState PreviousAgvState { get; private set; } = AgvState.Initial;

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

    /// <summary>
    /// Returns true when there is a life sign of the AGV in regular time intervals,
    /// returns false when there is no life sign for a certain time.
    /// </summary>
    public bool IsOnline { get; private set; }

    /// <summary>
    /// is AGV locked?
    /// </summary>
    public bool IsLocked { get; private set; }


    /// <summary>
    /// is the AGV  currently locked?
    /// </summary>
    public bool IsCharging => CurrentAgvState == AgvState.Charging;

    /// <summary>
    /// Does the agv need maintenance?
    /// </summary>
    public bool NeedsMaintenance { get; private set; }

    /// <summary>
    /// Returns True if Agv is in error (has any error) and not ready to drive
    /// </summary>
    public bool HasError { get; set; }

    /// <summary>
    /// Gets if the state of the agv changed.
    /// </summary>
    public bool StateChanged => CurrentAgvState != PreviousAgvState && (TimeOfStateTransition - DateTime.UtcNow).Seconds < 2;

    /// <summary>
    /// Agv is ready.
    /// </summary>
    public bool IsReady => IsReadyStatic.Compile().Invoke(this);

    /// <summary>
    /// Token used by the send status update schedule
    /// </summary>
    public Guid? AgvOfflineTokenId { get; set; }

    /// <summary>
    /// returns the time of the last state transition
    /// </summary>
    public DateTime TimeOfStateTransition { get; set; }

    /// <summary>
    /// returns the time of the last lock state change
    /// </summary>
    public DateTime TimeOfLockStateChanged { get; set; }

    /// <summary>
    /// If the graph of this agv exists.
    /// </summary>
    public bool GraphExists { get; set; }


    /// <summary>
    /// If the data of this agv exists.
    /// </summary>
    public bool DataExist { get; set; }

    /// <summary>
    /// Time that has to pass before resending a request telegram in case AgvGraph or AgvData are still null
    /// </summary>
    /// <remarks>Interval indicated in seconds</remarks>
    private const int AgvRequestTimeout = 30;

    /// <summary>
    /// The threshold for considering a battery change significant.
    /// </summary>
    public const double BatteryChargeChangedThreshold = 0.01d;

    /// <summary>
    /// Time that has to pass before resending a request telegram in case AgvGraph or AgvData are already given
    /// </summary>
    /// <remarks>Interval indicated in seconds</remarks>
    private const int DailyRequestUpdateInterval = 24 * 3600;

    /// <summary>
    /// Default constructor required to connect.
    /// </summary>
    public Agv(Guid correlationId,
        string agvId,
        AgvStatus? agvStatus = null,
        bool isLocked = false)
    {
        if (correlationId == Guid.Empty)
        {
            throw new InvalidOperationException($"{GetType()} requires correlation ID.");
        }
        CorrelationId = correlationId;
        AgvId = agvId;
        AgvStatus = agvStatus;
        IsLocked = isLocked;
        NeedsMaintenance = false;
        CurrentAgvState = AgvState.Initial;
    }

    /// <summary>
    /// EfCore ctor
    /// </summary>
    [UsedImplicitly]
    public Agv(string agvId)
    {
        CorrelationId = agvId.ToGuid();
        AgvId = agvId;
        CurrentAgvState = AgvState.Initial;
    }

    /// <summary>
    /// Test ctor
    /// </summary>
    public Agv()
    {
        AgvId = "Test";
        CurrentAgvState = AgvState.Initial;
    }

    /// <summary>
    /// Check if the Agv is eligible for charging.
    /// </summary>
    /// <param name="settings">The global agv settings.</param>
    public bool CheckIsEligibleForCharging(AgvSettings settings)
    {
        if (AgvStatus == null)
        {
            return false;
        }
        return AgvStatus.BatteryCharge <= settings.BatteryLevelLow;
    }

    /// <summary>
    /// Check if the Agv needs charging.
    /// </summary>
    /// <param name="settings">The global agv settings.</param>
    public bool CheckNeedsCharging(AgvSettings settings)
    {
        if (AgvStatus == null)
        {
            return false;
        }
        return AgvStatus.BatteryCharge <= settings.BatteryLevelCritical;
    }

    /// <summary>
    /// Discovers the agv.
    /// </summary>
    /// <param name="graphExists">True if a graph already exists.</param>
    public void TransitionToDiscovered(bool graphExists)
    {
        CurrentAgvState = CurrentAgvState.TransitionTo(AgvState.Discovered);
        GraphExists = graphExists;
    }

    /// <summary>
    /// Charge the agv.
    /// </summary>
    public void TransitionToCharging()
    {
        CurrentAgvState = CurrentAgvState.TransitionTo(AgvState.Charging);
    }

    /// <summary>
    /// Check to see if the difference in battery charge is bigger than <see cref="BatteryChargeChangedThreshold"/>.
    /// </summary>
    /// <returns></returns>
    private bool CheckBatteryChargeChange(double newBatteryCharge)
    {
        if (AgvStatus == null)
        {
            return false;
        }
        return Math.Abs(AgvStatus.BatteryCharge - newBatteryCharge) > BatteryChargeChangedThreshold;
    }

    private bool CheckLastNodeIdChanged(string? newLastNodeId)
    {
        if (AgvStatus == null)
        {
            return false;
        }
        return AgvStatus.LastNodeId != newLastNodeId;
    }

    private void SetIdle()
    {
        CurrentAgvState = AgvState.Idle;
    }

    /// <summary>
    /// Update the agv status.
    /// </summary>
    /// <param name="newStatus"></param>
    public (bool batteryChargeChanged, bool lastNodeIdChanged) UpdateAgvStatus(AgvStatus newStatus)
    {
        bool batteryChargeChanged = CheckBatteryChargeChange(newStatus.BatteryCharge);
        if (!batteryChargeChanged && AgvStatus != null)
        {
            newStatus = new AgvStatus(newStatus.CorrelationId, newStatus.OrderId, newStatus.Busy, newStatus.Automatic, AgvStatus.BatteryCharge, newStatus.Charging, newStatus.Disturbed, newStatus.LastNodeId);
        }

        bool lastNodeIdChanged = CheckLastNodeIdChanged(newStatus.LastNodeId);
        AgvStatus = newStatus;
        return (batteryChargeChanged, lastNodeIdChanged);
    }

    /// <summary>
    /// Try to accept the agv, i.e. set it to idle if it is not idling yet, busy, or charging.
    /// </summary>
    public bool TryAccept()
    {
        if (new [] { AgvState.Idle, AgvState.Busy, AgvState.Charging}.Contains(CurrentAgvState))
        {
            return false;
        }
        SetIdle();
        TimeOfStateTransition = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Transition to idle state.
    /// </summary>
    public void TransitionToIdle()
    {
        CurrentAgvState = CurrentAgvState.TransitionTo(AgvState.Idle);
    }

    /// <summary>
    /// Force the transition to idle state irrespective of previous state.
    /// </summary>
    public void ForceIdle()
    {
       SetIdle();
    }

    /// <summary>
    ///  Try to become busy.
    /// </summary>
    /// <returns></returns>
    public bool TryTransitionToBusy(bool ignoreLocked)
    {
        bool isAvailable = IsAvailable(ignoreLocked: ignoreLocked);
        if (!isAvailable)
        {
            return isAvailable;
        }
        CurrentAgvState = CurrentAgvState.TransitionTo(AgvState.Busy);
        return isAvailable;

    } 

    /// <summary>
    /// Checks if the agv is available.
    /// </summary>
    /// <param name="ignoreCharging">If charging should be ignored.</param>
    /// <param name="ignoreLocked">If locked should be ignored.</param>
    public bool IsAvailable(bool ignoreCharging = true, bool ignoreLocked = false)
    {
        bool unlocked = !IsLocked || ignoreLocked;
        bool chargingButIgnore = CurrentAgvState == AgvState.Charging && ignoreCharging;
        bool idle = CurrentAgvState == AgvState.Idle;
        return IsReady && (idle || chargingButIgnore) && unlocked;
    }

    /// <summary>
    /// Lock agv.
    /// </summary>
    public void Lock()
    {
        IsLocked = true;
        TimeOfLockStateChanged = DateTime.UtcNow;
    }

    /// <summary>
    /// Unlock agv.
    /// </summary>
    public void Unlock()
    {
        SetIdle();
        IsLocked = false;
        TimeOfLockStateChanged = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if it is time and necessary to request a graph.
    /// </summary>
    public bool ShouldRequestGraph()
    {

        double secondsSinceLastRequest = TimeSpan.MaxValue.TotalSeconds;
        if (AgvGraphRequestedLastTime != null)
        {
            secondsSinceLastRequest = ((TimeSpan)(DateTime.UtcNow - AgvGraphRequestedLastTime)).TotalSeconds;
        }

        bool shouldBeRequested = !GraphExists && secondsSinceLastRequest > DailyRequestUpdateInterval;
        if (shouldBeRequested)
        {
            AgvGraphRequestedLastTime = DateTime.UtcNow;
        }
        return shouldBeRequested;
    }

    /// <summary>
    ///  Checks if it is time to request new agv data.
    /// </summary>
    /// <returns></returns>
    public bool ShouldRequestData()
    {

        double secondsSinceLastRequest = TimeSpan.MaxValue.TotalSeconds;
        if (AgvDataRequestedLastTime != null)
        {
            secondsSinceLastRequest = ((TimeSpan)(DateTime.UtcNow - AgvDataRequestedLastTime)).TotalSeconds;
        }
        int timeout = DataExist ? DailyRequestUpdateInterval : AgvRequestTimeout;

        bool shouldBeRequested = secondsSinceLastRequest > timeout;
        if (shouldBeRequested)
        {
            AgvDataRequestedLastTime = DateTime.UtcNow;
        }
        return shouldBeRequested;
    }

    /// <summary>
    /// Go offline.
    /// </summary>
    public void GoOffline(Guid offlineToken)
    {
        if (AgvOfflineTokenId == null || AgvOfflineTokenId!.Value != offlineToken)
        {
            return;
        }
        IsOnline = false;
        AgvOfflineTokenId = default;
    }

    /// <summary>
    /// Sets agv online.
    /// </summary>
    public void SetOnline(Guid offlineToken)
    {
        AgvOfflineTokenId = offlineToken;
        IsOnline = true;
    }

    /// <summary>
    /// Marks this agv for maintenance at the next check.
    /// </summary>
    public void MarkAgvForMaintenance()
    {
        NeedsMaintenance = true;
    }

    /// <summary>
    /// Static expression to wrap <see cref="IsReady"/> for LINQ to SQL conversion.
    /// </summary>
    public static Expression<Func<Agv, bool>> IsReadyStatic
    {
        get
        {
            return agv => agv.AgvStatus != null && agv.AgvStatus!.LastNodeId != null && !agv.HasError;
        }
    }
}   
