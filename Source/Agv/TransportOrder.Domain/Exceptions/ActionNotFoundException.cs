using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Agv Action was not found
/// </summary>
public class ActionNotFoundException : NotFoundException
{
    /// <summary>
    /// Action was not found
    /// </summary>
    /// <param name="description">Description what action we were looking for</param>
    /// <param name="paramName">identifier name. example: AgvId</param>
    /// <param name="paramValue">identifier value</param>
    public ActionNotFoundException(string description, string paramName, object paramValue)
        : base(description, paramName, paramValue)
    {
    }
}