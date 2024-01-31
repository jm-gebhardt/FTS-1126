using Gebhardt.StoreWare.Agv.Common.Utilities;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Contracts;

/// <summary>
/// Interface for AGV identification for better generalization and simplification of documentation.
/// </summary>
[PublicAPI]
public interface IAgv
{
    /// <summary>
    /// The correlation Id of the Agv.
    /// </summary>
    Guid CorrelationId { get; }

    /// <summary>
    /// The serial number of the agv.
    /// It is unique for the whole system.
    /// </summary>
    /// <remarks>
    /// For internal correlation the ID is transformed into a Guid (see <see cref="StringExtensions.ToGuid(string)"/>
    /// </remarks>
    string SerialNumber { get; }
}