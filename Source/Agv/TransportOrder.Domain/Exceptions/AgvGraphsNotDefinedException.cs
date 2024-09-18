namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// A graph of is null.
/// </summary>
public class AgvGraphsNotDefinedException : BatchQueryException
{
    /// <summary>
    /// Initializes a new <see cref="AgvGraphsNotDefinedException"/>
    /// </summary>
    /// <param name="correlationIds">the correlation is of the agvs</param>
    public AgvGraphsNotDefinedException(ICollection<Guid> correlationIds)
        : base($"Graph undefined for agvs: {string.Join(',', correlationIds.Select(c => c.ToString()))}")
    {
    }
}