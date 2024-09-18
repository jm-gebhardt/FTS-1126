namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// No data was found for any of the given agvs.
/// </summary>
public class AgvsDataBatchQueryException : BatchQueryException
{
    /// <summary>
    /// Initializes a new <see cref="AgvsDataBatchQueryException"/>.
    /// </summary>
    /// <param name="correlationIds">the correlation is of the agvs</param>
    public AgvsDataBatchQueryException(ICollection<Guid> correlationIds)
        : base($"Data not found for agvs: {string.Join(',', correlationIds.Select(c => c.ToString()))}")
    {
    }
}