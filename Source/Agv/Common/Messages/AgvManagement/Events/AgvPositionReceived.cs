using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// An event reporting the current position of an AGV.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record AgvPositionReceived : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Identifier of the AGV.
        /// </summary>
        public string AgvId { get; }

        /// <summary>
        /// Returns true, when the position is known and valid.
        /// </summary>
        public bool PositionInitialized { get; }

        /// <summary>
        /// Current X-position [m].
        /// </summary>
        public float X { get; }

        /// <summary>
        /// Current Y-position [m].
        /// </summary>
        public float Y { get; }

        /// <summary>
        /// Current angle (rotation) [rad].
        /// </summary>
        public float Theta { get; }

        /// <summary>
        /// Deviation from the responded position [m].
        /// </summary>
        public float? DeviationRange { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="AgvPositionReceived"/>
        /// </summary>
        public AgvPositionReceived(Guid correlationId, string agvId, bool positionInitialized, float x, float y, float theta, float? deviationRange = null)
        {
            CorrelationId = correlationId;
            AgvId = agvId;
            PositionInitialized = positionInitialized;
            X = x;
            Y = y;
            Theta = theta;
            DeviationRange = deviationRange;
        }
    }
}
