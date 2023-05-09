using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands
{
    /// <summary>
    /// Request if the Agv could go to the busy state, does not change any state, just a question 
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record RequestCanAgvTransitionToBusy : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// If true the return value will not take the charging flag of the Agv into account1
        /// </summary>
        public bool IgnoreCharging { get; }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="ignoreCharging"></param>
        public RequestCanAgvTransitionToBusy(Guid correlationId, bool ignoreCharging)
        {
            CorrelationId = correlationId;
            IgnoreCharging = ignoreCharging;
        }
    }
}