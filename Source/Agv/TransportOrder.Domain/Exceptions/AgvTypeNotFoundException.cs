using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents the exception raised when an agv type could not be found.
/// </summary>
public class AgvTypeNotFoundException : NotFoundException
{
    /// <summary>
    /// Agv type was not found by id.
    /// </summary>
    public AgvTypeNotFoundException(Guid id)
        : base(nameof(AgvType), nameof(id), id)
    {
    }
}