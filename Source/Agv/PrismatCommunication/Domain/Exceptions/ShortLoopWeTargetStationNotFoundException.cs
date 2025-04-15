using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Exceptions;

/// <summary>
/// The exception raised when the <see cref="ShortLoopWeTargetStation"/>
/// could not be found.
/// </summary>
public class ShortLoopWeTargetStationNotFoundException : NotFoundException
{
    /// <inheritdoc cref="ShortLoopWeTargetStationNotFoundException"/>>
    public ShortLoopWeTargetStationNotFoundException(): base(nameof(ShortLoopWeTargetStation)){}
}