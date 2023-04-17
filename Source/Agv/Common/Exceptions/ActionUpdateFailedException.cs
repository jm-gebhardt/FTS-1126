
namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// Update of action in registry failed
/// </summary>
public class ActionUpdateFailedException : Exception
{
    /// <see cref="AgvLocationUnknownException"/>>
    /// <param name="actionCorrelationId">The CorrelationId of the </param>
    /// <param name="paramNameToUpdate">The object/property tried to update</param>
    /// <param name="paramValue">The target value</param>
    public ActionUpdateFailedException(Guid actionCorrelationId, string paramNameToUpdate, object paramValue ) 
        : base($"Update of Action with correlationId: {actionCorrelationId} of {paramNameToUpdate} to {paramValue} failed")
    {
        
    }   
}