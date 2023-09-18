using Gebhardt.StoreWare.Agv.Common.Utilities;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Contracts;

/// <summary>
/// Interface for AGV identification for better generalization and simplification of documentation.
/// </summary>
[PublicAPI]
public interface IAgvId
{
    /// <summary>
    /// Human readable unique AGV identification.
    /// <para>For TAR this is the serial number of the AGV.
    /// For other vendors this can be any other unique identification literal.</para>
    /// </summary><remarks>
    /// For internal correlation the ID is transformed into a Guid (see <see cref="StringExtensions.ToGuid(string)"/>
    /// </remarks>
    string AgvId { get; }
}