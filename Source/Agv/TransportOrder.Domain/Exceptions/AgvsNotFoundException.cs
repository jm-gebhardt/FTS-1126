namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;

/// <summary>
/// Agvs were not found
/// </summary>
public class AgvsNotFoundException : BatchQueryException
{
    /// <summary>
    /// Agvs was not found by correlation id.
    /// </summary>
    /// <param name="correlationIds">Correlation ids of the agvs</param>
    public AgvsNotFoundException(ICollection<Guid> correlationIds)
        : base($"Unknown agv correlation ids: {string.Join(',', correlationIds.Select(c => c.ToString()))}")
    {
    }
}