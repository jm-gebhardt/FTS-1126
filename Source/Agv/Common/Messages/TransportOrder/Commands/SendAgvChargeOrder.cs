using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands
{
    /// <summary>
    /// A command to the agv communication module to execute a charge order
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record SendAgvChargeOrder : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// The AGV that shall go charging
        /// </summary>
        public string AgvId { get; }

        /// <summary>
        /// The charger where the AGV shall go to
        /// </summary>
        public string ToCharger { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="SendAgvChargeOrder"/>
        /// </summary>
        public SendAgvChargeOrder(Guid correlationId, string agvId, string toCharger)
        {
            CorrelationId = correlationId;
            AgvId = agvId;
            ToCharger = toCharger;
        }
    }
}
