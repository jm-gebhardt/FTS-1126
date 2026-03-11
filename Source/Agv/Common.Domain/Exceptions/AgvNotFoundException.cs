namespace Gebhardt.Acc.Common.Domain.Exceptions;

/// <summary>
/// Agv was not found
/// </summary>
public class AgvNotFoundException : NotFoundException
{
    /// <summary>
    /// Agv was not found with AgvCorrelationId
    /// </summary>
    /// <param name="correlationId">Correlation Id of Agv</param>
    public AgvNotFoundException(Guid? correlationId)
        : base("Agv", nameof(correlationId), correlationId)
    {
    }

    /// <summary>
    /// Agv was not found 
    /// </summary>
    /// <param name="message">The message to be shown.</param>
    public AgvNotFoundException(string message): base(message){}
}