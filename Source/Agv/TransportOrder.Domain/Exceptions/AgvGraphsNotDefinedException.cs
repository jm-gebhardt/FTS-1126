namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;

/// <summary>
/// The graph of any of the Agvs is null.
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