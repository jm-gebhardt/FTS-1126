namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Exceptions
{
    /// <summary>
    /// Represents errors that occur when there is no content found in the e.g. registry
    /// </summary>
    public class NoContentException : Exception
    {
        /// <inheritdoc cref="NoContentException"/>
        /// <param name="paramName"></param>
        public NoContentException(string paramName)
            : base($"No content found for {paramName}")
        {

        }
    }
}
