using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Models
{
    /// <summary>
    /// Interface representing the Map table in Database
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public interface IMap: CorrelatedBy<Guid>
    {
        /// <summary>
        /// MapId
        /// </summary>
        string MapId { get; set; }

        /// <summary>
        /// Map in form bytes
        /// </summary>
        byte[]? MapValue { get; set; }

        /// <summary>
        /// Timestamp of last modification
        /// </summary>
        DateTime ModificationDate { get; set; }

        /// <summary>
        /// The resolution of the maps image. Expressed as how many [m] per pixel are represented
        /// </summary>
        double Resolution { get; set; }

        /// <summary>
        /// The x offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.
        /// </summary>
        double XOffset { get; set; }

        /// <summary>
        /// The y offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.
        /// </summary>
        double YOffset { get; set; }
    }
}
