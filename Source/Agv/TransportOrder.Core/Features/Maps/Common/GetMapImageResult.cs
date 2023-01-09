namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Common
{
    /// <summary>
    /// Defines a result for the GetMapImage request
    /// </summary>
    public class GetMapImageResult
    {
        /// <summary>
        /// <inheritdoc cref="GetMapImageResult"/>
        /// </summary>
        public byte[] Image { get; init; } = null!;
    }
}
