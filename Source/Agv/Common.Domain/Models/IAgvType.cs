using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// Describes an AGV type.
/// </summary>
[PublicAPI]
public interface IAgvType
{
    /// <summary>
    /// The unique identifier of the AGV type, consisting of manufacturer and series name.
    /// </summary>
    string AgvTypeIdentifier { get; }

    /// <summary>
    /// The manufacturer of the AGV type.
    /// </summary>
    string Manufacturer { get; }

    /// <summary>
    /// The series name of the AGV type.
    /// </summary>
    string SeriesName { get; }

    /// <summary>
    /// The length of the AGV type in [m].
    /// </summary>
    double Length { get; }

    /// <summary>
    /// The width of the AGV type in [m].
    /// </summary>
    double Width { get; }

    /// <summary>
    /// Whether the AGV type has been acknowledged (confirmed) by an operator.
    /// </summary>
    bool Acknowledged { get; }
}