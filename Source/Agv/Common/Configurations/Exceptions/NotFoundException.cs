namespace Gebhardt.StoreWare.Agv.Common.Configurations.Exceptions
{
    /// <summary>
    /// Represents errors that occur when it is not found in the e.g. registry
    /// </summary>
    public class NotFoundException : Exception
    {
        /// <summary>
        /// <inheritdoc cref="NotFoundException"/>
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="paramIdentifierName"></param>
        /// <param name="paramIdentifierValue"></param>
        public NotFoundException(string paramName, string paramIdentifierName, object paramIdentifierValue)
            : base($"{paramName} not found using the {paramIdentifierName}:{paramIdentifierValue}")
        {

        }
    }
}
