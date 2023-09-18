using Gebhardt.StoreWare.Agv.Common.Contracts;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

/// <summary>
/// IAgv is a read-only interface to provide current agv data
/// </summary>
[PublicAPI]
public interface IAgv : CorrelatedBy<Guid>, IAgvId
{
    /// <summary>
    /// The current state of the AGV
    /// </summary>
    public string CurrentState { get; }

    /// <summary>
    /// AGV Status
    /// </summary>
    public AgvStatus? AgvStatus { get; }

    /// <summary>
    /// Returns true when there is a life sign of the AGV in regular time intervals,
    /// returns false when there is no life sign for a certain time.
    /// </summary>
    public bool IsOnline { get; }
        
    /// <summary>
    /// is AGV locked?
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Returns True if Agv is in error (has any error) and not ready to drive
    /// </summary>
    public bool HasError { get; }

    /// <summary>
    /// If the graph of this agv exists.
    /// </summary>
    public bool GraphExists { get; set; }

    /// <summary>
    /// returns the time of the last state transition
    /// </summary>
    public DateTime TimeOfStateTransition { get; set; }
        
    /// <summary>
    /// returns the time of the last lock state change
    /// </summary>
    public DateTime TimeOfLockStateChanged { get; set; }
}