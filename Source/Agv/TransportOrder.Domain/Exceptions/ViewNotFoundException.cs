using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents the exception raised when a view could not be found.
/// </summary>
public class ViewNotFoundException : NotFoundException
{
    private const string View = "View";

    /// <summary>
    /// View was not found by id.
    /// </summary>
    public ViewNotFoundException(Guid id)
        : base(View, nameof(id), id)
    {
    }
}