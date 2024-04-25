using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Agv data was not found
/// </summary>
public class AgvDataNotFoundException : NotFoundException
{
    /// <summary>
    /// Agv data was not found for Agv with Agv correlation Id
    /// </summary>
    public AgvDataNotFoundException(Guid correlationId)
        : base(nameof(AgvData), nameof(correlationId), correlationId)
    {
    }
}