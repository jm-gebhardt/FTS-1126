     namespace Gebhardt.StoreWare.Agv.Common.Exceptions
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
        public MapNotFoundException(string paramIdentifierName, object paramIdentifierValue)
            : base($"Map not found using the {paramIdentifierName}:{paramIdentifierValue}")
        {

        }
    }
}
