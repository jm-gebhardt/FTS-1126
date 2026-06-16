using Gebhardt.Acc.Common.Domain.Exceptions;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// The Edge was not found.
/// </summary>
public class EdgeNotFoundException : NotFoundException
{
    /// <inheritdoc cref="EdgeNotFoundException"/>>
    [PublicAPI]
    public EdgeNotFoundException(Guid? edgeId, string? edgeDescription = null) : base($"{edgeDescription} Edge with Id {edgeId}")
    {
    }
}