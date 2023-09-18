using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Request if the Agv could go to the busy state, does not change any state, just a question 
/// </summary>
[PublicAPI]
public record CanAgvTransitionToBusyRequest : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// If true the return value will not take the AgvStatus.Charging flag of the Agv into account
    /// </summary>
    public bool IgnoreCharging { get; }

    /// <summary>
    /// If this is set to true, the Agv.IsLocked may also be true
    /// </summary>
    public bool IgnoreLocked { get; }

    /// <inheritdoc cref="CanAgvTransitionToBusyRequest"/>
    /// <param name="correlationId">The AgvCorrelationId</param>
    /// <param name="ignoreCharging">If true the return value will not take the charging flag of the Agv into account1</param>
    /// <param name="ignoreLocked">If this is set to true, the Agv.IsLocked may also be true</param>
    public CanAgvTransitionToBusyRequest(Guid correlationId, bool ignoreCharging, bool ignoreLocked)
    {
        CorrelationId = correlationId;
        IgnoreCharging = ignoreCharging;
        IgnoreLocked = ignoreLocked;
    }
}