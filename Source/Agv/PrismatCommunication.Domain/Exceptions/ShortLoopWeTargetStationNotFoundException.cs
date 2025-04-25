using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Exceptions;

/// <summary>
/// The exception raised when the <see cref="Models.KaeserGraph.ShortLoopWeTargetStation"/>
/// could not be found.
/// </summary>
public class ShortLoopWeTargetStationNotFoundException : NotFoundException
{
    /// <inheritdoc cref="ShortLoopWeTargetStationNotFoundException"/>>
    public ShortLoopWeTargetStationNotFoundException(): base(nameof(Models.KaeserGraph.ShortLoopWeTargetStation)){}
}