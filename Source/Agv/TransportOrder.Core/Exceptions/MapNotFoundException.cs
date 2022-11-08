namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Exceptions
{
    /// <summary>
    /// Represents errors that occur when the map is not found in the e.g. registry
    /// </summary>
    public class MapNotFoundException : Exception
    {
        /// <inheritdoc cref="MapNotFoundException"/>
        public MapNotFoundException()
        {
                
        }

        /// <inheritdoc cref="MapNotFoundException"/>
        /// <param name="message">Exception message</param>
        public MapNotFoundException(string message) : base(message)
        {

        }
    }
}
